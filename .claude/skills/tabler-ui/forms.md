# Tabler — Forms Reference

Source: https://docs.tabler.io/ui/forms/<slug>. URL slugs use a `form-` prefix
(e.g. `form-color-check`, `form-selectboxes`, `form-validation`). Tabler forms
build on Bootstrap form classes plus Tabler-specific extras.

---

## Form elements (text, textarea, select, check, radio, switch)
**Base:** `.form-label` (label), `.form-control` (text input / textarea / file),
`.form-select` (dropdown), `.form-check` + `.form-check-input` + `.form-check-label`
(checkbox/radio container), `.form-range` (slider).
**Variants/Modifiers:**
- Sizes: `.form-control-lg`, `.form-control-sm`
- Styles: `.form-control-rounded` (rounded corners), `.form-control-flush` (no borders)
- Switch: add `.form-switch` to the `.form-check` wrapper
- Inline checks: add `.form-check-inline` to each `.form-check`
**States/Validation:** use native `disabled` / `readonly` attributes. See Validation section.
**Data/JS:** none for plain elements.
**Example:**
```html
<!-- Text -->
<label class="form-label">Text</label>
<input type="text" class="form-control" placeholder="Your name" />

<!-- Textarea -->
<label class="form-label">Notes</label>
<textarea class="form-control" rows="3" placeholder="..."></textarea>

<!-- Select -->
<label class="form-label">Country</label>
<select class="form-select">
  <option>Turkey</option>
  <option>Germany</option>
</select>

<!-- Checkbox -->
<label class="form-check">
  <input class="form-check-input" type="checkbox" />
  <span class="form-check-label">Accept terms</span>
</label>

<!-- Radio (stacked) -->
<label class="form-check">
  <input class="form-check-input" type="radio" name="plan" />
  <span class="form-check-label">Monthly</span>
</label>

<!-- Switch -->
<label class="form-check form-switch">
  <input class="form-check-input" type="checkbox" />
  <span class="form-check-label">Enable notifications</span>
</label>

<!-- Range -->
<input type="range" class="form-range" min="0" max="100" />
```
**Gotchas:** Tabler wraps the input INSIDE the `<label class="form-check">` and uses
a `<span class="form-check-label">` (not a separate `for=`-linked label). The label
text is a sibling span AFTER the input.

---

## Input groups & addons
**Base:** `.input-group` wrapper; `.input-group-text` for static addon text/icons.
**Variants/Modifiers:**
- `.input-group-flat` — borderless / seamless addon styling
- Addon can be text, a `<button class="btn">`, a `.form-select`, or an icon
**Data/JS:** none.
**Example:**
```html
<!-- Static text addon (prefix) -->
<div class="input-group">
  <span class="input-group-text">@</span>
  <input type="text" class="form-control" placeholder="username" />
</div>

<!-- Suffix addon -->
<div class="input-group">
  <input type="text" class="form-control" placeholder="Amount" />
  <span class="input-group-text">₺</span>
</div>

<!-- With button -->
<div class="input-group">
  <input type="text" class="form-control" placeholder="Search" />
  <button class="btn" type="button">Go</button>
</div>

<!-- Flat variant -->
<div class="input-group input-group-flat">
  <span class="input-group-text">https://</span>
  <input type="text" class="form-control" />
</div>
```
**Gotchas:** Do not put margin classes on children; the group handles border-radius
collapsing automatically. Order in markup = visual order.

---

## Input with icon
**Base:** `.input-icon` wrapper; `.input-icon-addon` for the icon slot.
**Variants/Modifiers:** Icon defaults to leading (left). For trailing icon, place the
`.input-icon-addon` after the input and Tabler positions it right.
**Data/JS:** none (use a Tabler/inline SVG icon).
**Example:**
```html
<div class="input-icon">
  <input type="text" class="form-control" placeholder="Search…" />
  <span class="input-icon-addon">
    <!-- inline SVG icon here -->
  </span>
</div>
```
**Gotchas:** `.input-icon` is distinct from `.input-group`. Use `.input-icon` for a
decorative icon overlaid inside the field; use `.input-group` for functional addons.

---

## Color check (color input / swatch picker)
**Base:** `.form-colorinput` (wrapper `<label>`), `.form-colorinput-input`
(hidden radio/checkbox), `.form-colorinput-color` (visible swatch).
**Variants/Modifiers:**
- `.form-colorinput-light` — variant for light backgrounds
- Swatch color via bg utilities on `.form-colorinput-color`: `bg-dark`, `bg-white`,
  `bg-blue`, `bg-azure`, `bg-indigo`, `bg-purple`, `bg-pink`, `bg-red`, `bg-orange`,
  `bg-yellow`, `bg-lime`, `bg-green`
