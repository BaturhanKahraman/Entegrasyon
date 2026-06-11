# Tabler — Base & Layout Reference

Class-name lookup for Tabler UI BASE (colors, typography) and LAYOUT (navbars, navs/tabs, page headers, page layouts) sections. Tabler builds on Bootstrap 5, so all Bootstrap utility classes (spacing `m-*`/`p-*`, flex, grid, `text-*` alignment, `fs-*`, `fw-*`) are also available. Docs URL pattern: `https://docs.tabler.io/ui/<section>/<slug>` — note slugs are PLURAL: `navbars`, `navs-tabs`, `page-layouts`, `page-headers`.

---

## Colors
**Base:** `.text-COLOR` (text color) · `.bg-COLOR` (solid background)
**Color names:** `blue` `azure` `indigo` `purple` `pink` `red` `orange` `yellow` `lime` `green` `teal` `cyan` (+ semantic `primary` `secondary` `success` `info` `warning` `danger` `light` `dark` `muted`)
**Variants/Modifiers:**
- `.bg-COLOR-lt` — LIGHT/tinted background. Self-contained: pairs a pale background with a readable dark-of-same-hue text automatically. Use for soft chips/badges/alerts. No extra text class needed.
- `.text-COLOR-fg` — FOREGROUND text color meant to sit ON a solid `.bg-COLOR` (correct contrast, usually white). Only meaningful together with the solid background.
- Gray scale: `.text-gray-N` / `.bg-gray-N` where N = `50 100 200 300 400 500 600 700 800 900 950`.
- Social: `.bg-facebook` `.bg-twitter` `.bg-x` `.bg-linkedin` `.bg-google` `.bg-youtube` `.bg-github` `.bg-instagram` `.bg-pinterest` `.bg-vimeo` `.bg-dribbble` `.bg-vk` `.bg-rss` `.bg-flickr` `.bg-bitbucket` (and matching `.text-*`).
**Color combos (CRITICAL — project strict rule):**
- SOLID = `.bg-COLOR` + `.text-COLOR-fg` → e.g. `badge bg-green text-green-fg`. A bare `.bg-green` alone does NOT guarantee readable text.
- LIGHT = `.bg-COLOR-lt` alone → e.g. `badge bg-green-lt`. The `-lt` class already includes the matching readable text color; do NOT add `-fg`.
**Data/JS:** none.
**Example:**
```html
<!-- solid chip: background + matching foreground -->
<span class="badge bg-blue text-blue-fg">Solid</span>
<!-- light/soft chip: single class, text auto-handled -->
<span class="badge bg-blue-lt">Soft</span>
<p class="text-red">Error text</p>
<div class="bg-gray-100 text-gray-700 p-2">muted panel</div>
```
**Gotchas:** Never write `.bg-green` and expect white text — add `.text-green-fg`. Never combine `-lt` with `-fg` (redundant/wrong). Prefer `-lt` for badges/alerts to satisfy contrast cheaply.

---

## Typography
**Base:** semantic `<h1>`–`<h6>`, `<p>`; class equivalents `.h1`–`.h6` (apply heading size to any element without changing the tag).
**Variants/Modifiers (Tabler-specific):**
- Text transform: `.text-uppercase` `.text-lowercase` `.text-capitalize`
- Letter spacing: `.tracking-tight` `.tracking-normal` `.tracking-wide`
- Line height: `.lh-1` `.lh-sm` `.lh-base` `.lh-lg`
- Antialiasing: `.antialiased` `.subpixel-antialiased`
- Markdown wrapper: `.markdown` — applies default prose styles to raw HTML/markdown content (headings, lists, tables, blockquotes inside it get styled automatically).
**Variants/Modifiers (Bootstrap 5, also available in Tabler):**
- Font size: `.fs-1`…`.fs-6`, `.small`, `.fs-h1`-style not used — use `.fs-1`..`.fs-6`.
- Font weight: `.fw-bold` `.fw-semibold` `.fw-medium` `.fw-normal` `.fw-light`
- Color/muted: `.text-muted` `.text-secondary` `.text-reset`
- Alignment: `.text-start` `.text-center` `.text-end` (responsive `.text-md-center` etc.)
- Overflow: `.text-truncate` `.text-nowrap` `.text-wrap` `.text-break`
- Lead/quote/lists: `.lead` `.blockquote` `.blockquote-footer` `.list-unstyled` `.list-inline` + `.list-inline-item`
**Data/JS:** none.
**Example:**
```html
<div class="page-pretitle text-uppercase tracking-wide text-muted">Section</div>
<h2 class="h1">Big heading on an h2 tag</h2>
<p class="lead">Intro paragraph.</p>
<p class="text-muted fs-4 lh-lg">Secondary copy.</p>
<div class="markdown">{!-- raw markdown/html rendered with prose styles --}</div>
```
**Gotchas:** `.h1`–`.h6` are SIZE classes, not tags — keep the semantically correct tag for a11y and add the class for visual size. Tabler docs page only lists transform/tracking/lh/antialias/markdown explicitly; `.fs-*`/`.fw-*`/`.text-muted`/alignment come from the underlying Bootstrap 5 and are safe to use.

