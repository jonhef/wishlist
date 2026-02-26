# Auth Strategy

## Decision

Chosen mechanism: **JWT access token + refresh token**.

- Access token: JWT, short-lived.
- Refresh token: long-lived, with mandatory rotation on every `refresh`.
- Refresh token is stored in DB **only as a hash**.

This decision is the baseline for all new auth endpoints.

## Parameters

- Access TTL: **15 minutes**.
- Refresh TTL: **30 days**.
- Rotation: **always** (single-use refresh token).
- JWT signature: `RS256` (preferred) or `HS256` when KMS is unavailable.
- Required access token claims: `sub`, `exp`, `iat`, `jti`, `role`/`scope`.

## Storage

Web frontend approach:

- Refresh token is stored in an **`httpOnly` + `Secure` cookie**.
- `SameSite=Lax` by default, `SameSite=Strict` if UX allows.
- Keep access token in app memory (in-memory), not in `localStorage`.

Why not secure storage for web: in browsers this is usually `localStorage/sessionStorage`, which is worse for XSS risk. For native clients, platform secure storage can be used as a separate profile.

## DB Model For Refresh Tokens

Minimum `refresh_tokens` table:

- `id`
- `user_id`
- `token_hash` (SHA-256 or Argon2id)
- `jti`
- `family_id` (rotation chain)
- `expires_at`
- `created_at`
- `revoked_at` (nullable)
- `replaced_by_jti` (nullable)
- `ip` / `user_agent` (optional for audits)

## Flows

### Login

1. User passes credential validation.
2. Server issues access token (15m).
3. Server issues refresh token (30d) and stores its hash in DB.
4. Refresh is sent in an `httpOnly` cookie.

### Refresh

1. Client calls `/auth/refresh` with refresh cookie.
2. Server validates refresh signature/validity and compares hash in DB.
3. If token is valid and not revoked, old refresh is marked as used/revoked.
4. New pair is issued: access + refresh.
5. New refresh hash is stored in the same `family_id`.
6. If an old refresh is reused: revoke the whole `family` (replay protection).

### Logout

1. Client calls `/auth/logout`.
2. Server revokes current refresh (or whole family for logout-all-devices).
3. Server clears refresh cookie.
4. Access token expires naturally (15m) or is added to denylist in high-risk scenarios.

## Security Notes

- Always HTTPS.
- Refresh token rotation is mandatory.
- Store refresh only as hash; do not log plaintext.
- Add rate limit to `/auth/login` and `/auth/refresh`.
- For cookie-based refresh: CSRF protection (`SameSite` + anti-CSRF token for state-changing operations).
- Rotate signing keys on schedule; support `kid`.
- Logs: login/refresh/logout audit, refresh reuse detection.

## Tradeoffs

Pros:

- Scales better than in-memory server-side sessions.
- Short access TTL reduces damage when access token is compromised.
- Refresh rotation + DB hash gives controlled revocation and replay detection.

Cons:

- Implementation is more complex (family, rotation, revoke logic).
- Requires table/indexes and operational key discipline.
- With cookie approach, CSRF must be handled carefully.

## Non-Goals

- This document does not lock an external IdP/OAuth provider.
- This document does not lock MFA policy (will be in a separate document).
