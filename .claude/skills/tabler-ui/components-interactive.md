# Tabler — Interactive / JS-Plugin Components

Reference for Tabler UI components that wrap a third-party JS library. URL pattern: `https://docs.tabler.io/ui/components/<slug>`. Most init in `DOMContentLoaded`; many use the guard pattern `window.Lib && new Lib(...)`. Tabler ships vendored copies of these libs under `@tabler/core/dist/libs/<lib>/`.

---

## Autosize
**Base/classes:** `.form-control` on the `<textarea>`
**3rd-party lib:** autosize.js (`npm install autosize`)
**Init/Data:** Auto-init via attribute `data-bs-toggle="autosize"` — no JS call needed; library detects the attribute and grows the textarea height as the user types.
**Example:**
```html
<label class="form-label">Autosize example</label>
<textarea class="form-control" data-bs-toggle="autosize" placeholder="Type something…"></textarea>
```
**Gotchas:** autosize.js must be loaded for the attribute to do anything. Pure attribute-driven; no manual `autosize(el)` shown.

---

## Carousel
**Base/classes:** `.carousel`, `.carousel-slide` (slide transition) or `.carousel-fade` (fade), `.carousel-inner`, `.carousel-item` (`.active` on first), `.carousel-indicators`, `.carousel-control-prev` / `.carousel-control-next` (+ `.carousel-control-prev-icon` / `-next-icon`). Indicator variants: `.carousel-indicators-dot`, `.carousel-indicators-thumb`, `.carousel-indicators-vertical`.
**3rd-party lib:** Bootstrap 5 Carousel (Bootstrap JS bundle).
**Init/Data:** `data-bs-ride="carousel"` (autoplay), `data-bs-target="#id"` (link controls), `data-bs-slide-to="0"` (indicator index), `data-bs-slide="prev|next"` (arrows). No manual JS init needed.
**Example:**
```html
<div id="myCarousel" class="carousel slide" data-bs-ride="carousel">
  <div class="carousel-indicators">
    <button data-bs-target="#myCarousel" data-bs-slide-to="0" class="active"></button>
    <button data-bs-target="#myCarousel" data-bs-slide-to="1"></button>
  </div>
  <div class="carousel-inner">
    <div class="carousel-item active"><img class="d-block w-100" src="img1.jpg" alt=""></div>
    <div class="carousel-item"><img class="d-block w-100" src="img2.jpg" alt=""></div>
  </div>
  <a class="carousel-control-prev" data-bs-target="#myCarousel" role="button" data-bs-slide="prev">
    <span class="carousel-control-prev-icon"></span></a>
  <a class="carousel-control-next" data-bs-target="#myCarousel" role="button" data-bs-slide="next">
    <span class="carousel-control-next-icon"></span></a>
</div>
```
**Gotchas:** Bootstrap JS must be loaded. Exactly one `.carousel-item` needs `.active`.

---

## Charts
**Base/classes:** Chart target is a plain `<div id="chart-id" class="position-relative"></div>` inside `.card` > `.card-body`. No dedicated `.chart` class required — the ID is the hook.
**3rd-party lib:** ApexCharts (`npm install apexcharts`). Tabler uses ApexCharts for all charts.
**Init/Data:** Vanilla JS in `DOMContentLoaded`, guard with `window.ApexCharts`, `new ApexCharts(el, config).render()`.
**Example:**
```html
<div class="card"><div class="card-body">
  <div id="chart-demo-line" class="position-relative"></div>
</div></div>
<script>
document.addEventListener("DOMContentLoaded", function () {
  window.ApexCharts && (new ApexCharts(
    document.getElementById('chart-demo-line'),
    {
      chart: { type: 'line', fontFamily: 'inherit', height: 240, parentHeightOffset: 0, toolbar: { show: false }, animations: { enabled: false } },
      series: [{ name: 'Sales', data: [10, 30, 25, 40, 38] }],
      colors: ['var(--tblr-primary)'],
      tooltip: { theme: 'dark' },
      legend: { show: false }
    }
  )).render();
});
</script>
```
**Gotchas:** No extra CSS import. Use Tabler CSS vars for colors (`var(--tblr-primary)`, etc.). `chart.type`: line, area, bar, donut, heatmap. Common config: `series`, `labels`, `colors`, `tooltip.theme`, `legend`.

---

## Countup
**Base/classes:** None required — pure attribute on any element (e.g. `<h1>`).
**3rd-party lib:** countUp.js (`npm install countup.js`).
**Init/Data:** `data-countup` attribute; auto-animates when element enters viewport. Options passed as JSON in the attribute value.
**Example:**
```html
<h1 data-countup>30000</h1>
<h1 data-countup='{"duration":4,"prefix":"$","decimalPlaces":2}'>3000.50</h1>
```
**Options (JSON):** `duration` (s, def 2), `startVal` (def 0; > end = countdown), `decimalPlaces` (0), `useEasing` (true), `useGrouping` (true), `separator` (","), `decimal` ("."), `prefix` (""), `suffix` ("").
**Gotchas:** The element's text content is the target number. JSON in the attribute must be valid (mind quoting in Razor).

---

