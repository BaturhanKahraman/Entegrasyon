# Tabler — Navigation & Structure Components

Reference for navigation/structure Tabler UI components. Source: https://docs.tabler.io/ui/components/<slug>. Tabler is Bootstrap 5-based — Bootstrap utility/component classes (`.table-striped`, `.justify-content-center`, `.ms-auto`, etc.) work alongside Tabler classes.

Color tokens used throughout: `primary secondary success info warning danger light dark` plus the extended palette `blue azure indigo purple pink red orange yellow lime green teal cyan`. Solid color = `bg-COLOR text-COLOR-fg`; light variant = `bg-COLOR-lt` (auto-pairs text). Text color = `text-COLOR`.

---

## Breadcrumb
**Base:** `ol.breadcrumb` (ordered list)
**Variants/Modifiers:** `.breadcrumb-dots` (dot separators), `.breadcrumb-arrows` (arrow separators), `.breadcrumb-bullets` (bullet separators), `.breadcrumb-muted` (muted styling). Default separator = slash.
**Items:** `li.breadcrumb-item`; current page = `li.breadcrumb-item.active`.
**Color combos:** none (separator/muted only).
**Data/JS:** none. A11y: `aria-label="breadcrumbs"` on `<ol>`, `aria-current="page"` on active `<li>`.
**Example:**
```html
<ol class="breadcrumb breadcrumb-arrows" aria-label="breadcrumbs">
  <li class="breadcrumb-item"><a href="#">Home</a></li>
  <li class="breadcrumb-item"><a href="#">Library</a></li>
  <li class="breadcrumb-item active" aria-current="page"><a href="#">Data</a></li>
</ol>
```
**Gotchas:** Use `<ol>` not `<ul>`. Active item still wrapped in `<a>` in Tabler examples. Icons: put `<svg>` inside the `<a>` (no special class).

---

## Buttons
**Base:** `.btn` (on `<button>` or `<a>`).
**Color (solid):** `.btn-primary .btn-secondary .btn-success .btn-info .btn-warning .btn-danger .btn-dark .btn-light` AND full palette `.btn-blue .btn-azure .btn-indigo .btn-purple .btn-pink .btn-red .btn-orange .btn-yellow .btn-lime .btn-green .btn-teal .btn-cyan`.
**Outline:** `.btn-outline-COLOR` (e.g. `.btn-outline-primary`, `.btn-outline-danger`).
**Ghost** (transparent, color text/hover): `.btn-ghost-COLOR` (e.g. `.btn-ghost-secondary`).
**Sizes:** `.btn-xs .btn-sm .btn-lg .btn-xl`.
**Shape:** `.btn-pill` (fully rounded), `.btn-square` (no radius).
**Icon-only:** `.btn-icon` (square, centers a single icon — drop label text). Action icon: `.btn-action`.
**Link style:** `.btn-link` (looks like a link).
**Loading:** `.btn-loading` (shows spinner, hides text; keep text in markup).
**Full width:** add `.w-100`.
**Disabled:** `disabled` attribute on `<button>`, or `.disabled` class on `<a>`.
**Layout helpers:** `.btn-list` (flex container, spaces a row of buttons), `.btn-actions` (action button group). Bootstrap groups: `.btn-group`, `.btn-group-vertical`.
**Social:** `.btn-facebook .btn-twitter .btn-github` etc. (brand-colored).
**Icon animation:** `.btn-animate-icon` + one of `-rotate -shake -pulse -tada -move-start` (e.g. `.btn-animate-icon-rotate`).
**Data/JS:** none required for styling; `.btn-loading` is pure CSS.
**Example:**
```html
<div class="btn-list">
  <button class="btn btn-primary">Save</button>
  <a href="#" class="btn btn-outline-secondary">Cancel</a>
  <button class="btn btn-ghost-danger btn-sm">Delete</button>
  <button class="btn btn-icon btn-azure" aria-label="Add"><svg class="icon">…</svg></button>
  <button class="btn btn-success btn-loading">Submitting</button>
  <button class="btn btn-pill btn-teal">Pill</button>
</div>
```
**Gotchas:** `.btn-icon` requires `aria-label` for a11y (no text). For `.btn-loading` keep the original label inside so width stays stable. Outline/ghost do NOT take a separate `bg-` class.

---

