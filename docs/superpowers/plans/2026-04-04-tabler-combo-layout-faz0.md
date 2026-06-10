# Tabler Combo Layout — Faz 0: Layout Altyapısı Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mevcut sidebar-only layout'u Tabler combo layout'a (üst navbar + dinamik sidebar) dönüştür ve dark/light tema toggle ekle.

**Architecture:** Üst navbar ana grup menülerini barındırır, sidebar seçili grubun alt menülerini dinamik gösterir. Grup eşleştirmesi mevcut `activeNav` string'inden otomatik türetilir — mevcut view'lar değişmez. Tema toggle `localStorage` + render-blocking script ile FOUC-free çalışır.

**Tech Stack:** ASP.NET Core MVC, Tabler UI (Bootstrap 5), Vanilla JS, Tabler Icons (`ti ti-*`)

**Spec:** `docs/superpowers/specs/2026-04-04-tabler-combo-layout-ui-refresh-design.md`

---

### Task 1: ViewData Extension — Nav Group Desteği

**Files:**
- Modify: `Application/Entegrasyon.MVC/Infrastructure/Extensions/ViewDataExtensions.cs`
- Test: `Test/Entegrasyon.MVC.Test/Infrastructure/ViewDataExtensionsTests.cs`

- [ ] **Step 1: Write failing test for GetActiveNavGroup**

```csharp
// Test/Entegrasyon.MVC.Test/Infrastructure/ViewDataExtensionsTests.cs
using Entegrasyon.MVC.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.MVC.Test.Infrastructure;

public class ViewDataExtensionsTests
{
    private ViewDataDictionary CreateViewData()
        => new(new EmptyModelMetadataProvider(), new ModelStateDictionary());

    [Theory]
    [InlineData("products", "yonetim")]
    [InlineData("categories", "yonetim")]
    [InlineData("attributes", "yonetim")]
    [InlineData("brands", "yonetim")]
    [InlineData("pos", "yonetim")]
    [InlineData("sales", "yonetim")]
    [InlineData("orders", "yonetim")]
    [InlineData("shipping", "yonetim")]
    [InlineData("bulk-operations", "yonetim")]
    [InlineData("branch-offices", "yonetim")]
    [InlineData("marketplace-sync", "pazaryeri")]
    [InlineData("marketplace-matching", "pazaryeri")]
    [InlineData("marketplace-orders", "pazaryeri")]
    [InlineData("commission-rates", "pazaryeri")]
    [InlineData("matched-entities", "pazaryeri")]
    [InlineData("customers", "musteriler")]
    [InlineData("invoicing", "musteriler")]
    [InlineData("reports", "raporlar")]
    [InlineData("storefront", "Mağaza")]
    [InlineData("settings", "ayarlar")]
    [InlineData("users", "ayarlar")]
    [InlineData("roles", "ayarlar")]
    [InlineData("admin-notifications", "ayarlar")]
    [InlineData("dashboard", "")]
    [InlineData("chat", "")]
    [InlineData("", "")]
    public void GetActiveNavGroup_ShouldReturnCorrectGroup(string activeNav, string expectedGroup)
    {
        var viewData = CreateViewData();
        viewData.SetActiveNav(activeNav);

        var group = viewData.GetActiveNavGroup();

        group.Should().Be(expectedGroup);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --filter "FullyQualifiedName~ViewDataExtensionsTests" -v minimal`
Expected: FAIL — `GetActiveNavGroup` method does not exist

- [ ] **Step 3: Implement GetActiveNavGroup**

Add to `Application/Entegrasyon.MVC/Infrastructure/Extensions/ViewDataExtensions.cs`:

```csharp
private static readonly Dictionary<string, string> NavGroupMap = new()
{
    // Yonetim
    ["products"] = "yonetim",
    ["categories"] = "yonetim",
    ["attributes"] = "yonetim",
    ["brands"] = "yonetim",
    ["pos"] = "yonetim",
    ["sales"] = "yonetim",
    ["orders"] = "yonetim",
    ["shipping"] = "yonetim",
    ["bulk-operations"] = "yonetim",
    ["branch-offices"] = "yonetim",
    // Pazaryeri
    ["marketplace-sync"] = "pazaryeri",
    ["marketplace-matching"] = "pazaryeri",
    ["marketplace-orders"] = "pazaryeri",
    ["commission-rates"] = "pazaryeri",
    ["matched-entities"] = "pazaryeri",
    // Musteriler
    ["customers"] = "musteriler",
    ["invoicing"] = "musteriler",
    // Raporlar
    ["reports"] = "raporlar",
    // Mağaza
    ["storefront"] = "Mağaza",
    // Ayarlar
    ["settings"] = "ayarlar",
    ["users"] = "ayarlar",
    ["roles"] = "ayarlar",
    ["admin-notifications"] = "ayarlar",
};

public static string GetActiveNavGroup(this ViewDataDictionary viewData)
{
    var activeNav = viewData.GetActiveNav();
    return NavGroupMap.GetValueOrDefault(activeNav, "");
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --filter "FullyQualifiedName~ViewDataExtensionsTests" -v minimal`
Expected: PASS — all 26 test cases green

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.MVC/Infrastructure/Extensions/ViewDataExtensions.cs \
      Test/Entegrasyon.MVC.Test/Infrastructure/ViewDataExtensionsTests.cs
git commit -m "feat(layout): add GetActiveNavGroup extension for combo layout navigation"
```

---

### Task 2: Üst Navbar Partial Oluştur

**Files:**
- Create: `Application/Entegrasyon.MVC/Shared/Views/_Navbar.cshtml`

- [ ] **Step 1: Create _Navbar.cshtml**

```html
@{
    var activeNav = ViewData.GetActiveNav();
    var activeGroup = ViewData.GetActiveNavGroup();
}