## Data grid
**Base/classes:** `.datagrid` (container), `.datagrid-item` (each pair), `.datagrid-title` (label), `.datagrid-content` (value).
**3rd-party lib:** None — pure CSS responsive grid of field/value pairs.
**Init/Data:** None.
**Example:**
```html
<div class="datagrid">
  <div class="datagrid-item">
    <div class="datagrid-title">Field Name</div>
    <div class="datagrid-content">Field Value</div>
  </div>
</div>
```
**CSS vars:** `--tblr-datagrid-item-width` (def `15rem`), `--tblr-datagrid-padding` (gap, def `1.5rem`).
**Gotchas:** Not a sortable/paginated table — it is a labelled key/value display. Content area accepts avatars, badges, form-check, icons. For interactive tabular data use a real `.table` or another lib.

---

## Dropzone
**Base/classes:** `.dropzone` on the `<form>`. Optional `.dz-message` > `.dropzone-msg-title` / `.dropzone-msg-desc` for custom text. `.fallback` wraps the no-JS `<input type="file">`.
**3rd-party lib:** Dropzone.js (must be loaded before init).
**Init/Data:** `new Dropzone("#id")` in `DOMContentLoaded`. `action` = upload endpoint; `autocomplete="off"`, `novalidate` recommended.
**Example:**
```html
<form class="dropzone" id="dropzone-default" action="/upload" autocomplete="off" novalidate>
  <div class="fallback"><input name="file" type="file" multiple /></div>
  <div class="dz-message">
    <h3 class="dropzone-msg-title">Drop files here or click to upload</h3>
    <span class="dropzone-msg-desc">Custom description</span>
  </div>
</form>
<script>
document.addEventListener("DOMContentLoaded", function () { new Dropzone("#dropzone-default"); });
</script>
```
**Gotchas:** Add `multiple` to the input for multi-file. `action` must point at a real upload handler (and include antiforgery if used in this project). Dropzone auto-uploads on drop by default.

---

## Icons
**Base/classes:** `.icon` on the `<svg>`. Stroke (outline) icons use `.icon-1`; filled icons use `.icon-2`. Animations: `.icon-pulse`, `.icon-tada`, `.icon-rotate`.
**3rd-party lib:** Tabler Icons (5000+, MIT) — used as inline SVG (primary method; not sprite/font in docs).
**Init/Data:** None — paste the SVG from tabler.io/icons.
**Example:**
```html
<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"
     fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"
     stroke-linejoin="round" class="icon icon-1">
  <path d="[path data]" />
</svg>

<!-- colored: wrap with a text-* utility, SVG uses currentColor -->
<span class="text-red">
  <svg class="icon icon-1" ...><path d="..."/></svg>
</span>
```
**Gotchas:** Color inheritance needs `stroke="currentColor"` (outline) or `fill="currentColor"` (filled). Size via `width`/`height` attrs (default 24×24). Filled icons want `fill="currentColor"`; outline icons want `fill="none"`.

---

## Inline player
**Base/classes:** No required class — target is a `<div id>` with provider data-attrs.
**3rd-party lib:** Plyr (HTML5/YouTube/Vimeo player). Vendored: `@tabler/core/dist/libs/plyr/dist/plyr.min.js`.
**Init/Data:** `data-plyr-provider="youtube|vimeo|html5"`, `data-plyr-embed-id="<id>"`. Init `new Plyr("#id")` in `DOMContentLoaded`, guarded by `window.Plyr`.
**Example:**
```html
<div id="player-youtube" data-plyr-provider="youtube" data-plyr-embed-id="dQw4w9WgXcQ"></div>
<script src="/lib/plyr/plyr.min.js"></script>
<script>
document.addEventListener("DOMContentLoaded", function () {
  window.Plyr && new Plyr("#player-youtube");
});
</script>
```
**Gotchas:** Vimeo uses `data-plyr-provider="vimeo"` with the numeric embed id. Theme color via CSS var `--plyr-color-main`. Needs Plyr's CSS too for proper controls.

---

## Range slider
**Base/classes:** None — target is an empty `<div id>`; noUiSlider injects its own markup/styles.
**3rd-party lib:** noUiSlider (`npm install nouislider`) — load its CSS + JS.
**Init/Data:** `noUiSlider.create(el, options)` in `DOMContentLoaded`, guarded by `window.noUiSlider`.
**Example:**
```html
<div id="range-simple"></div>
<script>
document.addEventListener("DOMContentLoaded", function () {
  window.noUiSlider && noUiSlider.create(document.getElementById("range-simple"), {
    start: 20,
    connect: [true, false],
    step: 10,
    range: { min: 0, max: 100 }
  });
});
</script>
```
**Gotchas:** noUiSlider CSS is required for the track/handles to render. Two-handle range = pass `start: [a, b]` and `connect: [false, true, false]`. Read value via the slider instance's `.get()`.

---

