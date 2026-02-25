# Brand Style Guide v0

## Core direction

- Dark base first: deep backgrounds carry the interface, not gradients of bright color blocks.
- Pastel pink is the primary emotional accent (`primary`), used for call-to-action fill and subtle highlights.
- Neon is support-only (`accentNeon`): rings, glow, active indicators. It must not become the main fill or body text color.

## Surfaces

- `bg0` (app backdrop): `#0b0610`
- `bg1` (layout surface / panels): `#120a1a`
- `bg2` (cards, modals, elevated blocks): `#1a1024`
- `border` (low-contrast structure): `#2a1a33`
- `text` (primary text): `#f6eaf5`
- `mutedText` (secondary text): `#cbb7c9`

Usage notes:

- Main page/background layers: `bg0` + `bg1` gradients.
- Standard container backgrounds: `bg1`.
- Components with elevation (cards/modals/toasts): `bg2`.
- Keep borders subtle; avoid high-contrast white outlines for resting state.

## Accents and status

Primary accents:

- `primary` (pastel pink fill): `#f4a6c8`
- `primaryHover`: `#ffb7d9`
- `accentNeon` (glow/outline only): `#ff3fb4`
- `secondary` (lavender): `#bfa2ff`

Status accents (same aesthetic, no bootstrap defaults):

- `success`: `#4de1c1`
- `warn`: `#ffb86b`
- `error`: `#ff5a7a`
- `danger`: `#ff5a7a`

## Glow rules

Glow tokens:

- `glowSm`: `0 0 10px rgba(255,63,180,0.25)`
- `glowMd`: `0 0 18px rgba(255,63,180,0.35)`
- `glowLg`: `0 0 28px rgba(255,63,180,0.45)`

Allowed:

- Focus-visible state: `outline: 2px accentNeon` + `glowSm`
- Primary button hover: `glowMd`
- Active tab/nav item: border `accentNeon` + `glowSm`
- Card hover: very light border tint + optional `glowSm`

Not allowed:

- Neon glow in static resting states for all components
- Neon glow as substitute for focus outline
- Full neon backgrounds as large surfaces

## Typography

- Display: `Unbounded`
- Body: `Space Grotesk`
- Mono: `JetBrains Mono`
- `letterSpacingDisplay`: `-0.02em`

Fallback policy: each family must include `system-ui` fallback via token value.

## Do / Don't

Do:

1. Use `text` on `bg0/bg2` as default readable pair.
2. Keep neon mostly interactive (`hover`, `focus`, `active`).
3. Use pastel pink (`primary`) for primary CTA fills.
4. Use `mutedText` for helper/metadata text instead of lowering opacity on main text.

Don't:

1. Do not use `accentNeon` as body text color.
2. Do not apply `glowLg` to cards/lists in resting state.
3. Do not replace focus outline with glow only.
4. Do not introduce neutral gray admin-like backgrounds outside token palette.