<header class="navbar navbar-expand-md d-print-none">
    <div class="container-xl">
        @* ── Marka ── *@
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbar-menu">
            <span class="navbar-toggler-icon"></span>
        </button>
        <h1 class="navbar-brand navbar-brand-autodark d-none-navbar-horizontal pe-0 pe-md-3">
            <a href="/">Entegrasyon</a>
        </h1>

        <div class="collapse navbar-collapse" id="navbar-menu">
            <div class="d-flex flex-column flex-md-row flex-fill align-items-stretch align-items-md-center">
                <ul class="navbar-nav">
                    @* ── Dashboard (bağımsız link) ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "dashboard" ? "active" : "")" href="/">
                            <span class="nav-link-icon"><i class="ti ti-home"></i></span>
                            <span class="nav-link-title">Dashboard</span>
                        </a>
                    </li>

                    @* ── Yönetim ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeGroup == "yonetim" ? "active" : "")" href="/products">
                            <span class="nav-link-icon"><i class="ti ti-briefcase"></i></span>
                            <span class="nav-link-title">Yonetim</span>
                        </a>
                    </li>

                    @* ── Pazaryeri ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeGroup == "pazaryeri" ? "active" : "")" href="/marketplace/sync">
                            <span class="nav-link-icon"><i class="ti ti-building-store"></i></span>
                            <span class="nav-link-title">Pazaryeri</span>
                        </a>
                    </li>

                    @* ── Müşteriler ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeGroup == "musteriler" ? "active" : "")" href="/customers">
                            <span class="nav-link-icon"><i class="ti ti-users"></i></span>
                            <span class="nav-link-title">Musteriler</span>
                        </a>
                    </li>

                    @* ── Raporlar ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeGroup == "raporlar" ? "active" : "")" href="/reports/sales">
                            <span class="nav-link-icon"><i class="ti ti-chart-bar"></i></span>
                            <span class="nav-link-title">Raporlar</span>
                        </a>
                    </li>

                    @* ── Mesajlar (bağımsız link) ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "chat" ? "active" : "")" href="/chat">
                            <span class="nav-link-icon"><i class="ti ti-message-circle"></i></span>
                            <span class="nav-link-title">Mesajlar</span>
                        </a>
                    </li>

                    @* ── Mağaza ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeGroup == "Mağaza" ? "active" : "")" href="/settings/storefront">
                            <span class="nav-link-icon"><i class="ti ti-shopping-cart"></i></span>
                            <span class="nav-link-title">Mağaza</span>
                        </a>
                    </li>

                    @* ── Ayarlar ── *@
                    <li class="nav-item">
                        <a class="nav-link @(activeGroup == "ayarlar" ? "active" : "")" href="/settings/general">
                            <span class="nav-link-icon"><i class="ti ti-settings"></i></span>
                            <span class="nav-link-title">Ayarlar</span>
                        </a>
                    </li>
                </ul>
            </div>
        </div>

        @* ── Sağ taraf: Arama + Hızlı İşlemler + Tema + Bildirim + Kullanıcı ── *@
        <div class="navbar-nav flex-row order-md-last">
            @* ── Global Arama ── *@
            <div class="d-none d-md-flex me-3">
                <div class="input-icon">
                    <span class="input-icon-addon"><i class="ti ti-search"></i></span>
                    <input type="text" class="form-control" placeholder="Ara..." id="global-search" autocomplete="off" />
                </div>
            </div>

            @* ── Hızlı İşlemler ── *@
            <div class="nav-item dropdown me-2">
                <a href="#" class="nav-link px-0" data-bs-toggle="dropdown" tabindex="-1" aria-label="Hızlı İşlemler">
                    <i class="ti ti-bolt"></i>
                </a>
                <div class="dropdown-menu dropdown-menu-end">
                    <a class="dropdown-item" href="/products/create">
                        <i class="ti ti-plus me-2"></i>Urun Ekle
                    </a>
                    <a class="dropdown-item" href="/pos">
                        <i class="ti ti-cash me-2"></i>Satis Yap
                    </a>
                    <a class="dropdown-item" href="/sales">
                        <i class="ti ti-receipt me-2"></i>Satislar
                    </a>
                </div>
            </div>

            @* ── Tema Toggle ── *@
            <div class="nav-item me-2">
                <a href="#" class="nav-link px-0" id="theme-toggle" title="Tema degistir">
                    <i class="ti ti-moon" id="theme-icon"></i>
                </a>
            </div>

            @* ── Bildirim Zili ── *@
            <div class="nav-item me-2">
                @await Component.InvokeAsync("NotificationBell")
            </div>

            @* ── Kullanıcı Dropdown ── *@
            <div class="nav-item dropdown">
                <a href="#" class="nav-link d-flex lh-1 text-reset p-0" data-bs-toggle="dropdown" aria-label="Hesap menüsü">
                    <span class="avatar avatar-sm bg-blue-lt"><i class="ti ti-user"></i></span>
                    <div class="d-none d-xl-block ps-2">
                        <div>@(User.Identity?.Name ?? "Kullanici")</div>
                    </div>
                </a>
                <div class="dropdown-menu dropdown-menu-end dropdown-menu-arrow">
                    <a class="dropdown-item" href="/profile">
                        <i class="ti ti-user me-2"></i>Profil
                    </a>
                    <div class="dropdown-divider"></div>
                    <form asp-controller="Auth" asp-action="Logout" method="post">
                        <button type="submit" class="dropdown-item text-danger">
                            <i class="ti ti-logout me-2"></i>Cikis Yap
                        </button>
                    </form>
                </div>
            </div>
        </div>
    </div>
</header>
```

- [ ] **Step 2: Verify the file compiles**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Shared/Views/_Navbar.cshtml
git commit -m "feat(layout): create top navbar partial for combo layout"
```

---

### Task 3: Sidebar'ı Dinamik Grup Bazlı Yapıya Dönüştür

**Files:**
- Modify: `Application/Entegrasyon.MVC/Shared/Views/_Sidebar.cshtml`

- [ ] **Step 1: Rewrite _Sidebar.cshtml with dynamic group-based menus**

Replace the entire content of `_Sidebar.cshtml` with:

```html
@{
    var activeNav = ViewData.GetActiveNav();
    var activeGroup = ViewData.GetActiveNavGroup();
}

@* Sidebar sadece aktif bir grup varsa gösterilir (Dashboard ve Mesajlar bağımsız) *@
@if (!string.IsNullOrEmpty(activeGroup))
{
<aside class="navbar navbar-vertical navbar-expand-lg">
    <div class="container-fluid">
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#sidebar-menu">
            <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" id="sidebar-menu">
            <ul class="navbar-nav pt-lg-3">

                @if (activeGroup == "yonetim")
                {
                    <li class="nav-item"><span class="nav-link nav-link-title text-uppercase fw-bold small text-secondary">Yonetim</span></li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "products" ? "active" : "")" require-permission="Permissions.Products.View" href="/products">
                            <span class="nav-link-icon"><i class="ti ti-package"></i></span>
                            <span class="nav-link-title">Urunler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "categories" ? "active" : "")" require-permission="Permissions.Categories.View" href="/categories">
                            <span class="nav-link-icon"><i class="ti ti-category"></i></span>
                            <span class="nav-link-title">Kategoriler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "attributes" ? "active" : "")" require-permission="Permissions.Categories.View" href="/attributes">
                            <span class="nav-link-icon"><i class="ti ti-tags"></i></span>
                            <span class="nav-link-title">Ozellikler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "brands" ? "active" : "")" require-permission="Permissions.Brands.View" href="/brands">
                            <span class="nav-link-icon"><i class="ti ti-award"></i></span>
                            <span class="nav-link-title">Markalar</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "pos" ? "active" : "")" require-permission="Permissions.Sales.Create" href="/pos">
                            <span class="nav-link-icon"><i class="ti ti-device-desktop"></i></span>
                            <span class="nav-link-title">POS Terminal</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "sales" ? "active" : "")" require-permission="Permissions.Sales.View" href="/sales">
                            <span class="nav-link-icon"><i class="ti ti-receipt"></i></span>
                            <span class="nav-link-title">Satis Gecmisi</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "orders" ? "active" : "")" require-permission="Permissions.Orders.View" href="/orders">
                            <span class="nav-link-icon"><i class="ti ti-shopping-cart"></i></span>
                            <span class="nav-link-title">Satislar ve Siparişler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "shipping" ? "active" : "")" require-permission="Permissions.Cargo.View" href="/shipping">
                            <span class="nav-link-icon"><i class="ti ti-truck"></i></span>
                            <span class="nav-link-title">Kargo Takip</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "bulk-operations" ? "active" : "")" require-permission="Permissions.Products.Edit" href="/bulk-operations">
                            <span class="nav-link-icon"><i class="ti ti-layers-subtract"></i></span>
                            <span class="nav-link-title">Toplu Islem</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "branch-offices" ? "active" : "")" require-permission="Permissions.BranchOffices.View" href="/branch-offices">
                            <span class="nav-link-icon"><i class="ti ti-building-warehouse"></i></span>
                            <span class="nav-link-title">Depolar</span>
                        </a>
                    </li>
                }

                @if (activeGroup == "pazaryeri")
                {
                    <li class="nav-item"><span class="nav-link nav-link-title text-uppercase fw-bold small text-secondary">Pazaryeri</span></li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "matched-entities" ? "active" : "")" require-permission="Permissions.Marketplace.View" href="/matched-entities">
                            <span class="nav-link-icon"><i class="ti ti-template"></i></span>
                            <span class="nav-link-title">Hazir Sablonlar</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "marketplace-sync" ? "active" : "")" require-permission="Permissions.Marketplace.View" href="/marketplace/sync">
                            <span class="nav-link-icon"><i class="ti ti-refresh"></i></span>
                            <span class="nav-link-title">Senkronizasyon</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "marketplace-matching" ? "active" : "")" require-permission="Permissions.Marketplace.View" href="/marketplace/matching">
                            <span class="nav-link-icon"><i class="ti ti-link"></i></span>
                            <span class="nav-link-title">Urun Eslestirme</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "commission-rates" ? "active" : "")" require-permission="Permissions.Marketplace.View" href="/marketplace/commission-rates">
                            <span class="nav-link-icon"><i class="ti ti-percentage"></i></span>
                            <span class="nav-link-title">Komisyon Oranlari</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "marketplace-orders" ? "active" : "")" require-permission="Permissions.Orders.View" href="/marketplace/orders">
                            <span class="nav-link-icon"><i class="ti ti-clipboard-list"></i></span>
                            <span class="nav-link-title">Siparişler</span>
                        </a>
                    </li>
                }

                @if (activeGroup == "musteriler")
                {
                    <li class="nav-item"><span class="nav-link nav-link-title text-uppercase fw-bold small text-secondary">Musteriler</span></li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "customers" ? "active" : "")" require-permission="Permissions.Customers.View" href="/customers">
                            <span class="nav-link-icon"><i class="ti ti-users"></i></span>
                            <span class="nav-link-title">Musteriler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "invoicing" ? "active" : "")" require-permission="Permissions.Orders.View" href="/invoicing">
                            <span class="nav-link-icon"><i class="ti ti-file-invoice"></i></span>
                            <span class="nav-link-title">E-Fatura</span>
                        </a>
                    </li>
                }

                @if (activeGroup == "raporlar")
                {
                    <li class="nav-item"><span class="nav-link nav-link-title text-uppercase fw-bold small text-secondary">Raporlar</span></li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "reports" ? "active" : "")" require-permission="Permissions.Reports.View" href="/reports/sales">
                            <span class="nav-link-icon"><i class="ti ti-report-analytics"></i></span>
                            <span class="nav-link-title">Satis Raporlari</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Reports.View" href="/reports/profit-loss">
                            <span class="nav-link-icon"><i class="ti ti-trending-up"></i></span>
                            <span class="nav-link-title">Kar/Zarar Raporu</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Reports.View" href="/reports/product-performance">
                            <span class="nav-link-icon"><i class="ti ti-chart-dots"></i></span>
                            <span class="nav-link-title">Ürün Performansı</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Reports.View" href="/reports/stock-alerts">
                            <span class="nav-link-icon"><i class="ti ti-alert-triangle"></i></span>
                            <span class="nav-link-title">Stok Uyarilari</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Reports.View" href="/reports/inventory">
                            <span class="nav-link-icon"><i class="ti ti-clipboard-check"></i></span>
                            <span class="nav-link-title">Stok Raporlari</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Reports.View" href="/reports/marketplace">
                            <span class="nav-link-icon"><i class="ti ti-chart-pie"></i></span>
                            <span class="nav-link-title">Pazaryeri Raporlari</span>
                        </a>
                    </li>
                }

                @if (activeGroup == "Mağaza")
                {
                    <li class="nav-item"><span class="nav-link nav-link-title text-uppercase fw-bold small text-secondary">Mağaza</span></li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/settings/storefront">
                            <span class="nav-link-icon"><i class="ti ti-settings"></i></span>
                            <span class="nav-link-title">Mağaza Ayarlari</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/settings/storefront/legal">
                            <span class="nav-link-icon"><i class="ti ti-gavel"></i></span>
                            <span class="nav-link-title">Yasal Metinler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/settings/storefront/banners">
                            <span class="nav-link-icon"><i class="ti ti-photo"></i></span>
                            <span class="nav-link-title">Banner Yonetimi</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/settings/storefront/payment">
                            <span class="nav-link-icon"><i class="ti ti-credit-card"></i></span>
                            <span class="nav-link-title">Odeme Ayarlari</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/storefront/reviews">
                            <span class="nav-link-icon"><i class="ti ti-star"></i></span>
                            <span class="nav-link-title">Degerlendirmeler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/storefront/returns">
                            <span class="nav-link-icon"><i class="ti ti-arrow-back-up"></i></span>
                            <span class="nav-link-title">Iade Talepleri</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/storefront/campaigns">
                            <span class="nav-link-icon"><i class="ti ti-speakerphone"></i></span>
                            <span class="nav-link-title">Otomatik Kampanyalar</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/storefront/sellers">
                            <span class="nav-link-icon"><i class="ti ti-users-group"></i></span>
                            <span class="nav-link-title">Satici Yonetimi</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/storefront/payouts">
                            <span class="nav-link-icon"><i class="ti ti-cash"></i></span>
                            <span class="nav-link-title">Odeme Talepleri</span>
                        </a>
                    </li>
                }

                @if (activeGroup == "ayarlar")
                {
                    <li class="nav-item"><span class="nav-link nav-link-title text-uppercase fw-bold small text-secondary">Ayarlar</span></li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "settings" ? "active" : "")" require-permission="Permissions.Settings.View" href="/settings/general">
                            <span class="nav-link-icon"><i class="ti ti-adjustments"></i></span>
                            <span class="nav-link-title">Genel Ayarlar</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/settings/notifications">
                            <span class="nav-link-icon"><i class="ti ti-bell"></i></span>
                            <span class="nav-link-title">Bildirimler</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Integrations.View" href="/settings/integrations">
                            <span class="nav-link-icon"><i class="ti ti-plug"></i></span>
                            <span class="nav-link-title">Entegrasyonlar</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/settings/printing">
                            <span class="nav-link-icon"><i class="ti ti-printer"></i></span>
                            <span class="nav-link-title">Yazici & Barkod</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" require-permission="Permissions.Settings.View" href="/settings/desktop">
                            <span class="nav-link-icon"><i class="ti ti-device-laptop"></i></span>
                            <span class="nav-link-title">Masaustu Uygulama</span>
                        </a>
                    </li>
                    <li class="nav-item my-1"><hr class="dropdown-divider" /></li>
                    <li class="nav-item"><span class="nav-link nav-link-title text-uppercase fw-bold small text-secondary">Kullanici Yonetimi</span></li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "users" ? "active" : "")" require-permission="Permissions.Users.View" href="/users">
                            <span class="nav-link-icon"><i class="ti ti-users"></i></span>
                            <span class="nav-link-title">Kullanicilar</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "roles" ? "active" : "")" require-permission="Permissions.Roles.View" href="/roles">
                            <span class="nav-link-icon"><i class="ti ti-shield-lock"></i></span>
                            <span class="nav-link-title">Roller</span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link @(activeNav == "admin-notifications" ? "active" : "")" require-permission="Permissions.Notifications.Manage" href="/admin/notifications">
                            <span class="nav-link-icon"><i class="ti ti-bell-ringing"></i></span>
                            <span class="nav-link-title">Bildirim Yonetimi</span>
                        </a>
                    </li>
                }

            </ul>
        </div>
    </div>
</aside>
}
```

