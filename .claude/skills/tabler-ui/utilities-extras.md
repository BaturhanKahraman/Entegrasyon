# Tabler — Utilities, Plugins, Icons & Extras

Reference for Tabler UI utility classes, plugins (flags/payments/social), Tabler Icons embedding, illustrations and email templates. Tabler is Bootstrap-based, so most utilities follow Bootstrap conventions (`.m-*`, `.p-*`, `.border`, `.rounded`) — this doc captures the exact classes and Tabler-specific additions. Source: https://docs.tabler.io/ui/

> Spacing/border/align utilities are responsive-capable in Bootstrap fashion where applicable (e.g. `.m-md-4`), but stick to the documented base classes below unless verified.

---

## Borders

Source: https://docs.tabler.io/ui/utilities/borders

### Direction (which sides get a border)
- `.border` — border on all sides
- `.border-top` — top only
- `.border-end` — right (end) only
- `.border-bottom` — bottom only
- `.border-start` — left (start) only
- `.border-x` — left + right
- `.border-y` — top + bottom

### Size / thickness
- `.border-0` — remove all borders
- `.border` — standard thickness
- `.border-wide` — thicker, more prominent border (Tabler-specific)

### Remove borders selectively
- `.border-top-0`, `.border-end-0`, `.border-bottom-0`, `.border-start-0`
- `.border-x-0`, `.border-y-0`

### Border color (theme colors)
- `.border-primary`, `.border-secondary`, `.border-success`, `.border-warning`, `.border-danger`, `.border-info`, `.border-dark`, `.border-light`
- (Tabler theme palette colors also work, e.g. `.border-azure`, `.border-green`, `.border-red` etc.)

### Border opacity
- `.border-opacity-10`, `.border-opacity-25`, `.border-opacity-50`, `.border-opacity-75`

### Border radius
- `.rounded-0` — no rounding
- `.rounded` — standard radius
- `.rounded-1`, `.rounded-2`, `.rounded-3` — increasing radius
- `.rounded-circle` — full circle
- (Bootstrap also provides `.rounded-pill`, `.rounded-top/-end/-bottom/-start`)

```html
<div class="border border-primary rounded-2 border-opacity-50">…</div>
<div class="border-bottom border-wide">heading underline</div>
<img class="rounded-circle" src="avatar.jpg" alt="">
```

---

## Cursors

Source: https://docs.tabler.io/ui/utilities/cursors

| Class | Effect |
|---|---|
| `.cursor-auto` | cursor depends on element content (browser default per context) |
| `.cursor-default` | the default arrow cursor |
| `.cursor-pointer` | pointing hand — element is clickable |
| `.cursor-move` | element can be moved |
| `.cursor-not-allowed` | action not allowed |
| `.cursor-none` | no cursor displayed |
| `.cursor-help` | help info available |
| `.cursor-progress` | action in progress (still interactive) |
| `.cursor-wait` | busy, not interactive |
| `.cursor-text` | text can be selected/typed |
| `.cursor-v-text` | vertical-text input |
| `.cursor-grab` | element can be grabbed |
| `.cursor-grabbing` | element is being grabbed |
| `.cursor-zoom-in` | can zoom in |
| `.cursor-zoom-out` | can zoom out |

```html
<div class="cursor-pointer">Clickable row</div>
<button class="cursor-not-allowed" disabled>Disabled</button>
```

---

## Interactions (pointer-events & user-select)

Source: https://docs.tabler.io/ui/utilities/interactions

### Text selection
- `.user-select-all` — clicking selects all content in the element
- `.user-select-auto` — default browser selection behavior
- `.user-select-none` — content not selectable

### Pointer events
- `.pe-none` — disables interaction (no click). **A11y:** also add `tabindex="-1"` and `aria-disabled="true"`.
- `.pe-auto` — re-enables interaction (default)

```html
<p class="user-select-all">Click to select this token</p>
<span class="user-select-none">non-selectable label</span>
<a href="#" class="pe-none" tabindex="-1" aria-disabled="true">Disabled link</a>
```

---

## Margins & Spacing

Source: https://docs.tabler.io/ui/utilities/margins

### Spacing scale (applies to margin, padding, gap)
| Step | Value |
|---|---|
| 0 | 0 |
| 1 | 0.25rem |
| 2 | 0.5rem |
| 3 | 1rem |
| 4 | 1.5rem |
| 5 | 2rem |
| 6 | 3rem |
| 7 | 5rem |
| 8 | 8rem |

> Note: Tabler extends the Bootstrap scale beyond 5 — it goes up to **8**.