## Dropdowns
**Base:** `.dropdown` (wrapper) + `.dropdown-toggle` trigger (`.btn .dropdown-toggle`) + `.dropdown-menu` (panel).
**Items:** `.dropdown-item` (links/buttons); `.dropdown-header` (section label); `.dropdown-divider` (`<div>` separator); icon inside item via `.dropdown-item-icon` on the `<svg>`.
**Menu modifiers:** `.dropdown-menu-arrow` (pointer arrow), `.dropdown-menu-card` (render menu body as a card), `.dropdown-menu-end` (right-align), dark menu = `.dropdown-menu.bg-dark.text-white`.
**Direction (Bootstrap):** `.dropup .dropend .dropstart` on the `.dropdown` wrapper.
**Item states:** `.active`, `.disabled`.
**Color combos:** badges in items via `.badge.ms-auto` (right-aligned).
**Data/JS:** `data-bs-toggle="dropdown"` on the toggle. `data-bs-auto-close="true|inside|outside|false"` controls close behavior. `aria-expanded`, `aria-haspopup` for a11y.
**Example:**
```html
<div class="dropdown">
  <button class="btn dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false">Menu</button>
  <div class="dropdown-menu dropdown-menu-end dropdown-menu-arrow">
    <span class="dropdown-header">Section</span>
    <a class="dropdown-item active" href="#"><svg class="dropdown-item-icon">…</svg>Action</a>
    <a class="dropdown-item disabled" href="#">Disabled</a>
    <div class="dropdown-divider"></div>
    <a class="dropdown-item" href="#">More <span class="badge bg-red-lt ms-auto">3</span></a>
  </div>
</div>
```
**Gotchas:** Requires Bootstrap JS bundle. `.dropdown-menu-end` aligns to right edge of toggle. Use `data-bs-auto-close="outside"` for menus with forms/checkboxes inside.

---

## Pagination
**Base:** `ul.pagination` > `li.page-item` > `a.page-link`.
**States:** `.page-item.active` (current), `.page-item.disabled` (non-clickable prev/next).
**Variants:** `.pagination-outline` (subtle outline), `.pagination-circle` (round buttons), combinable: `.pagination.pagination-circle.pagination-outline`. Text labels via `.page-text` (e.g. "Showing 1 to 10").
**Sizes (Bootstrap):** `.pagination-sm`, `.pagination-lg`.
**Alignment (Bootstrap utils on `ul`):** `.justify-content-center`, `.justify-content-end`.
**Color combos:** none.
**Data/JS:** none. Disabled prev/next use `tabindex="-1"` + `aria-disabled="true"`. Ellipsis = a `.page-item.disabled` with `&hellip;`.
**Example:**
```html
<ul class="pagination justify-content-center">
  <li class="page-item disabled"><a class="page-link" href="#" tabindex="-1" aria-disabled="true">
    <svg class="icon">…</svg> prev</a></li>
  <li class="page-item"><a class="page-link" href="#">1</a></li>
  <li class="page-item active"><a class="page-link" href="#">2</a></li>
  <li class="page-item disabled"><span class="page-link">&hellip;</span></li>
  <li class="page-item"><a class="page-link" href="#">Next <svg class="icon">…</svg></a></li>
</ul>
```
**Gotchas:** Prev/next icon links are the Tabler default — wrap SVG inside `.page-link`.

---

## Steps
**Base:** `.steps` (container) > `.step-item` (each step; `<a>`, `<span>`, or `<div>`).
**State:** `.step-item.active` (current — also colors all preceding steps as done).
**Color variants:** `.steps-COLOR` on container, e.g. `.steps-green .steps-red .steps-blue` (any palette color).
**Modifiers:** `.steps-counter` (numeric circles instead of inline labels).
**Color combos:** color applied via `.steps-COLOR` only.
**Data/JS:** static; optional tooltip per step via `data-bs-toggle="tooltip" data-bs-placement="top" title="…"`.
**Example:**
```html
<div class="steps steps-counter steps-green">
  <a href="#" class="step-item">Order</a>
  <a href="#" class="step-item active">Payment</a>
  <span class="step-item">Delivery</span>
</div>
```
**Gotchas:** No documented `.steps-vertical` — docs show horizontal only. `.steps-counter` items may be empty (number is auto-generated).

---