- [ ] **Step 2: Verify the file compiles**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Shared/Views/_Sidebar.cshtml
git commit -m "feat(layout): convert sidebar to dynamic group-based navigation"
```

---

### Task 4: Tabler Icons CSS Ekle

**Files:**
- Check: `Application/Entegrasyon.MVC/wwwroot/lib/tabler/` for icons

Yeni sidebar ve navbar `ti ti-*` class'larıyla Tabler Icons kullanıyor. Eski sidebar inline SVG kullanıyordu. Tabler Icons CSS'inin yüklü olup olmadığını kontrol et.

- [ ] **Step 1: Check if Tabler Icons CSS exists**

Run: `ls Application/Entegrasyon.MVC/wwwroot/lib/tabler/css/ | grep -i icon`

If no icon CSS found, download it:

Run: `curl -sL https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@latest/dist/tabler-icons.min.css -o Application/Entegrasyon.MVC/wwwroot/lib/tabler/css/tabler-icons.min.css`

Also download the font files:

Run:
```bash
mkdir -p Application/Entegrasyon.MVC/wwwroot/lib/tabler/fonts
curl -sL https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@latest/dist/fonts/tabler-icons.woff2 -o Application/Entegrasyon.MVC/wwwroot/lib/tabler/fonts/tabler-icons.woff2
curl -sL https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@latest/dist/fonts/tabler-icons.woff -o Application/Entegrasyon.MVC/wwwroot/lib/tabler/fonts/tabler-icons.woff
```

- [ ] **Step 2: Fix font path in the CSS if needed**

The CSS might reference `../fonts/` — verify the path matches the directory structure. If fonts are at `wwwroot/lib/tabler/fonts/` and CSS is at `wwwroot/lib/tabler/css/`, the relative path `../fonts/` should work.

- [ ] **Step 3: Add icon CSS to _Layout.cshtml (will be done in Task 5)**

This step is handled in the next task. Just verify the files exist.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/lib/tabler/css/tabler-icons.min.css \
      Application/Entegrasyon.MVC/wwwroot/lib/tabler/fonts/
git commit -m "chore: add Tabler Icons webfont for navbar and sidebar icons"
```