### Margin classes
- All sides: `.m-0` … `.m-8`
- Sides: `.mt-*` (top), `.mb-*` (bottom), `.ms-*` (start/left), `.me-*` (end/right), `.mx-*` (left+right), `.my-*` (top+bottom)
- `.mx-auto` — horizontally center a fixed-width block (margins → auto)
- `.m-auto`, `.mt-auto`, `.mb-auto`, etc. — auto on that axis/side

### Padding classes
Same scale and side notation, prefix `p`:
- `.p-0` … `.p-8`
- `.pt-*`, `.pb-*`, `.ps-*`, `.pe-*`, `.px-*`, `.py-*`
- (Padding has no `auto`.)

### Gap (for flex/grid containers)
- `.gap-0` … `.gap-8` — space between grid/flex children
- (Bootstrap also: `.row-gap-*`, `.column-gap-*`)

```html
<div class="m-4 p-3">card-ish box</div>
<div class="mt-4 mb-8">extra bottom spacing (8rem)</div>
<div class="mx-auto" style="width:320px">centered block</div>
<div class="d-grid gap-3">…grid items spaced 1rem…</div>
<div class="d-flex gap-2">…flex items spaced 0.5rem…</div>
```

**Gotcha:** Negative margins are not documented on this Tabler page; rely on Bootstrap's `.m*-n1`…`.m*-n5` only if confirmed in the build, otherwise avoid.

---

## Vertical Align

Source: https://docs.tabler.io/ui/utilities/vertical-align

Affects inline / inline-block / inline-table / table-cell elements only.

- `.align-baseline`
- `.align-top`
- `.align-middle`
- `.align-bottom`
- `.align-text-top`
- `.align-text-bottom`

```html
<span class="align-middle"><i class="ti ti-star"></i> middle-aligned icon + text</span>
<td class="align-top">top cell</td>
```

---

## Visually Hidden (screen-reader utilities)

Source: https://docs.tabler.io/ui/utilities/visually-hidden

- `.visually-hidden` — hidden visually but exposed to assistive tech (screen readers)
- `.visually-hidden-focusable` — hidden until focused (or a child is focused via `:focus-within`); ideal for skip links

```html
<h2 class="visually-hidden">Section title for screen readers</h2>
<a class="visually-hidden-focusable" href="#content">Skip to main content</a>
<button class="btn btn-icon"><i class="ti ti-trash"></i><span class="visually-hidden">Delete</span></button>
```

**Use for icon-only buttons** to provide an accessible label.

---

## Plugin: Flags (country flags)

Source: https://docs.tabler.io/ui/plugins/flags — distributed as a separate plugin CSS.

### Include
```html
<link rel="stylesheet"
  href="https://cdn.jsdelivr.net/npm/@tabler/core@latest/dist/css/tabler-flags.min.css">
```

### Usage — `.flag` + `.flag-country-{ISO2}`
```html
<span class="flag flag-country-us"></span>
<span class="flag flag-country-tr"></span>
<span class="flag flag-country-gb"></span>
```

### Sizes
`.flag-xs`, `.flag-sm`, `.flag-md`, `.flag-lg`, `.flag-xl`
```html
<span class="flag flag-lg flag-country-tr"></span>
```

250+ countries, two-char ISO codes (e.g. `tr`, `us`, `gb`, `fr`, `de`, `jp`).

---

## Plugin: Payments (payment provider logos)

Source: https://docs.tabler.io/ui/plugins/payments

### Include
```html
<link rel="stylesheet"
  href="https://cdn.jsdelivr.net/npm/@tabler/core@latest/dist/css/tabler-payments.min.css">
```

### Usage — `.payment` + `.payment-provider-{name}`
```html
<span class="payment payment-provider-visa"></span>
<span class="payment payment-provider-mastercard"></span>
<span class="payment payment-provider-paypal"></span>
```

### Dark variants
Add `-dark` suffix: `.payment-provider-visa-dark`, `.payment-provider-mastercard-dark`.

### Sizes
`.payment-xs`, `.payment-sm`, `.payment-md`, `.payment-lg`, `.payment-xl`
```html
<span class="payment payment-lg payment-provider-visa"></span>
```

150+ providers: Visa, Mastercard, Amex, PayPal, Apple Pay, Google Pay, Amazon Pay, Stripe, Klarna, Alipay, WeChat Pay, Bitcoin, Ethereum, etc.

---

## Plugin: Social Icons

Source: https://docs.tabler.io/ui/plugins/social-icons

### Include
```html
<link rel="stylesheet"
  href="https://cdn.jsdelivr.net/npm/@tabler/core@latest/dist/css/tabler-socials.min.css">
```