- Shape: add `rounded-circle` for circular swatch (omit for square)
- Native color picker alternative: `<input type="color" class="form-control form-control-color">`
**States/Validation:** uses checked state of the hidden input.
**Data/JS:** none.
**Example:**
```html
<!-- Swatch group -->
<label class="form-colorinput">
  <input name="color" type="radio" value="blue" class="form-colorinput-input" checked />
  <span class="form-colorinput-color bg-blue rounded-circle"></span>
</label>
<label class="form-colorinput">
  <input name="color" type="radio" value="red" class="form-colorinput-input" />
  <span class="form-colorinput-color bg-red rounded-circle"></span>
</label>

<!-- Native color picker -->
<input type="color" class="form-control form-control-color" value="#066fd1"
  title="Choose your color" />
```
**Gotchas:** Swatch color comes from `bg-*` utility classes, NOT inline `style`.
For arbitrary hex values use the native `type="color"` + `.form-control-color`.

---

## Form fieldset (grouping)
**Base:** `<fieldset class="form-fieldset">` wraps a logical group of inputs.
**Variants/Modifiers:** none specific; combine with `.mb-3` spacing on inner rows.
**States/Validation:** inherits from contained controls.
**Data/JS:** none.
**Example:**
```html
<fieldset class="form-fieldset">
  <div class="mb-3">
    <label class="form-label required">Full name</label>
    <input type="text" class="form-control" />
  </div>
  <div class="mb-3">
    <label class="form-label">Email</label>
    <input type="email" class="form-control" />
  </div>
  <label class="form-check">
    <input type="checkbox" class="form-check-input" />
    <span class="form-check-label required">I agree</span>
  </label>
</fieldset>
```
**Gotchas:** It is a real `<fieldset>` element (accessibility benefit). Add a
`<legend>` if a group caption is needed.

---

## Form helpers (hints, required, label description, help popover)
**Base:**
- `.form-hint` — helper text below an input
- `.form-label.required` — appends a red asterisk to the label
- `.form-label-description` — supplementary text inside a label (e.g. char count)
- `.form-help` — small `?` badge that opens a Bootstrap popover
**Data/JS:** `.form-help` uses Bootstrap popover attributes
(`data-bs-toggle="popover"`, `data-bs-placement`, `data-bs-html`, `data-bs-content`).
**Example:**
```html
<!-- Required label -->
<label class="form-label required">Email</label>
<input type="email" class="form-control" />
<div class="form-hint">We'll never share your email with anyone else.</div>

<!-- Label with description (char counter) -->
<label class="form-label">
  Bio <span class="form-label-description">56/100</span>
</label>
<textarea class="form-control"></textarea>

<!-- Label with help popover -->
<label class="form-label">
  ZIP Code
  <span class="form-help" data-bs-toggle="popover" data-bs-placement="top"
    data-bs-html="true" data-bs-content="<p>Your 5-digit postal code.</p>">?</span>
</label>
```
**Gotchas:** `.required` only renders the asterisk visually — still add the native
`required` attribute on the input for actual validation. `.form-help` requires
Bootstrap's popover JS to be initialized.

---

## Selectgroup (select boxes — button / pills / boxed)
**Base:** `.form-selectgroup` (container), `.form-selectgroup-item` (each label),
`.form-selectgroup-input` (hidden radio/checkbox), `.form-selectgroup-label`
(visible content). `.form-selectgroup-check` = visual check indicator for boxed style.
**Variants/Modifiers:**
- `.form-selectgroup-pills` — pill-shaped buttons
- `.form-selectgroup-boxes` — full boxed cards with check indicator
- Single vs multiple selection = use `type="radio"` vs `type="checkbox"` on inputs
**States/Validation:** driven by checked state of hidden input.
**Data/JS:** none.
**Example:**
```html
<!-- Button-style (radio, single select) -->
<div class="form-selectgroup">
  <label class="form-selectgroup-item">
    <input type="radio" name="size" value="s" class="form-selectgroup-input" checked />
    <span class="form-selectgroup-label">S</span>
  </label>
  <label class="form-selectgroup-item">
    <input type="radio" name="size" value="m" class="form-selectgroup-input" />
    <span class="form-selectgroup-label">M</span>
  </label>
</div>

<!-- Pills (checkbox, multi select) -->
<div class="form-selectgroup form-selectgroup-pills">
  <label class="form-selectgroup-item">
    <input type="checkbox" name="tags" value="new" class="form-selectgroup-input" />
    <span class="form-selectgroup-label">New</span>
  </label>
</div>

<!-- Boxed with check indicator -->
<div class="form-selectgroup form-selectgroup-boxes d-flex">
  <label class="form-selectgroup-item flex-fill">
    <input type="radio" name="plan" value="pro" class="form-selectgroup-input" />
    <div class="form-selectgroup-label d-flex align-items-center p-3">
      <span class="form-selectgroup-check"></span>
      Pro plan — ₺99/mo
    </div>
  </label>
</div>
```
**Gotchas:** For boxed variant the label is a `<div class="form-selectgroup-label">`
(not span) and must contain `.form-selectgroup-check`. Keep `name` consistent for
radio groups; use `checkbox` for multi-select.

