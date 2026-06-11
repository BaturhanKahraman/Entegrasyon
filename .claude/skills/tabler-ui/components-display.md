# Tabler — Display & Feedback Components

> Source: https://docs.tabler.io/ui/components/<slug>. Extracted reference for accurate class usage.
>
> **#1 RULE — solid color combos:** A solid-colored element needs BOTH a `bg-COLOR` class AND a `text-COLOR-fg` text class. Light variants use the `-lt` suffix: `bg-COLOR-lt` (+ `text-COLOR-lt-fg`). Never write `bg-green` alone on a badge/element expecting readable text.
>
> Tabler colors: `blue azure indigo purple pink red orange yellow lime green teal cyan` (+ semantic `primary secondary success danger warning info dark light`). Semantic colors (`success/danger/warning/info`) are aliases used by alerts/progress; the named palette (`green/red/yellow/blue`) uses the `-fg`/`-lt` system.

---

## Alerts
**Base:** `.alert` (add `role="alert"`)
**Color variants:** `.alert-success` `.alert-info` `.alert-warning` `.alert-danger` (also named: `.alert-lime`, `.alert-cyan`, social `.alert-facebook` etc.)
**Modifiers:**
- `.alert-dismissible` — enables the close button
- `.alert-important` — solid/eye-catching fill (color becomes background). Combine: `.alert.alert-important.alert-success`
- `.alert-link` — styles `<a>` inside the alert to match text color
**Structure:** `.alert-icon` (icon wrapper), `.alert-title` / `.alert-heading` (title), `.alert-description` (body text)
**Data/JS:** close button `<a class="btn-close" data-bs-dismiss="alert" aria-label="close"></a>`
**Example:**
```html
<div class="alert alert-success alert-dismissible" role="alert">
  <div class="alert-icon"><!-- svg --></div>
  <div>
    <h4 class="alert-title">Saved!</h4>
    <div class="alert-description">Changes were stored successfully.</div>
  </div>
  <a class="btn-close" data-bs-dismiss="alert" aria-label="close"></a>
</div>
```
**Gotchas:** Standard alerts are the light/tinted style. For a fully saturated solid alert add `.alert-important`. Use `.alert-link` (not raw `<a>`) for links inside alerts.

---

## Avatars
**Base:** `.avatar` (works on `<span>`, `<a>`, `<div>`)
**Sizes:** `.avatar-xs` `.avatar-sm` (default) `.avatar-md` `.avatar-lg` `.avatar-xl` `.avatar-2xl`
**Shapes:** default (slight radius); `.rounded`, `.rounded-3`, `.rounded-circle` (full circle), `.rounded-0` (square)
**Content types:**
- Image: `style="background-image: url(/img/x.jpg)"` (NOT an `<img>` tag)
- Initials: text node inside the span, e.g. `JL`
- Icon: nested SVG
**Color combos (initials/icon):** light tint = `.bg-COLOR-lt` (e.g. `.bg-primary-lt`, `.bg-green-lt`, `.bg-red-lt`, `.bg-purple-lt`). Solid = `.bg-COLOR text-COLOR-fg`.
**Status indicator:** nest a `.badge` with a color, e.g. `<span class="badge bg-success"></span>` (positions in corner)
**Lists:** `.avatar-list` (grouped row); `.avatar-list-stacked` (overlapping). Overflow counter uses an avatar with `+5` text.
**Example:**
```html
<span class="avatar avatar-lg rounded-circle" style="background-image: url(/img/u.jpg)"></span>
<span class="avatar bg-primary-lt">JL</span>
<div class="avatar-list avatar-list-stacked">
  <span class="avatar avatar-sm rounded-circle" style="background-image:url(/a.jpg)"></span>
  <span class="avatar avatar-sm rounded-circle bg-blue-lt">+5</span>
</div>
```
**Gotchas:** Image avatars use `background-image`, not `<img>`. Initial avatars need a `-lt` bg (or solid `bg + text-*-fg`) — bare `bg-green` gives unreadable contrast.

---

