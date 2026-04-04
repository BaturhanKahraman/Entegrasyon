# Yönetim Grubu UI Refresh — Faz 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Yönetim grubundaki tüm sayfalarda design system kurallarını uygula — inline SVG→Tabler Icons, table-hover, card-table, wizard steps, duplicate başlık temizliği.

**Architecture:** Mekanik pattern-based değişiklikler. İş mantığına dokunulmaz. Mevcut HTMX pattern'leri korunur.

**Tech Stack:** ASP.NET Core MVC, Tabler UI, Tabler Icons (`ti ti-*`)

---

## Issue-Based Task Grouping

Dosya başına değil, sorun türü başına gruplandırılmıştır — aynı pattern'i tüm dosyalara uygulamak daha verimli.

### Task 1: Tüm tablolara `table-hover` ekle

**Files:** Yönetim grubundaki tüm .cshtml dosyaları

- [ ] **Step 1: Find and replace across all Yönetim feature views**

Her `class="table table-vcenter card-table"` → `class="table table-vcenter card-table table-hover"` olmalı.

Her `class="table table-vcenter"` (card-table olmayan) → `class="table table-vcenter table-hover"` olmalı.

Her `class="table card-table table-sm"` → `class="table card-table table-sm table-hover"` olmalı.

Sadece zaten `table-hover` olmayan tablolara ekle. Aşağıdaki feature klasörlerindeki tüm .cshtml dosyalarını tara:
Products, Categories, Attributes, Brands, POS, Sales, Orders, Shipping, BulkOperations, BranchOffices

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/
git commit -m "style(yonetim): add table-hover to all tables in management views

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Eksik `card-table` class'larını ekle

**Files (8 dosya):**
- `Features/Products/Views/Edit.cshtml` (line ~115)
- `Features/Products/Views/Partials/_CreateStep2.cshtml` (line ~21)
- `Features/Products/Views/Partials/_CreateStep3Review.cshtml` (line ~49)
- `Features/Products/Views/Partials/_DiscountDialog.cshtml` (line ~39)
- `Features/Categories/Views/Partials/_CreateStep2Attributes.cshtml` (line ~22)
- `Features/Categories/Views/Partials/_CreateStep3Review.cshtml` (line ~34)
- `Features/BulkOperations/Views/_ValidationPreview.cshtml` (line ~50)
- `Features/BranchOffices/Views/_StockTransferDialog.cshtml` (line ~42)

- [ ] **Step 1: Add card-table class to tables missing it**

For each file, find `table table-vcenter` (without `card-table`) and add `card-table`. For `table-sm` tables, add `card-table` as well.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/
git commit -m "style(yonetim): add missing card-table class to wizard and dialog tables

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Inline SVG → Tabler Icons (Products + Categories)

**Files (12 dosya):**
- `Features/Products/Views/Index.cshtml`
- `Features/Products/Views/Detail.cshtml`
- `Features/Products/Views/Edit.cshtml`
- `Features/Products/Views/Variants.cshtml`
- `Features/Products/Views/TrendyolSend.cshtml`
- `Features/Products/Views/Create.cshtml`
- `Features/Categories/Views/Index.cshtml`
- `Features/Categories/Views/Import.cshtml`
- `Features/Categories/Views/Partials/_CategoryNode.cshtml`
- `Features/Categories/Views/Partials/_CategoryDetail.cshtml`

- [ ] **Step 1: Replace inline SVGs with Tabler Icons**

Common SVG → Icon mapping:
- `icon-tabler-plus` / plus SVG → `<i class="ti ti-plus"></i>`
- `icon-tabler-trash` / trash SVG → `<i class="ti ti-trash"></i>`
- `icon-tabler-edit` / pencil SVG → `<i class="ti ti-pencil"></i>`
- `icon-tabler-download` / download SVG → `<i class="ti ti-download"></i>`
- `icon-tabler-upload` / upload SVG → `<i class="ti ti-upload"></i>`
- `icon-tabler-search` / search SVG → `<i class="ti ti-search"></i>`
- `icon-tabler-eye` / eye SVG → `<i class="ti ti-eye"></i>`
- `icon-tabler-chevron-right` → `<i class="ti ti-chevron-right"></i>`
- `icon-tabler-package` → `<i class="ti ti-package"></i>`
- `icon-tabler-file-import` → `<i class="ti ti-file-import"></i>`
- `icon-tabler-refresh` → `<i class="ti ti-refresh"></i>`
- `icon-tabler-send` → `<i class="ti ti-send"></i>`
- `icon-tabler-x` / close SVG → `<i class="ti ti-x"></i>`
- `icon-tabler-check` → `<i class="ti ti-check"></i>`
- Any other SVG → find equivalent at tabler-icons.io