---

## Navbars
**Base:** `.navbar` (on `<header>` for horizontal, `<aside>` for vertical).
**Variants/Modifiers:**
- Responsive expand: `.navbar-expand-sm` / `.navbar-expand-md` / `.navbar-expand-lg` — breakpoint at which the collapsed toggler expands to full menu.
- `.navbar-vertical` — sidebar layout (combine with an expand class, placed on `<aside>`).
- Theme: `data-bs-theme="dark"` attribute for dark navbar (Tabler's modern way) — older `.navbar-dark` / `.navbar-light` still seen.
- `.d-print-none` — hide navbar when printing.
- `.sticky-top` — Bootstrap sticky positioning.
**Inner structure classes:**
- `.navbar-brand` (logo/title link) · `.navbar-brand-autodark` (auto invert logo in dark mode)
- `.navbar-nav` (the `<ul>`) · `.nav-item` (each `<li>`, add `.active` for current) · `.nav-link` (the `<a>`)
- `.nav-link-icon` (icon span inside a link) · `.nav-link-title` (text label inside a link)
- `.navbar-toggler` + `.navbar-toggler-icon` (mobile hamburger button)
- `.navbar-collapse` (the collapsible region, id-targeted) · `.collapse` (Bootstrap collapse)
- `.container-xl` — width wrapper inside the navbar.
- Dropdowns: `.dropdown` → `.nav-link.dropdown-toggle` → `.dropdown-menu` (`.dropdown-menu-end` right-align, `.dropdown-menu-arrow` arrow, `.dropdown-divider`, `.dropdown-item`).
**Data/JS:** toggler uses `data-bs-toggle="collapse"` + `data-bs-target="#navbar-menu"`. Dropdowns use `data-bs-toggle="dropdown"`.
**Example:**
```html
<header class="navbar navbar-expand-md d-print-none">
  <div class="container-xl">
    <button class="navbar-toggler" type="button"
            data-bs-toggle="collapse" data-bs-target="#navbar-menu">
      <span class="navbar-toggler-icon"></span>
    </button>
    <a href="/" class="navbar-brand navbar-brand-autodark">Logo</a>
    <div class="collapse navbar-collapse" id="navbar-menu">
      <ul class="navbar-nav">
        <li class="nav-item active">
          <a class="nav-link" href="/"><span class="nav-link-title">Home</span></a>
        </li>
        <li class="nav-item dropdown">
          <a class="nav-link dropdown-toggle" href="#" data-bs-toggle="dropdown">More</a>
          <div class="dropdown-menu dropdown-menu-end">
            <a class="dropdown-item" href="#">Action</a>
          </div>
        </li>
      </ul>
    </div>
  </div>
</header>
```
**Gotchas:** `data-bs-target` value must match the collapse element's `id` (with `#`). Active state goes on `.nav-item.active`, not on the link. Vertical sidebar = `.navbar.navbar-vertical` on `<aside>`, still needs an `.navbar-expand-*`.

---

## Navs and tabs
**Base:** `.nav` (on a `<ul>` or `<div>`), each item `.nav-item`, each link `.nav-link`.
**Variants/Modifiers:**
- Style: `.nav-tabs` (classic tabs) · `.nav-pills` (pill buttons) · `.nav-underline` (underline active).
- Layout: `.flex-column` (vertical nav) · Bootstrap `.nav-fill` (equal-area items) · `.nav-justified` (equal-width items).
- States: `.nav-link.active` (current) · `.nav-link.disabled` (non-clickable).
- Dropdown inside tabs: `.nav-item.dropdown` → `.nav-link.dropdown-toggle` → `.dropdown-menu` / `.dropdown-item`.
- Tab panels (Bootstrap): `.tab-content` wrapper → `.tab-pane` per panel; active panel gets `.active.show`.
**Data/JS (JS-driven tabs):** trigger link uses `data-bs-toggle="tab"` (or `"pill"`) + `data-bs-target="#pane-id"` (or `href="#pane-id"`), `role="tab"`. Panels use `role="tabpanel"`.
**Example:**
```html
<ul class="nav nav-tabs" role="tablist">
  <li class="nav-item" role="presentation">
    <a class="nav-link active" data-bs-toggle="tab" href="#tab-1" role="tab">Tab 1</a>
  </li>
  <li class="nav-item" role="presentation">
    <a class="nav-link" data-bs-toggle="tab" href="#tab-2" role="tab">Tab 2</a>
  </li>
  <li class="nav-item">
    <a class="nav-link disabled" href="#">Disabled</a>
  </li>
</ul>
<div class="tab-content">
  <div class="tab-pane active show" id="tab-1" role="tabpanel">Panel 1</div>
  <div class="tab-pane" id="tab-2" role="tabpanel">Panel 2</div>
</div>

<!-- vertical -->
<ul class="nav nav-pills flex-column">
  <li class="nav-item"><a class="nav-link active" href="#">Active</a></li>
</ul>
```
**Gotchas:** The initially shown pane needs BOTH `.active` and `.show`; the matching tab link needs `.active`. `data-bs-target`/`href` must match the pane `id`. `.nav-fill` vs `.nav-justified`: fill = proportional to content, justified = strictly equal width. Use `.nav-tabs` for card tab headers (often together with `.card-header`).

---

## Page headers
**Base:** `.page-header` (the header block at the top of `.page-body`, or directly in `.page-wrapper`).
**Variants/Modifiers:**
- `.page-header-border` — adds a bottom border separator.
- Typography children: `.page-pretitle` (small uppercase label above title), `.page-title` (main `<h2>`), `.page-subtitle` (descriptive line below title).
- Layout helpers: `.row` `.align-items-center` (vertical-center row) · `.col` (flexible title area) · `.col-auto` (auto-width, e.g. avatar or action group) · `.ms-auto` (push actions right) · `.btn-list` (wraps action buttons with spacing).
**Data/JS:** none.
**Example:**
```html
<div class="page-header page-header-border d-print-none">
  <div class="container-xl">
    <div class="row g-2 align-items-center">
      <div class="col">
        <div class="page-pretitle">Overview</div>
        <h2 class="page-title">Dashboard</h2>
      </div>
      <div class="col-auto ms-auto d-print-none">
        <div class="btn-list">
          <a href="#" class="btn btn-primary">New</a>
        </div>
      </div>
    </div>
  </div>
</div>
```
**Gotchas:** `.page-title` is normally an `<h2>`, not `<h1>`. Right-align actions with `.col-auto.ms-auto` (not just `.col-auto`). Wrap header content in `.container-xl` to align with the page body width. `.page-pretitle` already renders small/uppercase/muted — don't double up the transform classes.

---

## Page layouts
**Base skeleton:** `.page` (root) → navbar(s) → `.page-wrapper` → `.page-body` → `.container-xl` (content width). `.page-header` lives inside `.page-wrapper` (above or alongside `.page-body`).
**Variants/Modifiers:**
- `.page-wrapper` — main content column to the right of / below the navbar.
- `.page-body` — scrollable main content region (usually contains `.container-xl` + `.row`).
- `.page-header` — optional header band inside the wrapper.
- Width wrapper: `.container-xl` (boxed max-width) vs Bootstrap `.container-fluid` (full width).
- `.page-center` — centers a single card vertically+horizontally (login/error/empty full-screen pages).
- Card grid spacing inside `.page-body`: `.row.row-cards` (gap between cards) · `.row.row-deck` (equal-height cards).
- Dark sidebar: `data-bs-theme="dark"` on the vertical navbar.
**Two canonical layouts:**
1. Horizontal navbar (top) — `<header class="navbar navbar-expand-md">` then `.page-wrapper`.
2. Vertical sidebar — `<aside class="navbar navbar-vertical navbar-expand-lg">` then `.page-wrapper`.
**Data/JS:** none for the skeleton (navbar collapse/dropdown JS as in the Navbars section).
**Example — sidebar layout:**
```html
<div class="page">
  <aside class="navbar navbar-vertical navbar-expand-lg" data-bs-theme="dark">
    <div class="container-fluid"><!-- brand + navbar-nav --></div>
  </aside>
  <div class="page-wrapper">
    <div class="page-header">
      <div class="container-xl"><h2 class="page-title">Page</h2></div>
    </div>
    <div class="page-body">
      <div class="container-xl">
        <div class="row row-cards"><!-- cards --></div>
      </div>
    </div>
  </div>
</div>
```
**Example — horizontal layout:**
```html
<div class="page">
  <header class="navbar navbar-expand-md d-print-none"><!-- ... --></header>
  <div class="page-wrapper">
    <div class="page-body">
      <div class="container-xl"><!-- content --></div>
    </div>
  </div>
</div>
```
**Example — centered (login/error):**
```html
<div class="page page-center">
  <div class="container container-tight py-4"><!-- single card --></div>
</div>
```
**Gotchas:** `.page-wrapper` must be a sibling of the navbar inside `.page` — not nested inside the navbar. Use the SAME width wrapper (`.container-xl`) in both `.page-header` and `.page-body` so the title lines up with the content. For full-screen single-card pages use `.page.page-center` + `.container-tight`, not the wrapper/body skeleton. `.row-cards`/`.row-deck` go on the `.row`, not on `.page-body`.