## Badges
**Base:** `.badge`
**Color combos (CRITICAL):**
- Solid: `.badge.bg-COLOR.text-COLOR-fg` → e.g. `badge bg-red text-red-fg`
- Light: `.badge.bg-COLOR-lt.text-COLOR-lt-fg` → e.g. `badge bg-blue-lt text-blue-lt-fg`
- Available colors: blue azure indigo purple pink red orange yellow lime green teal cyan
**Modifiers/sizes:** `.badge-pill` (rounded full), `.badge-sm` / `.badge-lg`
**Specialized:**
- Notification dot: `.badge-notification` (empty span inside a button/element), often `.badge.bg-red.text-red-fg.badge-notification`
- Blinking: add `.badge-blink`
- Link badge: put `.badge` on an `<a>`
**Example:**
```html
<span class="badge bg-green text-green-fg">Active</span>
<span class="badge bg-yellow-lt text-yellow-lt-fg">Pending</span>
<span class="badge bg-blue text-blue-fg badge-pill">12</span>
<button class="btn position-relative">
  Inbox <span class="badge bg-red text-red-fg badge-notification"></span>
</button>
<h1>Title <span class="badge bg-green-lt text-green-lt-fg">New</span></h1>
```
**Gotchas:** This is THE most-broken component. `badge bg-green` alone renders dark text on green — always pair with `text-green-fg` (solid) or use `bg-green-lt text-green-lt-fg` (light). Use `ms-2` to space a badge after button/heading text.

---

## Cards
**Base:** `.card`
**Structure:** `.card-header`, `.card-body`, `.card-footer`, `.card-title`, `.card-img-top` (top image)
**Sizes (padding):** `.card-sm`, `.card-md` (default), `.card-lg`
**Status accent bar:** `.card-status-top` or `.card-status-start` (left), combined with a color: `.card-status-top.bg-danger` / `.bg-green`
**Variations:** `.card-stacked` (stacked effect); `.card-tabs` (tabbed card wrapper); use `.row.row-deck` on a row to equalize card heights; `.card-borderless` / `.card-active` exist in Tabler though not in the core docs snippet.
**Example:**
```html
<div class="card">
  <div class="card-status-top bg-danger"></div>
  <div class="card-header"><h3 class="card-title">Report</h3></div>
  <div class="card-body">Body content</div>
  <div class="card-footer">Footer</div>
</div>
```
```html
<div class="row row-deck">
  <div class="col-md-4"><div class="card">...</div></div>
  <div class="col-md-4"><div class="card">...</div></div>
</div>
```
**Gotchas:** Status bar color goes on the `.card-status-*` element itself. For equal-height card grids use `row-deck` on the parent `.row`. Ribbons inside a card require the card to be `position-relative` (it is by default).

---

## Empty (Empty states)
**Base:** `.empty` (centered container)
**Structure:**
- `.empty-icon` — SVG icon wrapper
- `.empty-img` — illustration/image wrapper
- `.empty-header` — large display text (e.g. "404")
- `.empty-title` — main heading
- `.empty-subtitle` — secondary text (often `.text-secondary`)
- `.empty-action` — button/action container
**Example:**
```html
<div class="empty">
  <div class="empty-icon"><!-- svg --></div>
  <p class="empty-title">No results found</p>
  <p class="empty-subtitle text-secondary">Try adjusting your search or filters.</p>
  <div class="empty-action">
    <a href="#" class="btn btn-primary">Reset filters</a>
  </div>
</div>
```
```html
<div class="empty">
  <div class="empty-header">404</div>
  <p class="empty-title">Page not found</p>
  <div class="empty-action"><a href="/" class="btn btn-primary">Take me home</a></div>
</div>
```
**Gotchas:** Use either `.empty-icon`, `.empty-img`, or `.empty-header` — not all three. Subtitle typically pairs with `.text-secondary`.

---

