# Auth Strategy

## Decision

Выбран механизм: **JWT access token + refresh token**.

- Access token: JWT, короткоживущий.
- Refresh token: долгоживущий, с обязательной ротацией на каждом `refresh`.
- Refresh token хранится в БД **только в виде хеша**.

Это решение фиксируем как базовое для всех новых auth-эндпоинтов.

## Parameters

- Access TTL: **15 минут**.
- Refresh TTL: **30 дней**.
- Rotation: **всегда** (один refresh-токен одноразовый).
- Подпись JWT: `RS256` (предпочтительно) или `HS256` при отсутствии KMS.
- Обязательные claims access token: `sub`, `exp`, `iat`, `jti`, `role`/`scope`.

## Storage

Решение для web frontend:

- Refresh token хранится в **`httpOnly` + `Secure` cookie**.
- `SameSite=Lax` по умолчанию, `SameSite=Strict` если UX позволяет.
- Access token хранить в памяти приложения (in-memory), не в `localStorage`.

Почему не secure storage для web: в браузере это обычно `localStorage/sessionStorage`, что хуже по XSS-риску. Для native-клиентов использовать platform secure storage допустимо как отдельный профиль.

## DB Model For Refresh Tokens

Минимальная таблица `refresh_tokens`:

- `id`
- `user_id`
- `token_hash` (SHA-256 или Argon2id)
- `jti`
- `family_id` (цепочка ротации)
- `expires_at`
- `created_at`
- `revoked_at` (nullable)
- `replaced_by_jti` (nullable)
- `ip` / `user_agent` (опционально для аудитов)

## Flows

### Login

1. Пользователь проходит проверку credentials.
2. Сервер выдает access token (15m).
3. Сервер выдает refresh token (30d) и сохраняет его hash в БД.
4. Refresh отправляется в `httpOnly` cookie.

### Refresh

1. Клиент вызывает `/auth/refresh` с refresh cookie.
2. Сервер проверяет подпись/валидность refresh и сверяет hash в БД.
3. Если токен валиден и не отозван, старый refresh помечается как использованный/отозванный.
4. Выпускается новая пара: access + refresh.
5. Новый refresh hash сохраняется в той же `family_id`.
6. При повторном использовании старого refresh: ревокация всей `family` (replay защита).

### Logout

1. Клиент вызывает `/auth/logout`.
2. Сервер ревокает текущий refresh (или всю family для logout-all-devices).
3. Сервер очищает refresh cookie.
4. Access токен истекает естественно (15m) или добавляется в denylist при high-risk сценариях.

## Security Notes

- Всегда HTTPS.
- Ротация refresh токенов обязательна.
- Refresh хранить только как hash; plaintext не логировать.
- Добавить rate limit на `/auth/login` и `/auth/refresh`.
- Для cookie-based refresh: CSRF защита (`SameSite` + anti-CSRF token для state-changing операций).
- Ротация signing keys по расписанию; поддержка `kid`.
- Логи: аудит login/refresh/logout, детект reuse refresh.

## Tradeoffs

Плюсы:

- Масштабируется лучше, чем server-side sessions в memory.
- Короткий access TTL снижает ущерб при компрометации access токена.
- Ротация refresh + hash в БД дает управляемую ревокацию и replay detection.

Минусы:

- Сложнее реализации (family, rotation, revoke logic).
- Нужна таблица/индексы и операционная дисциплина по ключам.
- При cookie-подходе нужно аккуратно закрыть CSRF.

## Non-Goals

- Здесь не фиксируется внешний IdP/OAuth provider.
- Здесь не фиксируется MFA policy (будет отдельным документом).