### Usage — `.social` + `.social-app-{platform}`
```html
<span class="social social-app-facebook"></span>
<span class="social social-app-x"></span>
<span class="social social-app-instagram"></span>
<span class="social social-app-github"></span>
<span class="social social-app-linkedin"></span>
<span class="social social-app-youtube"></span>
```

30+ platforms: facebook, x, instagram, github, linkedin, youtube, discord, telegram, whatsapp, tiktok, etc.

**Note:** For interactive/branded social *buttons* in app UI you often just use Tabler Icons brand glyphs instead (`<i class="ti ti-brand-github"></i>`) — the socials plugin renders the official colored logos.

---

## Tabler Icons (the important one)

Tabler Icons: 5000+ free SVG icons, designed on a **24×24 grid with 2px stroke**, outline + filled variants. Two practical embedding methods in this MVC project: **webfont** (simplest, used in most Tabler templates) and **inline SVG / sprite**.

### Method 1 — Webfont (recommended for Razor views)

Include the webfont CSS once (layout `<head>`):
```html
<!-- via CDN -->
<link rel="stylesheet"
  href="https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@latest/dist/tabler-icons.min.css">
```
(When Tabler Core is bundled it typically already ships the `ti` font; if icons render as boxes, the webfont CSS is missing.)

Embed an icon with an `<i>` (or `<span>`) using `ti` + `ti-{name}`:
```html
<i class="ti ti-home"></i>
<i class="ti ti-user"></i>
<i class="ti ti-brand-tabler"></i>
<i class="ti ti-trash"></i>
```

Name = the kebab-case icon name from https://tabler.io/icons (e.g. `arrow-left`, `plus`, `edit`, `search`, `chevron-down`). Brand icons are prefixed `brand-` (e.g. `ti-brand-github`).

**Filled variants:** filled icons use the `-filled` suffix on the name, e.g.:
```html
<i class="ti ti-star-filled"></i>
<i class="ti ti-heart-filled"></i>
```

### Method 2 — Inline SVG / sprite

```html
<!-- direct inline SVG (control via width/height + stroke) -->
<svg xmlns="http://www.w3.org/2000/svg" class="icon icon-tabler icon-tabler-home"
     width="24" height="24" viewBox="0 0 24 24" stroke-width="2"
     stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
  <!-- icon paths -->
</svg>

<!-- sprite reference -->
<svg width="24" height="24">
  <use xlink:href="/lib/tabler-icons/tabler-sprite.svg#tabler-home" />
</svg>
```

### Sizing & coloring

**Webfont** — icon is a font glyph, so size with `font-size` and color with `color` (it inherits `currentColor`):
```html
<i class="ti ti-bell" style="font-size: 1.5rem; color: var(--tblr-red);"></i>
<!-- inherits surrounding text color/size by default -->
<span class="text-success fs-2"><i class="ti ti-check"></i> Done</span>
```

**Inline SVG** — size with `width`/`height` (or `.icon` class), color via `stroke="currentColor"`, thickness via `stroke-width`:
```css
.icon-tabler { width: 32px; height: 32px; color: red; stroke-width: 1.5; }
```

### Tabler helper classes for icons
- `.icon` — base sizing class Tabler applies to inline SVG icons (≈24px, vertical-align tweaks)
- `.icon-lg`, `.icon-sm` — larger / smaller icon sizing
- Pair with button helpers: `.btn .btn-icon` for icon-only buttons (always add a `.visually-hidden` label for a11y)

```html
<a href="#" class="btn btn-icon" aria-label="Edit">
  <i class="ti ti-edit"></i>
</a>
<button class="btn btn-primary">
  <i class="ti ti-plus"></i> Yeni Ekle
</button>
```

**Gotchas:**
- Icon renders as a square/box → webfont CSS not loaded, or wrong icon name.
- Use `currentColor` (default) so icons follow text color utilities (`.text-muted`, `.text-danger`, theme colors).
- For clickable icon-only elements always supply an accessible name (`aria-label` or `.visually-hidden` text).

---

## Illustrations (light)

Source: https://docs.tabler.io/illustrations/introduction

Tabler Illustrations is a collection of ~85 high-quality, customizable SVG illustrations matching the Tabler design system — good for empty states, onboarding, error/404 pages. They're customizable (recolor to brand). Embed as plain `<img src="…svg">` or inline SVG. Get them from https://tabler.io/illustrations (some are part of the paid bundle). Pair with Tabler's `.empty` component for empty-state screens.

---

## Emails (light)

Source: https://docs.tabler.io/emails/introduction

Tabler Emails is a set of ~80 responsive, customizable HTML email templates, compatible with 90+ email clients/devices. Ships both compiled ready-to-use HTML and source templates for customization. Not part of the in-app MVC UI — use only if you need branded transactional/marketing emails. Get them from https://tabler.io/emails.
