# Dashboard UI Refresh — Faz 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dashboard'u Tabler UI bileşenleriyle iyileştir — stat kartlarına renkli ikon + trend göstergesi, grafiklere tema uyumlu renkler, QuickActions'a Tabler Icons, duplicate başlık düzeltmesi.

**Architecture:** Mevcut HTMX lazy-loading pattern korunur. Sadece partial view'lar ve DTO'lar güncellenir. Grafikler `data-apex-chart` attribute'ü ile tema hook'una bağlanır. İş mantığına dokunulmaz.

**Tech Stack:** ASP.NET Core MVC, Tabler UI, ApexCharts, HTMX, Tabler Icons (`ti ti-*`)

**Spec:** `docs/superpowers/specs/2026-04-04-tabler-combo-layout-ui-refresh-design.md` — Bölüm 3.1 (Stat Cards) ve 3.7 (Charts)

---

### Task 1: Dashboard Index — Duplicate Başlık Düzeltmesi

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Dashboard/Views/Index.cshtml`

Layout'taki `page-header` zaten "Dashboard" başlığını gösteriyor. View'ın kendi `page-header` bölümünü (GENEL BAKIS / Dashboard) kaldırarak çakışmayı düzelt.

- [ ] **Step 1: Remove duplicate page header from Index.cshtml**

In `Application/Entegrasyon.MVC/Features/Dashboard/Views/Index.cshtml`, find and remove the `page-header` block:

```html
<div class="page-header d-print-none">
    <div class="container-xl">
        <div class="row align-items-center">
            <div class="col-auto">
                <span class="page-pretitle">GENEL BAKİS</span>
                <h2 class="page-title">Dashboard</h2>
            </div>
        </div>
    </div>
</div>
```

Keep the `@{ ViewData.SetPageTitle("Dashboard"); ViewData.SetActiveNav("dashboard"); }` at the top — those are needed by the layout.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Dashboard/Views/Index.cshtml
git commit -m "fix(dashboard): remove duplicate page header

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Stat Kartları — Renkli İkon + Trend Göstergesi

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_StatsCards.cshtml`

Mevcut stat kartları düz beyaz kartlar — sadece subheader + h1 sayı. Design system kuralına göre: sol tarafta renkli avatar içinde ikon, sağ tarafta trend göstergesi eklenmeli.

Not: Trend verisi (yüzde değişim) şu an DTO'da yok. Bu task'ta sadece statik ikon + renk ekliyoruz. Trend verisi ileride backend'den geldiğinde kolayca eklenebilecek yapıyı hazırlıyoruz.

- [ ] **Step 1: Rewrite _StatsCards.cshtml with Tabler stat card pattern**

Replace the entire content of `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_StatsCards.cshtml` with:

```html
@model Entegrasyon.Entity.Dtos.Dashboard.DashboardStatsDto

<div class="row row-deck row-cards mb-3">
    @* ── Toplam Ürün ── *@
    <div class="col-sm-6 col-lg-3">
        <div class="card card-sm">
            <div class="card-body">
                <div class="row align-items-center">
                    <div class="col-auto">
                        <span class="bg-primary text-white avatar">
                            <i class="ti ti-package icon"></i>
                        </span>
                    </div>
                    <div class="col">
                        <div class="subheader">TOPLAM URUN</div>
                        <div class="h1 mb-0">@Model.TotalProducts.ToString("N0")</div>
                    </div>
                </div>
                <div class="mt-2">
                    <small class="text-secondary">@Model.TotalVariants.ToString("N0") varyant</small>
                </div>
            </div>
        </div>
    </div>

    @* ── Bugünkü Satış ── *@
    <div class="col-sm-6 col-lg-3">
        <div class="card card-sm">
            <div class="card-body">
                <div class="row align-items-center">
                    <div class="col-auto">
                        <span class="bg-success text-white avatar">
                            <i class="ti ti-cash icon"></i>
                        </span>
                    </div>
                    <div class="col">
                        <div class="subheader">BUGUNKU SATIS</div>
                        <div class="h1 mb-0">@Model.TodaySales</div>
                    </div>
                </div>
                <div class="mt-2">
                    <small class="text-secondary">@Model.TodayRevenue.ToString("C2", new System.Globalization.CultureInfo("tr-TR"))</small>
                </div>
            </div>
        </div>
    </div>

    @* ── Bekleyen Sipariş ── *@
    <div class="col-sm-6 col-lg-3">
        <div class="card card-sm">
            <div class="card-body">
                <div class="row align-items-center">
                    <div class="col-auto">
                        <span class="bg-warning text-white avatar">
                            <i class="ti ti-clock icon"></i>
                        </span>
                    </div>
                    <div class="col">
                        <div class="subheader">BEKLEYEN SIPARIS</div>
                        <div class="h1 mb-0 @(Model.PendingOrders > 0 ? "text-warning" : "")">@Model.PendingOrders</div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    @* ── Düşük Stok ── *@
    <div class="col-sm-6 col-lg-3">
        <div class="card card-sm">
            <div class="card-body">
                <div class="row align-items-center">
                    <div class="col-auto">
                        <span class="bg-danger text-white avatar">
                            <i class="ti ti-alert-triangle icon"></i>
                        </span>
                    </div>
                    <div class="col">
                        <div class="subheader">DUSUK STOK</div>
                        <div class="h1 mb-0 @(Model.LowStockProducts > 0 ? "text-danger" : "")">@Model.LowStockProducts</div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_StatsCards.cshtml
git commit -m "feat(dashboard): add colored icons to stat cards

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Haftalık Satış Grafiği — Tema Uyumlu Renkler

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_WeeklyChart.cshtml`