---

### Task 5: _Layout.cshtml — Combo Layout + FOUC Script + Tema Toggle

**Files:**
- Modify: `Application/Entegrasyon.MVC/Shared/Views/_Layout.cshtml`

- [ ] **Step 1: Rewrite _Layout.cshtml with combo layout structure**

Replace the entire content with:

```html
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
    <meta http-equiv="X-UA-Compatible" content="ie=edge" />
    <title>@ViewData.GetPageTitle() - Entegrasyon</title>
    <meta name="csrf-token" content="@Antiforgery.GetAndStoreTokens(Context).RequestToken" />
    @* FOUC Prevention: tema CSS'den önce uygulanır *@
    <script>
        (function() {
            var theme = localStorage.getItem('tabler-theme') || 'light';
            document.documentElement.setAttribute('data-bs-theme', theme);
        })();
    </script>
    <link rel="stylesheet" href="~/lib/tabler/css/tabler.min.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/lib/tabler/css/tabler-icons.min.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body class="layout-fluid">
    <div class="page">
        @* ── Üst Navbar ── *@
        <partial name="~/Shared/Views/_Navbar.cshtml" />

        <div class="page-wrapper">
            @* ── Sol Sidebar (dinamik, grup bazlı) ── *@
            <partial name="~/Shared/Views/_Sidebar.cshtml" />

            <div class="page-body">
                <div class="container-xl">
                    @* ── Sayfa Başlığı ── *@
                    <div class="page-header d-print-none mb-3">
                        <div class="row align-items-center">
                            <div class="col-auto">
                                <h2 class="page-title">@ViewData.GetPageTitle()</h2>
                            </div>
                        </div>
                    </div>

                    <div id="toast-container"></div>
                    <partial name="~/Shared/Views/Partials/_Toast.cshtml" />
                    <div id="modal-container"></div>
                    @RenderBody()
                </div>
            </div>
        </div>
    </div>

    <script src="~/lib/tabler/js/tabler.min.js" asp-append-version="true"></script>
    <script src="~/lib/htmx/htmx.min.js" asp-append-version="true"></script>
    <script src="~/lib/apexcharts/apexcharts.min.js" asp-append-version="true"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @* SignalR sadece Chat sayfasında yüklenir — SSE bildirimler için gerekli değil *@
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

- [ ] **Step 2: Verify build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/Shared/Views/_Layout.cshtml
git commit -m "feat(layout): implement combo layout with FOUC-free theme support"
```