## Tracking
**Base/classes:** `.tracking` (container), `.tracking-block` (each status block). Status colors on the block: `.bg-success`, `.bg-danger`, `.bg-warning`; no class = "no data".
**3rd-party lib:** Bootstrap 5 (for tooltips + `.bg-*` utilities); Bootstrap JS needed for tooltips.
**Init/Data:** Per block: `data-bs-toggle="tooltip"`, `data-bs-placement="top"`, `title="<status>"`. Tooltips must be initialized (Bootstrap Tooltip on `[data-bs-toggle="tooltip"]`).
**Example:**
```html
<div class="tracking">
  <div class="tracking-block bg-success" data-bs-toggle="tooltip" data-bs-placement="top" title="Operational"></div>
  <div class="tracking-block bg-danger"  data-bs-toggle="tooltip" data-bs-placement="top" title="Downtime"></div>
  <div class="tracking-block"></div> <!-- no data -->
</div>
```
**Gotchas:** Bootstrap tooltips are opt-in — they require JS init (`new bootstrap.Tooltip(el)` for each, or a global initializer) or they won't show. Pure CSS bars otherwise. Good for uptime/status-history strips.

---

## Vector Maps
**Base/classes:** Wrap in aspect-ratio box: `.ratio` + `.ratio-4x3` > inner `<div>` > `<div id class="w-100 h-100">`.
**3rd-party lib:** jsVectorMap. Vendored: `@tabler/core/dist/libs/jsvectormap/dist/js/jsvectormap.min.js` + a map data file (e.g. `.../maps/js/jsvectormap-world.js`). Also needs jsVectorMap CSS.
**Init/Data:** `new jsVectorMap({ selector, map, ... })` in `DOMContentLoaded`; call `map.updateSize()` on window resize.
**Example:**
```html
<div class="ratio ratio-4x3"><div>
  <div id="map-empty" class="w-100 h-100"></div>
</div></div>
<script src="/lib/jsvectormap/jsvectormap.min.js"></script>
<script src="/lib/jsvectormap/maps/jsvectormap-world.js"></script>
<script>
document.addEventListener("DOMContentLoaded", function () {
  const map = new jsVectorMap({
    selector: '#map-empty',
    map: 'world',
    backgroundColor: 'transparent',
    regionStyle: { initial: { fill: 'var(--tblr-bg-surface-secondary)', stroke: 'var(--tblr-border-color)', strokeWidth: 2 } },
    zoomOnScroll: false,
    zoomButtons: false
  });
  window.addEventListener("resize", () => map.updateSize());
});
</script>
```
**Gotchas:** You MUST load both the library JS and the specific map data JS (`world`, `world_merc`, etc.) — map renders blank without the data file. Config: `series` (color scales/region data), `markers` (coordinates), `lines` (marker connections), `zoomOnScroll`, `zoomButtons`.

---

## WYSIWYG editor
**Base/classes:** None — target is a `<textarea id>`; editor replaces it.
**3rd-party lib:** HugeRTE (a TinyMCE-compatible fork). API mirrors TinyMCE: `hugeRTE.init(options)`.
**Init/Data:** `hugeRTE.init({ selector, ... })` in `DOMContentLoaded`.
**Example:**
```html
<textarea id="hugerte-mytextarea">Hello, <b>Tabler</b>!</textarea>
<script>
document.addEventListener("DOMContentLoaded", function () {
  hugeRTE.init({
    selector: '#hugerte-mytextarea',
    height: 300,
    menubar: false,
    statusbar: false,
    plugins: ['advlist','autolink','lists','link','image','charmap','preview','anchor','searchreplace','visualblocks','code','fullscreen','insertdatetime','media','table','help','wordcount'],
    toolbar: 'undo redo | formatselect | bold italic backcolor | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | removeformat',
    content_style: 'body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; font-size: 14px; }'
  });
});
</script>
```
**Gotchas:** Older Tabler docs/versions used TinyMCE — current docs use HugeRTE (`hugeRTE.init`, TinyMCE-syntax options). Dark mode via `skin: 'oxide-dark'` + `content_css: 'dark'`. Submitted form value comes from the underlying textarea (editor syncs back on submit).

---

## Quick reference table

| Component | Hook | 3rd-party lib | Auto-init? |
|---|---|---|---|
| Autosize | `data-bs-toggle="autosize"` | autosize.js | Yes (attr) |
| Carousel | `.carousel` + `data-bs-ride` | Bootstrap 5 | Yes (attr) |
| Charts | `#id .position-relative` | ApexCharts | No (`new ApexCharts`) |
| Countup | `data-countup` | countUp.js | Yes (viewport) |
| Data grid | `.datagrid` | none (CSS) | n/a |
| Dropzone | `form.dropzone` | Dropzone.js | No (`new Dropzone`) |
| Icons | `svg.icon` | Tabler Icons (inline SVG) | n/a |
| Inline player | `data-plyr-provider` | Plyr | No (`new Plyr`) |
| Range slider | `#id` (empty div) | noUiSlider | No (`noUiSlider.create`) |
| Tracking | `.tracking` / `.tracking-block` | Bootstrap (tooltip) | tooltip needs init |
| Vector Maps | `.ratio` + `#id` | jsVectorMap (+ map data) | No (`new jsVectorMap`) |
| WYSIWYG | `textarea#id` | HugeRTE | No (`hugeRTE.init`) |