## Tabs
**Base:** `ul.nav.nav-tabs` > `li.nav-item` > `a.nav-link` (tab triggers) + `.tab-content` > `.tab-pane` (panels).
**Variants:** `.nav-pills` (pill style), `.nav-fill` (expand to fill width), `.nav-justified` (equal width). In cards: `.nav-tabs.card-header-tabs` placed in `.card-header`. Vertical: put `.nav` items in a flex column (`.flex-column`) alongside content.
**States:** active trigger = `.nav-link.active`; active panel = `.tab-pane.active.show`; disabled trigger = `.nav-link.disabled`.
**Color combos:** none.
**Data/JS:** `data-bs-toggle="tabs"` on the `<ul>`; `data-bs-toggle="tab"` on each trigger (`href`/`data-bs-target` points to panel `#id`). A11y: `role="tablist"`, `role="tab"`, `role="tabpanel"`, `aria-selected`.
**Example:**
```html
<ul class="nav nav-tabs" data-bs-toggle="tabs" role="tablist">
  <li class="nav-item" role="presentation">
    <a href="#tab-1" class="nav-link active" data-bs-toggle="tab" role="tab">Home</a></li>
  <li class="nav-item" role="presentation">
    <a href="#tab-2" class="nav-link" data-bs-toggle="tab" role="tab">Profile</a></li>
  <li class="nav-item ms-auto">  <!-- right-pushed item -->
    <a href="#tab-3" class="nav-link" data-bs-toggle="tab" role="tab"><svg class="icon">…</svg></a></li>
</ul>
<div class="tab-content">
  <div id="tab-1" class="tab-pane active show" role="tabpanel">Home content</div>
  <div id="tab-2" class="tab-pane" role="tabpanel">Profile content</div>
  <div id="tab-3" class="tab-pane" role="tabpanel">Settings</div>
</div>
```
**Gotchas:** Active panel needs BOTH `.active.show`. `data-bs-toggle="tabs"` (plural) on container is Tabler convenience; per-link is `"tab"` (singular). Requires Bootstrap JS.

---

## Timelines
**Base:** `ul.timeline` > `li.timeline-event`.
**Parts:** `.timeline-event-icon` (icon bubble), `.timeline-event-card` (combined with `.card` for the content box), inner `.card-body`. Time/meta typically `.text-secondary.float-end` inside the card.
**Variants:** `.timeline-simple` on `<ul>` (simplified, no card boxes).
**Color combos:** on `.timeline-event-icon` use solid `bg-COLOR text-COLOR-fg` or light `bg-COLOR-lt` (e.g. `bg-green-lt`, `bg-x-lt`, `bg-facebook-lt`).
**Data/JS:** none.
**Example:**
```html
<ul class="timeline">
  <li class="timeline-event">
    <div class="timeline-event-icon bg-green-lt"><svg class="icon">…</svg></div>
    <div class="card timeline-event-card">
      <div class="card-body">
        <div class="text-secondary float-end">10 hrs ago</div>
        <h4>Order shipped</h4>
        <p class="text-secondary">Tracking #ABC123 created.</p>
      </div>
    </div>
  </li>
</ul>
```
**Gotchas:** `.timeline-event-card` is meant to be combined with `.card`. Use `-lt` icon backgrounds for subtle look; solid needs the `text-COLOR-fg` pair.

---

## Divider
**Base:** `.hr-text` (horizontal rule with centered text label — on a `<div>`).
**Position variants:** `.hr-text-start` (left), default centered, `.hr-text-end` (right).
**Color variants:** add `.text-COLOR` (e.g. `.hr-text.text-green`, `.hr-text.text-primary`).
**Color combos:** via `text-COLOR` util only.
**Data/JS:** none.
**Example:**
```html
<div class="hr-text">or</div>
<div class="hr-text hr-text-start text-primary"><svg class="icon">…</svg> Section</div>
<hr class="my-3">  <!-- plain rule -->
```
**Gotchas:** Docs cover only horizontal text dividers — no documented vertical divider class. Icon: place `<svg>` inside the `.hr-text` element. Plain line = Bootstrap `<hr>`.

---

## Segmented Control
**Base:** `nav.nav.nav-segmented` (Bootstrap tab plugin under the hood — no custom `.segmented-control` class) > `button.nav-link`.
**States:** active = `.nav-link.active`; disabled = `disabled` attribute.
**Variants/Modifiers:** `.nav-segmented-vertical` (vertical), sizes `.nav-sm` / `.nav-lg`, full width `.w-100`.
**Color combos:** none.
**Data/JS:** `role="tablist"` on nav, `role="tab"` + `data-bs-toggle="tab"` + `aria-selected="true|false"` on each button (when wired to panels). Without panels it's a plain styled segmented selector.
**Example:**
```html
<nav class="nav nav-segmented w-100" role="tablist">
  <button class="nav-link active" role="tab" aria-selected="true">Day</button>
  <button class="nav-link" role="tab" aria-selected="false">Week</button>
  <button class="nav-link" role="tab" aria-selected="false" disabled>Month</button>
</nav>
```
**Gotchas:** It is `.nav.nav-segmented`, NOT a standalone `.segmented-control`. To switch content, wire it like tabs with `data-bs-toggle="tab"` + `.tab-content`.