---

### Task 6: Tema Toggle JavaScript

**Files:**
- Modify: `Application/Entegrasyon.MVC/wwwroot/js/site.js`

- [ ] **Step 1: Add theme toggle and chart theme hook to site.js**

Add the following section to the **top** of `site.js`, before the HTMX config:

```javascript
// ── Theme Toggle ─────────────────────────────────────────────────────

(function () {
    var toggle = document.getElementById('theme-toggle');
    var icon = document.getElementById('theme-icon');
    if (!toggle || !icon) return;

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('tabler-theme', theme);
        icon.className = theme === 'dark' ? 'ti ti-sun' : 'ti ti-moon';
        onThemeChange(theme);
    }

    // Sayfa yüklendiğinde ikon durumunu ayarla
    var current = localStorage.getItem('tabler-theme') || 'light';
    icon.className = current === 'dark' ? 'ti ti-sun' : 'ti ti-moon';

    toggle.addEventListener('click', function (e) {
        e.preventDefault();
        var next = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        applyTheme(next);
    });
})();

// ── Chart Theme Hook ─────────────────────────────────────────────────

function onThemeChange(theme) {
    var isDark = theme === 'dark';
    var textColor = isDark ? '#a0aec0' : '#666';
    var gridColor = isDark ? '#2c3e56' : '#e0e0e0';

    document.querySelectorAll('[data-apex-chart]').forEach(function (el) {
        var chart = ApexCharts.getChartByID(el.id);
        if (chart) {
            chart.updateOptions({
                theme: { mode: theme },
                xaxis: { labels: { style: { colors: textColor } } },
                yaxis: { labels: { style: { colors: textColor } } },
                grid: { borderColor: gridColor }
            });
        }
    });
}
```