## Modals
**Base structure:** `.modal` (container, `tabindex="-1"`) → `.modal-dialog` → `.modal-content` → `.modal-header` / `.modal-body` / `.modal-footer`; title `.modal-title`
**Sizes:** `.modal-sm` `.modal-lg` `.modal-xl` `.modal-full-width` (apply to `.modal-dialog`)
**Layout:** `.modal-dialog-centered` (vertically centered), `.modal-dialog-scrollable` (scroll long body)
**Status/color bar:** `.modal-status` + color, e.g. `<div class="modal-status bg-danger"></div>` (top accent). Centered icon modals use `.modal-body.text-center.py-4`.
**Data/JS:**
- Trigger: `data-bs-toggle="modal" data-bs-target="#id"`
- Close: `data-bs-dismiss="modal"`
- Static backdrop: `data-bs-backdrop="static"` (and usually `data-bs-keyboard="false"`)
**Example:**
```html
<button class="btn" data-bs-toggle="modal" data-bs-target="#modal-danger">Delete</button>

<div class="modal modal-blur fade" id="modal-danger" tabindex="-1" role="dialog">
  <div class="modal-dialog modal-sm modal-dialog-centered" role="document">
    <div class="modal-content">
      <div class="modal-status bg-danger"></div>
      <button class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
      <div class="modal-body text-center py-4">
        <h3>Are you sure?</h3>
        <div class="text-secondary">This action cannot be undone.</div>
      </div>
      <div class="modal-footer">
        <button class="btn me-auto" data-bs-dismiss="modal">Cancel</button>
        <button class="btn btn-danger">Delete</button>
      </div>
    </div>
  </div>
</div>
```
**Gotchas:** Container needs `tabindex="-1"`; Tabler uses `.modal-blur.fade` for the blurred backdrop. Size + centering both go on `.modal-dialog`. The status accent bar uses semantic `bg-danger`/`bg-success` etc.

---

## Offcanvas
**Base:** `.offcanvas` (`tabindex="-1"`) → `.offcanvas-header` (with `.offcanvas-title`) + `.offcanvas-body`
**Placement:** `.offcanvas-start` (left) `.offcanvas-end` (right) `.offcanvas-top` `.offcanvas-bottom`
**State:** `.show` (visible)
**Data/JS:**
- Trigger: `data-bs-toggle="offcanvas" data-bs-target="#id"`
- Close: `data-bs-dismiss="offcanvas"`
- Backdrop control: `data-bs-backdrop="false"`; allow body scroll: `data-bs-scroll="true"`
**Example:**
```html
<button class="btn" data-bs-toggle="offcanvas" data-bs-target="#oc">Open</button>

<div class="offcanvas offcanvas-end" tabindex="-1" id="oc">
  <div class="offcanvas-header">
    <h2 class="offcanvas-title">Settings</h2>
    <button class="btn-close" data-bs-dismiss="offcanvas" aria-label="Close"></button>
  </div>
  <div class="offcanvas-body">Panel content here.</div>
</div>
```
**Gotchas:** Placement class is required (no default). Use `data-bs-scroll="true"` + `data-bs-backdrop="false"` together for a non-blocking side panel.

---