For each file: find every `<svg ... class="icon ...">...</svg>` block and replace with the corresponding `<i class="ti ti-[name]"></i>`. Keep any surrounding `<span>` or button structure.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ Application/Entegrasyon.MVC/Features/Categories/
git commit -m "style(products,categories): replace inline SVGs with Tabler Icons

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Inline SVG → Tabler Icons (Remaining Features)

**Files (10 dosya):**
- `Features/Attributes/Views/Partials/_AttributeDetail.cshtml`
- `Features/Brands/Views/Index.cshtml`
- `Features/POS/Views/Index.cshtml`
- `Features/POS/Views/Partials/_POSCart.cshtml`
- `Features/POS/Views/Partials/_POSPaymentDialog.cshtml`
- `Features/Sales/Views/Index.cshtml`
- `Features/Sales/Views/Partials/_SaleCart.cshtml`
- `Features/Sales/Views/Partials/_SaleCustomerBadge.cshtml`
- `Features/Orders/Views/Index.cshtml`
- `Features/Orders/Views/Detail.cshtml`
- `Features/BulkOperations/Views/Index.cshtml`

- [ ] **Step 1: Replace inline SVGs with Tabler Icons**

Same mapping as Task 3. For each file, find and replace all `<svg>` blocks.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/
git commit -m "style(yonetim): replace inline SVGs with Tabler Icons in remaining views

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Wizard Steps → Tabler `steps steps-counter`

**Files (2 dosya):**
- `Features/Products/Views/Create.cshtml`
- `Features/Categories/Views/Create.cshtml`

- [ ] **Step 1: Replace wizard-steps with Tabler steps component**

In each file, find the `<div class="wizard-steps">` block and replace with:

```html
<div class="steps steps-counter mb-4">
    <a href="#" class="step-item active" data-step="1">Temel Bilgiler</a>
    <a href="#" class="step-item" data-step="2">Ozellikler</a>
    <a href="#" class="step-item" data-step="3">Onizleme</a>
</div>
```

Also update any JavaScript that toggles `wizard-step` classes to use `step-item` and `active` instead.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Products/ Application/Entegrasyon.MVC/Features/Categories/
git commit -m "style(products,categories): migrate wizard-steps to Tabler steps component

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Duplicate `page-pretitle` Temizliği

**Files:** 24 instance across all Yönetim views

- [ ] **Step 1: Remove duplicate page-pretitle blocks from views**

The layout already renders the page title via `ViewData.GetPageTitle()`. Each view that has its own `page-pretitle` + `page-title` block creates a duplicate. Remove these blocks but keep the `ViewData.SetPageTitle()` and `ViewData.SetActiveNav()` calls.

Pattern to find and remove:
```html
<div class="page-header d-print-none">
    <div class="container-xl">
        <div class="row align-items-center">
            <div class="col-auto">
                <span class="page-pretitle">...</span>
                <h2 class="page-title">...</h2>
            </div>
            ...
        </div>
    </div>
</div>
```

IMPORTANT: Some views have action buttons (like "Yeni Urun", "Excel'e Aktar") inside the page-header. These buttons should be preserved — move them to the top of the content area or into a card-header.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/
git commit -m "style(yonetim): remove duplicate page headers from management views

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Build + Test Doğrulaması

- [ ] **Step 1:** `dotnet build Entegrasyon.sln --verbosity minimal`
- [ ] **Step 2:** `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --verbosity minimal`
- [ ] **Step 3:** `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --verbosity minimal`
- [ ] **Step 4:** `dotnet test Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj --verbosity minimal`

---

### Task 8: Görsel Doğrulama

- [ ] **Step 1:** Start app: `cd Application/Entegrasyon.MVC && dotnet run`
- [ ] **Step 2:** Login and navigate to Products, Categories, Orders — verify no broken layouts
- [ ] **Step 3:** Check Products/Create wizard — verify Tabler steps component works
- [ ] **Step 4:** Toggle dark/light theme — verify tables and icons look correct in both
- [ ] **Step 5:** Fix any visual issues found