- [ ] **Step 2: Verify build and no JS syntax errors**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded (JS is static, no compile-time check — will verify in browser)

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/js/site.js
git commit -m "feat(layout): add theme toggle and chart theme hook"
```

---

### Task 7: _TopBar.cshtml Temizliği

**Files:**
- Delete: `Application/Entegrasyon.MVC/Shared/Views/_TopBar.cshtml`

`_TopBar.cshtml`'in tüm işlevi artık `_Navbar.cshtml` ve `_Layout.cshtml`'deki page-header tarafından karşılanıyor. Eski dosyayı silelim.

- [ ] **Step 1: Verify _TopBar is not referenced anywhere else**

Run: `grep -r "_TopBar" Application/Entegrasyon.MVC/ --include="*.cshtml" --include="*.cs"`

Expected: Only reference should be the old `_Layout.cshtml` (which we already replaced). If other files reference it, update them first.

- [ ] **Step 2: Delete _TopBar.cshtml**

Run: `rm Application/Entegrasyon.MVC/Shared/Views/_TopBar.cshtml`

- [ ] **Step 3: Verify build**

Run: `dotnet build Application/Entegrasyon.MVC/Entegrasyon.MVC.csproj --verbosity minimal`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.MVC/Shared/Views/_TopBar.cshtml
git commit -m "chore: remove _TopBar.cshtml, replaced by _Navbar.cshtml"
```

---

### Task 8: site.css — Tema Uyumlu Wizard Stilleri

**Files:**
- Modify: `Application/Entegrasyon.MVC/wwwroot/css/site.css`

Mevcut `.wizard-steps` custom CSS'ini kaldır — Tabler'ın native `steps steps-counter` bileşenine geçilecek (Faz 2'de). Şimdilik sadece tema uyumluluğu bozacak hardcoded renkleri düzelt.

- [ ] **Step 1: Replace hardcoded colors with CSS variables**

Replace the `.wizard-steps` section in `site.css`:

```css
/* Wizard step indicators — Tabler tema uyumlu */
.wizard-steps {
    display: flex;
    gap: 1rem;
}
.wizard-steps .wizard-step {
    padding: 0.5rem 1rem;
    border-radius: 4px;
    background: var(--tblr-bg-surface-secondary);
    color: var(--tblr-secondary);
    font-size: 0.875rem;
}
.wizard-steps .wizard-step.active {
    background: var(--tblr-primary);
    color: #fff;
    font-weight: bold;
}
.wizard-steps .wizard-step.done {
    background: var(--tblr-success);
    color: #fff;
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/Entegrasyon.MVC/wwwroot/css/site.css
git commit -m "fix(css): use CSS variables for theme-compatible wizard styles"
```

---

### Task 9: Build + Test Doğrulaması

**Files:** None (verification only)

- [ ] **Step 1: Full build**

Run: `dotnet build Entegrasyon.sln --verbosity minimal`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: Run unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --verbosity minimal`
Expected: All tests pass

- [ ] **Step 3: Run MVC tests**

Run: `dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --verbosity minimal`
Expected: All tests pass (including new ViewDataExtensionsTests)

- [ ] **Step 4: Run AdminPanel tests**

Run: `dotnet test Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj --verbosity minimal`
Expected: All tests pass

- [ ] **Step 5: Commit verification complete**

No commit needed — this is a verification step. If any tests fail, fix the issues before proceeding.

---

### Task 10: Görsel Doğrulama

**Files:** None (manual verification)

- [ ] **Step 1: Start the application**

Run: `cd Application/Entegrasyon.MVC && dotnet run`

- [ ] **Step 2: Verify in browser**

Open `http://localhost:5100` and check:
1. Üst navbar görünür — marka, grup menüleri, arama, hızlı işlemler, tema toggle, bildirim, kullanıcı
2. Dashboard'da sidebar gizli (bağımsız link)
3. "Yönetim" grubuna tıklayınca sidebar'da ürünler/kategoriler/markalar vb. görünür
4. Tema toggle çalışıyor — dark/light geçişi anında
5. Sayfa yenilendiğinde tema korunur (FOUC yok)
6. Mobil görünümde navbar hamburger menü çalışıyor
7. Permission tag helper'ları çalışıyor (yetkisiz menüler gizli)

- [ ] **Step 3: Fix any visual issues found**

Address layout, spacing, or icon issues discovered during verification.

- [ ] **Step 4: Final commit if fixes needed**

```bash
git add -A
git commit -m "fix(layout): visual fixes for combo layout"
```