Mevcut grafik hardcoded `#206bc4` rengi kullanıyor. Tema toggle'da dark/light geçişinde renkler güncellenmeli. `data-apex-chart` attribute'ü ekleyerek `site.js`'deki `onThemeChange` hook'una bağla.

- [ ] **Step 1: Update _WeeklyChart.cshtml with theme-aware colors**

Replace the entire content of `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_WeeklyChart.cshtml` with:

```html
@model List<Entegrasyon.Entity.Dtos.Dashboard.DailySalesDto>

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Haftalik Satis</h3>
    </div>
    <div class="card-body">
        @if (Model == null || Model.Count == 0)
        {
            <div class="text-center text-secondary py-4">Henuz satis verisi yok.</div>
        }
        else
        {
            <div id="weekly-chart" data-apex-chart style="height: 250px;"></div>
            <script>
                (function () {
                    var isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
                    var textColor = isDark ? '#a0aec0' : '#666';
                    var gridColor = isDark ? '#2c3e56' : '#e0e0e0';

                    var options = {
                        chart: {
                            id: 'weekly-chart',
                            type: 'area',
                            height: 250,
                            toolbar: { show: false },
                            fontFamily: 'inherit',
                            background: 'transparent'
                        },
                        theme: { mode: isDark ? 'dark' : 'light' },
                        series: [{
                            name: 'Ciro (TL)',
                            data: [@string.Join(",", Model.Select(d => d.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture)))]
                        }],
                        xaxis: {
                            categories: [@string.Join(",", Model.Select(d => $"'{d.Date:dd MMM}'"))],
                            labels: { style: { colors: textColor } }
                        },
                        yaxis: {
                            labels: {
                                style: { colors: textColor },
                                formatter: function (val) { return val.toLocaleString('tr-TR') + ' TL'; }
                            }
                        },
                        colors: [getComputedStyle(document.documentElement).getPropertyValue('--tblr-primary').trim() || '#206bc4'],
                        fill: { type: 'gradient', gradient: { opacityFrom: 0.4, opacityTo: 0.05 } },
                        stroke: { curve: 'smooth', width: 2 },
                        grid: { borderColor: gridColor },
                        tooltip: {
                            y: { formatter: function (val) { return val.toLocaleString('tr-TR') + ' TL'; } }
                        },
                        dataLabels: { enabled: false }
                    };

                    new ApexCharts(document.querySelector('#weekly-chart'), options).render();
                })();
            </script>
        }
    </div>
</div>
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_WeeklyChart.cshtml
git commit -m "feat(dashboard): make weekly chart theme-aware with CSS variables

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Son 30 Gün Ciro Trendi — Tema Uyumlu Renkler

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_ProfitChart.cshtml`

Aynı tema uyumu `_ProfitChart` için de uygulanmalı. Hardcoded `#206bc4`'ü CSS variable'a çevir, `data-apex-chart` ekle.

- [ ] **Step 1: Update _ProfitChart.cshtml with theme-aware colors**

Replace the entire content of `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_ProfitChart.cshtml` with:

```html
@model Entegrasyon.Entity.Dtos.Reports.SalesReportDto

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Son 30 Gun — Ciro Trendi</h3>
    </div>
    <div class="card-body">
        <div class="row align-items-center mb-3">
            <div class="col-auto">
                <div class="subheader">Toplam Ciro</div>
                <div class="h2 mb-0">@Model.Summary.TotalRevenue.ToString("N2") TL</div>
            </div>
            <div class="col-auto">
                <div class="subheader">Toplam Satis</div>
                <div class="h2 mb-0">@Model.Summary.TotalSales</div>
            </div>
            <div class="col-auto">
                <div class="subheader">Ortalama Siparis</div>
                <div class="h2 mb-0">@Model.Summary.AverageOrderValue.ToString("N2") TL</div>
            </div>
        </div>

        @if (Model.DailySales == null || Model.DailySales.Count == 0)
        {
            <div class="text-center text-secondary py-4">Henuz satis verisi yok.</div>
        }
        else
        {
            <div id="profit-chart" data-apex-chart style="height: 250px;"></div>
            <script>
                (function () {
                    var isDark = document.documentElement.getAttribute('data-bs-theme') === 'dark';
                    var textColor = isDark ? '#a0aec0' : '#666';
                    var gridColor = isDark ? '#2c3e56' : '#e0e0e0';

                    var options = {
                        chart: {
                            id: 'profit-chart',
                            type: 'bar',
                            height: 250,
                            toolbar: { show: false },
                            fontFamily: 'inherit',
                            background: 'transparent'
                        },
                        theme: { mode: isDark ? 'dark' : 'light' },
                        series: [{
                            name: 'Ciro (TL)',
                            data: [@string.Join(",", Model.DailySales.Select(d => d.Revenue.ToString(System.Globalization.CultureInfo.InvariantCulture)))]
                        }],
                        xaxis: {
                            categories: [@string.Join(",", Model.DailySales.Select(d => $"'{d.Date:dd.MM}'"))],
                            labels: {
                                rotate: -45,
                                style: { fontSize: '10px', colors: textColor }
                            }
                        },
                        yaxis: {
                            labels: {
                                style: { colors: textColor },
                                formatter: function (val) { return val.toLocaleString('tr-TR'); }
                            }
                        },
                        colors: [getComputedStyle(document.documentElement).getPropertyValue('--tblr-primary').trim() || '#206bc4'],
                        plotOptions: { bar: { borderRadius: 2, columnWidth: '70%' } },
                        grid: { borderColor: gridColor },
                        tooltip: {
                            y: { formatter: function (val) { return val.toLocaleString('tr-TR') + ' TL'; } }
                        },
                        dataLabels: { enabled: false }
                    };

                    new ApexCharts(document.querySelector('#profit-chart'), options).render();
                })();
            </script>
        }
    </div>
</div>
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_ProfitChart.cshtml
git commit -m "feat(dashboard): make profit chart theme-aware with CSS variables

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Quick Actions — Tabler Icons'a Geçiş

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_QuickActions.cshtml`

Inline SVG ikonlarını `ti ti-*` class'larıyla değiştir.

- [ ] **Step 1: Rewrite _QuickActions.cshtml with Tabler Icons**

Replace the entire content of `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_QuickActions.cshtml` with:

```html
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Hizli Islemler</h3>
    </div>
    <div class="card-body">
        <div class="d-flex flex-wrap gap-2">
            <a href="/products/add" class="btn btn-primary">
                <i class="ti ti-plus me-1"></i>Urun Ekle
            </a>
            <a href="/pos" class="btn btn-success">
                <i class="ti ti-cash me-1"></i>Satis Yap
            </a>
            <a href="/sales" class="btn btn-warning">
                <i class="ti ti-receipt me-1"></i>Satislar
            </a>
        </div>
    </div>
</div>
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_QuickActions.cshtml
git commit -m "feat(dashboard): replace inline SVGs with Tabler Icons in quick actions

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Marketplace Status — Table Hover + Badge İyileştirmesi

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_MarketplaceStatus.cshtml`

Tabloya `table-hover` ekle, boş durum için Tabler `empty` pattern'i kullan.

- [ ] **Step 1: Update _MarketplaceStatus.cshtml**

Replace the entire content of `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_MarketplaceStatus.cshtml` with:

```html
@model List<Entegrasyon.Entity.Dtos.Dashboard.MarketplaceStatusDto>

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Pazaryeri Durumu</h3>
    </div>
    @if (Model == null || Model.Count == 0)
    {
        <div class="card-body">
            <div class="empty">
                <div class="empty-icon"><i class="ti ti-building-store icon"></i></div>
                <p class="empty-title">Pazaryeri bağlantısı yok</p>
                <p class="empty-subtitle text-secondary">Entegrasyon ayarlarından pazaryeri ekleyin.</p>
            </div>
        </div>
    }
    else
    {
        <div class="table-responsive">
            <table class="table table-vcenter card-table table-hover">
                <thead>
                    <tr>
                        <th>PAZARYERİ</th>
                        <th class="text-center">SENKRON</th>
                        <th class="text-center">BEKLEYEN</th>
                        <th class="text-center">HATA</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var mp in Model)
                    {
                        <tr>
                            <td>@mp.Name</td>
                            <td class="text-center"><span class="badge bg-success">@mp.SyncedCount</span></td>
                            <td class="text-center">
                                @if (mp.PendingCount > 0)
                                {
                                    <span class="badge bg-warning">@mp.PendingCount</span>
                                }
                                else
                                {
                                    <span class="text-secondary">0</span>
                                }
                            </td>
                            <td class="text-center">
                                @if (mp.FailedCount > 0)
                                {
                                    <span class="badge bg-danger">@mp.FailedCount</span>
                                }
                                else
                                {
                                    <span class="text-secondary">0</span>
                                }
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_MarketplaceStatus.cshtml
git commit -m "feat(dashboard): improve marketplace status table with hover and empty state

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Recent Activity — Table Hover + Empty State

**Files:**
- Modify: `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_RecentActivity.cshtml`

Tabloya `table-hover` ekle, boş durum için Tabler `empty` pattern'i kullan.

- [ ] **Step 1: Update _RecentActivity.cshtml**

Replace the entire content of `Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_RecentActivity.cshtml` with:

```html
@model List<Entegrasyon.Entity.Dtos.Dashboard.RecentActivityDto>

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Son Aktiviteler</h3>
    </div>
    @if (Model == null || Model.Count == 0)
    {
        <div class="card-body">
            <div class="empty">
                <div class="empty-icon"><i class="ti ti-activity icon"></i></div>
                <p class="empty-title">Henuz aktivite yok</p>
            </div>
        </div>
    }
    else
    {
        <div class="table-responsive">
            <table class="table table-vcenter card-table table-hover">
                <thead>
                    <tr>
                        <th>ISLEM</th>
                        <th class="text-end">TARİH</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var activity in Model)
                    {
                        <tr>
                            <td>@activity.Content</td>
                            <td class="text-end text-secondary">@activity.CreatedAt.ToString("dd.MM.yyyy HH:mm")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Features/Dashboard/Views/Partials/_RecentActivity.cshtml
git commit -m "feat(dashboard): improve recent activity table with hover and empty state

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: Build + Test Doğrulaması

**Files:** None (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build Entegrasyon.sln --verbosity minimal`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: Run unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --verbosity minimal`
Expected: All ~1509 tests pass

- [ ] **Step 3: Run MVC tests**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --verbosity minimal`
Expected: All tests pass

- [ ] **Step 4: Run AdminPanel tests**

Run: `dotnet test Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj --verbosity minimal`
Expected: All tests pass

---

### Task 9: Görsel Doğrulama

**Files:** None (manual verification)

- [ ] **Step 1: Start the application**

Run: `cd Application/Entegrasyon.MVC && dotnet run`

- [ ] **Step 2: Login and verify dashboard in light mode**

Open `http://localhost:5100`, login with admin/123456789 and check:
1. Duplicate başlık yok — tek "Dashboard" başlığı (layout'tan)
2. Stat kartlarında renkli ikonlar: mavi paket (ürün), yeşil para (satış), sarı saat (sipariş), kırmızı uyarı (stok)
3. Haftalık satış grafiği tema uyumlu renklerde
4. Ciro trendi grafiği tema uyumlu
5. Quick actions butonlarında Tabler Icons (artı, para, fiş)
6. Pazaryeri durumu tablosunda hover efekti
7. Son aktiviteler tablosunda hover efekti

- [ ] **Step 3: Toggle dark theme and verify**

Click the moon icon in the navbar. Check:
1. Tüm kartlar dark tema'ya uyumlu
2. Grafikler otomatik dark renklere geçti (koyu arka plan, açık text)
3. Tablolar dark tema'da okunabilir

- [ ] **Step 4: Fix any visual issues found**

Address layout, spacing, or color issues discovered during verification.