## Toasts
**Base:** `.toast` → `.toast-header` + `.toast-body`; stack inside `.toast-container`
**State:** `.show` to display; A11y: `role="alert" aria-live="assertive" aria-atomic="true"`
**Data/JS:**
- `data-bs-toggle="toast"` (enable), `data-bs-autohide="false"` (don't auto-dismiss), `data-bs-delay="5000"` (ms before hide)
- Close button: `data-bs-dismiss="toast"`
**Positioning:** wrap in `.toast-container` with Bootstrap position utils, e.g. `position-fixed top-0 end-0 p-3`
**Example:**
```html
<div class="toast-container position-fixed top-0 end-0 p-3">
  <div class="toast show" role="alert" aria-live="assertive" aria-atomic="true"
       data-bs-autohide="false">
    <div class="toast-header">
      <strong class="me-auto">Notification</strong>
      <small class="text-secondary">just now</small>
      <button class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
    </div>
    <div class="toast-body">Your file has been uploaded.</div>
  </div>
</div>
```
**Gotchas:** Toasts are hidden by default — add `.show` (static demo) or trigger via JS `new bootstrap.Toast(el).show()`. Positioning is done by `.toast-container` + Bootstrap `position-fixed` utilities, not a Tabler-specific class. NOTE: the project uses **Notyf** for runtime toasts — prefer that for dynamic JS toasts; use this markup only for static/inline cases.

---

## Tooltips
**Base:** any element with `data-bs-toggle="tooltip"` + `title="..."`
**Attributes:**
- `data-bs-placement="top|bottom|left|right"`
- `title="text"` (the content; or `data-bs-title`)
- `data-bs-html="true"` (allow HTML in title)
- `data-bs-custom-class="..."` (Bootstrap 5.2+, custom styling)
**JS init (REQUIRED):** tooltips are opt-in — must be initialized:
```js
document.querySelectorAll('[data-bs-toggle="tooltip"]')
  .forEach(el => new bootstrap.Tooltip(el));
```
**Example:**
```html
<button class="btn" data-bs-toggle="tooltip" data-bs-placement="top" title="Helpful hint">
  Hover me
</button>
<button class="btn" data-bs-toggle="tooltip" data-bs-html="true" title="<b>Bold</b> text">HTML</button>
```
**Gotchas:** Won't appear without the JS init call. Default placement is `top`.

---

## Popovers
**Base:** any element with `data-bs-toggle="popover"`
**Attributes:**
- `data-bs-content="..."` (body), `title="..."` or `data-bs-title="..."` (header)
- `data-bs-placement="top|right|bottom|left"`
- `data-bs-trigger="click|hover|focus|manual"` (default click; combine e.g. `"focus"` to dismiss on next click)
- `data-bs-container="body"` (avoid clipping inside scroll containers)
- `data-bs-html="true"` (allow HTML)
**JS init (REQUIRED):**
```js
document.querySelectorAll('[data-bs-toggle="popover"]')
  .forEach(el => new bootstrap.Popover(el));
```
**Example:**
```html
<button class="btn" data-bs-toggle="popover" data-bs-placement="top"
        title="Popover title" data-bs-content="And here's the body content.">
  Click to toggle
</button>
<button class="btn btn-primary" data-bs-toggle="popover" data-bs-trigger="hover"
        data-bs-content="Shown on hover">Hover</button>
```
**Gotchas:** Like tooltips, requires JS init. Slug is singular `/components/popover` (plural 404s). Use `data-bs-container="body"` when inside cards/tables to prevent clipping.

---

## Placeholder (loading skeleton)
**Base:** `.placeholder` (skeleton block; often used with `col-*` for width)
**Animations (on wrapper):** `.placeholder-glow` (pulse) or `.placeholder-wave` (sweep)
**Sizes:** `.placeholder-xs` `.placeholder-sm` (default) `.placeholder-lg`
**Width:** Bootstrap grid `col-*` (e.g. `col-9`, `col-7`) sets the line width
**Color:** standard `bg-*` utilities (`bg-primary`, `bg-secondary`, ...)
**Specialized:**
- Avatar skeleton: `.avatar.placeholder` (+ size `.avatar-lg` etc.)
- Image skeleton: `.placeholder` on a `.ratio` element (e.g. `.ratio.ratio-21x9.placeholder`)
**Example:**
```html
<div class="card placeholder-glow">
  <div class="ratio ratio-21x9 card-img-top placeholder"></div>
  <div class="card-body">
    <div class="placeholder col-9 mb-3"></div>
    <div class="placeholder placeholder-xs col-10"></div>
    <a href="#" class="btn btn-primary disabled placeholder col-4" aria-hidden="true"></a>
  </div>
</div>
```
**Gotchas:** The animation class (`placeholder-glow`/`placeholder-wave`) goes on the PARENT wrapper, not the placeholder element. Width comes from `col-*`, not a width utility.

---

## Spinners (loaders)
**Base:** `.spinner-border` (rotating ring) or `.spinner-grow` (pulsing)
**Size:** `.spinner-border-sm` (small variant)
**Color:** text color utilities — `text-blue` `text-green` `text-red` ... (full named palette)
**A11y:** add `role="status"`
**Example:**
```html
<div class="spinner-border" role="status"></div>
<div class="spinner-border text-blue" role="status"></div>
<div class="spinner-border spinner-border-sm" role="status"></div>
<div class="spinner-grow text-green" role="status"></div>
<a href="#" class="btn btn-primary">
  <span class="spinner-border spinner-border-sm me-2" role="status"></span> Loading
</a>
```
**Gotchas:** Spinner color uses `text-COLOR` (not `bg-`). For full-overlay loading Tabler also has dimmer/`.loader` patterns elsewhere, but spinners here are inline. `.animated-dots` exists for a typing-dots effect.

---

## Progress (progress bars)
**Base:** `.progress` (track) → `.progress-bar` (fill)
**Sizes:** `.progress-sm` `.progress-lg` (on `.progress`)
**Color (on `.progress-bar`):** `bg-COLOR` — `bg-primary` `bg-success` `bg-danger` `bg-green` `bg-red` ... ; light fill `bg-COLOR-lt`
**Effects (on `.progress-bar`):** `.progress-bar-striped`, `.progress-bar-animated`, `.progress-bar-indeterminate` (no fixed width)
**Stacked/multiple:** put several `.progress-bar` in one `.progress`, or use `.progress-stacked` to combine multiple `.progress` elements
**Width:** inline `style="width: 65%"` on `.progress-bar`
**A11y (on `.progress-bar`):** `role="progressbar" aria-valuenow="65" aria-valuemin="0" aria-valuemax="100" aria-label="65% Complete"`
**Example:**
```html
<div class="progress">
  <div class="progress-bar bg-primary" style="width: 65%"
       role="progressbar" aria-valuenow="65" aria-valuemin="0" aria-valuemax="100">
  </div>
</div>
<div class="progress progress-sm">
  <div class="progress-bar bg-green progress-bar-striped progress-bar-animated" style="width: 40%"></div>
</div>
<div class="progress">
  <div class="progress-bar bg-primary progress-bar-indeterminate"></div>
</div>
```
**Gotchas:** Color and width go on `.progress-bar`, size on `.progress`. Indeterminate bar omits the `width` style. Progress uses semantic `bg-*` directly (no `-fg` needed — it's a background fill, no text).

---

## Ribbons
**Base:** `.ribbon` — must live inside a `position-relative` container (a `.card` is relative by default)
**Placement:** `.ribbon-top` `.ribbon-bottom` `.ribbon-start` (left) `.ribbon-end` (right); combine for corners, e.g. `.ribbon.ribbon-top.ribbon-start` (top-left)
**Style variant:** `.ribbon-bookmark` (bookmark/flag shape)
**Color:** `bg-COLOR` (e.g. `bg-red`, `bg-green`, `bg-orange`)
**Content:** text or a nested icon SVG
**Example:**
```html
<div class="card">
  <div class="ribbon ribbon-top ribbon-start bg-red">New</div>
  <div class="card-body">...</div>
</div>
<div class="card">
  <div class="ribbon ribbon-bookmark bg-orange"><!-- svg icon --></div>
  <div class="card-body">...</div>
</div>
```
**Gotchas:** Ribbon must be a child of a `position-relative` element or it positions off-screen. Default placement is top-right (`.ribbon` alone). Color is a plain `bg-*` (no `-fg`).

---

## Statuses (status indicators)
**Base:** `.status` (label with dot) — pair with a status color
**Color:** `.status-COLOR` — `status-green` `status-red` `status-blue` `status-yellow` `status-azure` `status-indigo` `status-purple` `status-pink` `status-orange` `status-lime` `status-teal` `status-cyan`
**Parts:**
- `.status-dot` — the colored dot
- `.status-dot-animated` — pulsing dot
- `.status-indicator` + `.status-indicator-circle` — multi-circle radar style; `.status-indicator-animated` to animate
- `.status-lite` — reduced-prominence variant
**Example:**
```html
<span class="status status-green">
  <span class="status-dot"></span> Online
</span>
<span class="status status-red">
  <span class="status-dot status-dot-animated"></span> Live
</span>
<span class="status-indicator status-blue status-indicator-animated">
  <span class="status-indicator-circle"></span>
  <span class="status-indicator-circle"></span>
  <span class="status-indicator-circle"></span>
</span>
```
**Gotchas:** Color goes on the `.status` wrapper (`status-green`), the dot just inherits it. `status-COLOR` uses the named palette, NOT semantic `success/danger`. The `.status` pill style differs from a `.badge` — use status for live/online state, badge for counts/labels.