---

## Image check (selectable image tiles)
**Base:** `.form-imagecheck` (wrapper `<label>`), `.form-imagecheck-input`
(hidden checkbox/radio), `.form-imagecheck-figure` (visual container),
`.form-imagecheck-image` (the `<img>`).
**Variants/Modifiers:** radio vs checkbox = only the input `type` differs; all
classes identical. (No `.form-imagecheck-caption` class in current docs.)
**States/Validation:** checked state of hidden input toggles the selected style.
**Data/JS:** none.
**Example:**
```html
<!-- Checkbox (multi) -->
<label class="form-imagecheck">
  <input type="checkbox" name="photo" value="1" class="form-imagecheck-input" />
  <span class="form-imagecheck-figure">
    <img src="/img/a.jpg" alt="" class="form-imagecheck-image" />
  </span>
</label>

<!-- Radio (single) -->
<label class="form-imagecheck">
  <input type="radio" name="avatar" value="2" class="form-imagecheck-input" />
  <span class="form-imagecheck-figure">
    <img src="/img/b.jpg" alt="" class="form-imagecheck-image" />
  </span>
</label>
```
**Gotchas:** The figure is a `<span>` (inline-block styled), not `<figure>`. Use
`radio` + shared `name` for single-pick galleries.

---

## Input mask
**Base:** `.form-control` input + `data-mask` attribute. Library: **IMask** (`imask`).
**Variants/Modifiers / Data/JS:**
- `data-mask="<pattern>"` — required, defines the mask (`0` = digit, `a` = letter,
  `*` = alphanumeric per IMask).
- `data-mask-visible="true"` — shows the mask placeholder characters as you type.
- Init: include IMask (`<script src="https://cdn.jsdelivr.net/npm/imask"></script>`)
  and Tabler wires `data-mask` automatically; for advanced patterns use IMask JS API.
**Example:**
```html
<!-- Phone -->
<input type="text" class="form-control" data-mask="(00) 0000-0000"
  data-mask-visible="true" placeholder="(00) 0000-0000" autocomplete="off" />

<!-- Date -->
<input type="text" class="form-control" data-mask="00/00/0000"
  data-mask-visible="true" placeholder="dd/mm/yyyy" autocomplete="off" />

<!-- Credit card -->
<input type="text" class="form-control" data-mask="0000 0000 0000 0000"
  data-mask-visible="true" placeholder="0000 0000 0000 0000" autocomplete="off" />

<!-- Number / currency (use IMask number type via JS for thousands sep) -->
<input type="text" class="form-control" data-mask="000 000 000"
  data-mask-visible="true" placeholder="0" autocomplete="off" />
```
**Gotchas:** Official docs only show the phone example; date/card/currency patterns
above follow IMask's `0`-digit convention. For currency with thousands separators or
decimals, initialize IMask in JS (`IMask(el, { mask: Number, thousandsSeparator: '.' })`)
rather than `data-mask`. Always add `autocomplete="off"`.
This project ships IMask in `wwwroot/lib/` (per project frontend libs).

---

## Validation states
**Base:** add `.is-valid` or `.is-invalid` to the control (`.form-control`,
`.form-select`, etc.). Feedback messages: `.valid-feedback` / `.invalid-feedback`
(shown when the adjacent control has the matching state class).
**Variants/Modifiers (Tabler-specific):**
- `.is-valid-lite` — subtle valid (tick only, no colored border)
- `.is-invalid-lite` — subtle invalid (cross only, no colored border)
  (combine with the base state class, e.g. `is-invalid is-invalid-lite`)
**States/Validation:** Bootstrap's `.was-validated` on the `<form>` triggers native
constraint-validation styling on submit (Bootstrap behavior; not Tabler-specific).
**Data/JS:** none required for static states; Bootstrap JS for `.was-validated` flow.
**Example:**
```html
<!-- Valid -->
<label class="form-label">City</label>
<input type="text" class="form-control is-valid" value="Ankara" />
<div class="valid-feedback">Looks good!</div>

<!-- Invalid -->
<label class="form-label required">City</label>
<input type="text" class="form-control is-invalid" required />
<div class="invalid-feedback">Please provide a valid city.</div>

<!-- Subtle (lite) -->
<input type="text" class="form-control is-valid is-valid-lite" />
<input type="text" class="form-control is-invalid is-invalid-lite" />

<!-- Invalid checkbox -->
<label class="form-check">
  <input class="form-check-input is-invalid" type="checkbox" required />
  <span class="form-check-label">Accept terms</span>
  <div class="invalid-feedback">You must agree.</div>
</label>
```
**Gotchas:** `.valid-feedback`/`.invalid-feedback` are hidden by default and only
display when a sibling control carries `.is-valid`/`.is-invalid` (or inside a
`.was-validated` form). For server-side (ASP.NET) validation just toggle the
`is-invalid` class + render `.invalid-feedback` directly. The `-lite` variants are
the Tabler addition over stock Bootstrap.