---

## Switch Icon
**Base:** `button.switch-icon` containing `.switch-icon-a` (state 1) and `.switch-icon-b` (state 2), each wrapping an `<svg>`.
**State:** add `.active` to `.switch-icon` to show icon B (toggled).
**Animation variants:** `.switch-icon-fade .switch-icon-scale .switch-icon-flip .switch-icon-slide-up .switch-icon-slide-left .switch-icon-slide-down .switch-icon-slide-end`.
**Color combos:** color each span independently (e.g. `.switch-icon-a.text-secondary`, `.switch-icon-b.text-red`).
**Data/JS:** `data-bs-toggle="switch-icon"` on the button enables auto-toggle of `.active`.
**Example:**
```html
<button class="switch-icon switch-icon-flip" data-bs-toggle="switch-icon" aria-label="Toggle">
  <span class="switch-icon-a text-secondary"><svg class="icon">…</svg></span>
  <span class="switch-icon-b text-red"><svg class="icon">…</svg></span>
</button>
```
**Gotchas:** Needs Tabler JS for the toggle. Provide `aria-label` (icon-only button).

---

## Tables
**Base:** `.table` (on `<table>`).
**Tabler-specific variants:** `.table-vcenter` (vertically center cells), `.table-nowrap` (no wrap), `.card-table` (table flush inside a `.card`, no double borders/padding), `.table-mobile-md` / `.table-mobile-lg` (stack to cards below breakpoint).
**Bootstrap-inherited (work in Tabler):** `.table-striped`, `.table-hover`, `.table-bordered`, `.table-borderless`, `.table-sm` (compact).
**Responsive wrapper:** `.table-responsive` (+ breakpoint `.table-responsive-sm|-md|-lg|-xl`) — wrap the `<table>` in a `<div>`.
**Row/cell color (on `<tr>` or `<td>`):** `.table-active` plus color rows `.table-primary .table-secondary .table-success .table-danger .table-warning .table-info .table-light .table-dark`.
**Sticky header:** `.sticky-top` on `<thead>`.
**Sortable columns:** `.table-sort` on `<th>` (Tabler datatable / list.js style; pair with `data-sort` value and asc/desc state class). Sort UI typically driven by Tabler's `Sortable`/list.js or a datatable lib.
**Width helper:** `.w-1` on a `<th>`/`<td>` shrinks the column to content (useful for checkbox/avatar/actions columns).
**Header style:** Tabler headers use uppercase muted text by default; `<th>` content commonly wrapped/styled, body muted cells use `.text-secondary`.
**Color combos:** row color classes above; badges/avatars/progress placed inside `<td>` (e.g. `<span class="badge bg-success-lt">`, `<div class="progress progress-sm">`).
**Data/JS:** static by default. Selectable rows = a checkbox column (`<input class="form-check-input">`); sorting/filtering needs a JS lib (list.js / datatables).
**Example (card table, striped, hover, responsive, selectable + sortable header):**
```html
<div class="card">
  <div class="table-responsive">
    <table class="table table-vcenter card-table table-striped table-hover">
      <thead>
        <tr>
          <th class="w-1"><input class="form-check-input m-0 align-middle" type="checkbox"></th>
          <th class="table-sort" data-sort="name">Name</th>
          <th>Status</th>
          <th class="w-1"></th>
        </tr>
      </thead>
      <tbody>
        <tr class="table-active">
          <td><input class="form-check-input m-0 align-middle" type="checkbox"></td>
          <td>Acme Inc.</td>
          <td><span class="badge bg-success-lt">Active</span></td>
          <td><a href="#" class="btn btn-ghost-secondary btn-icon"><svg class="icon">…</svg></a></td>
        </tr>
      </tbody>
    </table>
  </div>
</div>
```
**Gotchas:** Tabler's own tables page documents `.table-vcenter`, `.table-nowrap`, `.table-responsive`, color rows, `.sticky-top`, `.w-1` — but striped/hover/bordered are inherited from Bootstrap and render correctly. Use `.card-table` (not plain `.table`) when the table sits directly inside a `.card` so edges align. `.table-sort` is presentational; actual sorting requires JS. For selectable rows there is no `.table-selected` class — add a checkbox column and toggle a row highlight class yourself.
