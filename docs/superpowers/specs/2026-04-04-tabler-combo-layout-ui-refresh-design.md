# Tabler Combo Layout + UI Refresh — Design Spec

**Tarih:** 2026-04-04
**Yaklaşım:** Design System First
**Kapsam:** Combo layout, dark/light tema toggle, tüm sayfalarda Tabler UI iyileştirmesi

---

## 1. Combo Layout Yapısı

### Mevcut Yapı
```
<div class="page">
  <aside class="navbar navbar-vertical">   ← sidebar (dark, tüm menüler)
  <div class="page-wrapper">
    <header class="page-header">            ← başlık + bildirim + kullanıcı
    <div class="page-body">                ← içerik
```

### Yeni Yapı
```
<html data-bs-theme="light">
<div class="page">
  <header class="navbar navbar-expand-md">  ← ÜST NAVBAR
  <div class="page-wrapper">
    <aside class="navbar navbar-vertical">  ← SOL SIDEBAR (dinamik alt menüler)
    <div class="page-wrapper">
      <div class="page-header">             ← sayfa başlığı + breadcrumb
      <div class="page-body">              ← içerik
```

### Üst Navbar İçeriği (soldan sağa)
1. **Marka:** "Entegrasyon" logosu + metin
2. **Ana grup menüleri:** Dashboard | Yönetim | Pazaryeri | Müşteriler | Raporlar | Mesajlar | Mağaza | Ayarlar
3. **Global arama çubuğu** — ürün/sipariş/müşteri arama
4. **Hızlı işlemler dropdown** — ürün ekle, satış yap vb.
5. **Tema toggle** — güneş/ay ikonu (dark↔light)
6. **Bildirim zili** — mevcut NotificationBell ViewComponent
7. **Kullanıcı dropdown** — profil, çıkış

Dashboard ve Mesajlar üst navbar'da bağımsız linklerdir (alt menüleri yoktur).

### Navbar Grup → Sidebar Alt Menü Eşleştirmesi

| Üst Navbar Grubu | Sidebar Alt Menüleri |
|---|---|
| **Yönetim** | Ürünler, Kategoriler, Özellikler, Markalar, POS, Satışlar, Siparişler, Kargo, Toplu İşlemler, Şubeler |
| **Pazaryeri** | Senkronizasyon, Eşleştirme, Siparişler, Komisyon Oranları, Şablonlar |
| **Müşteriler** | Müşteriler, Faturalama |
| **Raporlar** | Satış, Kar/Zarar, Performans, Uyarılar, Envanter, Pazaryeri |
| **Mağaza** | Ayarlar, Yasal, Bannerlar, Ödemeler, Yorumlar, İadeler, Kampanyalar, Satıcılar, Payoutlar |
| **Ayarlar** | Genel, Bildirimler, Entegrasyonlar, Yazdırma, Masaüstü, Kullanıcılar, Roller, Admin Bildirimleri |

**Not:** Kullanıcı Yönetimi (Kullanıcılar, Roller, Admin Bildirimleri) Ayarlar grubunun altına taşınmıştır.

### Sidebar Dinamik Davranışı
- Üst navbar'da bir grup tıklandığında sidebar o grubun alt menülerini gösterir
- Aktif sayfa sidebar'da vurgulanır (`active` class)
- Aktif grup üst navbar'da vurgulanır
- Dashboard veya Mesajlar tıklandığında sidebar gizlenir (alt menü yok)
- Sayfa yüklendiğinde URL'den aktif grup ve menü otomatik belirlenir

---

## 2. Tema Toggle Mekanizması

### Geçiş Yöntemi
- `<html data-bs-theme="light|dark">` attribute'ü toggle edilir
- Tüm sayfa (navbar, sidebar, içerik) aynı temayı takip eder — combo karışımı yapılmaz
- Sidebar dark modda koyu, light modda açık renk olur

### Kullanıcı Tercihi Saklama
- `localStorage.setItem('tabler-theme', 'dark'|'light')`
- Varsayılan: `light`

### FOUC Önleme (Flash of Unstyled Content)
`<head>` içinde, CSS dosyalarından **önce** render-blocking inline script:

```html
<head>
  <script>
    (function() {
      var theme = localStorage.getItem('tabler-theme') || 'light';
      document.documentElement.setAttribute('data-bs-theme', theme);
    })();
  </script>
  <!-- Tabler CSS sonra yüklenir -->
</head>
```

Bu script blocking olduğu için tarayıcı hiçbir şey çizmeden temayı uygular.

### Toggle Butonu
- Üst navbar'ın sağ tarafında
- Light modda: `ti ti-moon` ikonu (karanlığa geç)
- Dark modda: `ti ti-sun` ikonu (aydınlığa geç)
- Tıklandığında: attribute toggle + localStorage kaydet + ikon değiştir
- Sayfa yenilenmez — anında geçiş

---

## 3. Design System — UI Pattern Kuralları

### 3.1 İstatistik Kartları (Stat Cards)
**Kullanıldığı yerler:** Dashboard, Raporlar, MarketplaceSync

**Standart yapı:**
```html
<div class="card card-sm">
  <div class="card-body">
    <div class="row align-items-center">
      <div class="col-auto">
        <span class="bg-primary text-white avatar">
          <i class="ti ti-package icon"></i>
        </span>
      </div>
      <div class="col">
        <div class="subheader">TOPLAM ÜRÜN</div>
        <div class="h1 mb-0">1,000,005</div>
      </div>
      <div class="col-auto">
        <span class="text-green d-inline-flex align-items-center lh-1">
          7% <i class="ti ti-trending-up ms-1"></i>
        </span>
      </div>
    </div>
  </div>
</div>
```

**Kurallar:**
- Sol: renkli avatar içinde ikon
- Orta: `subheader` (küçük gri etiket) + `h1` (büyük sayı)
- Sağ: trend göstergesi — yeşil yukarı ok (`text-green` + `ti-trending-up`) veya kırmızı aşağı ok (`text-red` + `ti-trending-down`)
- Opsiyonel: kart altında sparkline mini grafik

### 3.2 Tablolar (Data Tables)
**Kullanıldığı yerler:** ~20+ feature (en yaygın pattern)

**Standart yapı:**
```html
<div class="card">
  <div class="card-header">
    <h3 class="card-title">Başlık</h3>
    <!-- opsiyonel: filtre/arama/butonlar -->
  </div>
  <div class="table-responsive">
    <table class="table table-vcenter card-table table-hover">
      <thead>...</thead>
      <tbody>...</tbody>
    </table>
  </div>
  <!-- opsiyonel: card-footer ile pagination -->
</div>
```

**Kurallar:**
- Class'lar: `table table-vcenter card-table table-hover`
- `table-responsive` wrapper ile mobil uyum
- Boş tablo durumunda `_EmptyState` partial gösterilir
- Satır aksiyonları: sağda dropdown veya ikon butonlar
- Pagination: `card-footer` içinde `_Pagination` partial

### 3.3 Formlar (Create/Edit)
**Kullanıldığı yerler:** ~15+ feature

**Standart yapı:**
```html
<div class="card">
  <div class="card-header">
    <h3 class="card-title">Form Başlığı</h3>
  </div>
  <div class="card-body">
    <form-group asp-for="Name" />
    <form-group asp-for="Description" />
  </div>
  <div class="card-footer text-end">
    <a class="btn btn-link" href="...">İptal</a>
    <button type="submit" class="btn btn-primary">Kaydet</button>
  </div>
</div>
```

**Kurallar:**
- Mevcut `<form-group asp-for>` tag helper korunur
- Form kart içinde: `card > card-header > card-body > card-footer`
- Submit butonu sağda (`text-end`), iptal butonu solda (link stil)
- Validation hataları input altında kırmızı text (mevcut davranış korunur)

### 3.4 Detail/Show Sayfaları
**Kullanıldığı yerler:** Siparişler, Ürünler, Kategoriler, Müşteriler

**Standart yapı:**
- Sol kolon (`col-lg-8`): ana bilgiler — geniş kart
- Sağ kolon (`col-lg-4`): yan bilgiler — dar kartlar (durum, tarihler, meta)
- `row row-deck row-cards` grid yapısı

### 3.5 Empty States
**Mevcut:** `_EmptyState.cshtml` partial

**İyileştirme — standart yapı:**
```html
<div class="empty">
  <div class="empty-icon">
    <i class="ti ti-package icon"></i>
  </div>
  <p class="empty-title">Henüz ürün yok</p>
  <p class="empty-subtitle text-secondary">İlk ürününüzü ekleyerek başlayın.</p>
  <div class="empty-action">
    <a href="..." class="btn btn-primary">
      <i class="ti ti-plus"></i> Ürün Ekle
    </a>
  </div>
</div>
```

**Kurallar:**
- Tabler `empty` bileşeni kullanılır
- Büyük gri ikon + başlık + açıklama + CTA butonu (opsiyonel)

### 3.6 Wizard (Multi-step)
**Kullanıldığı yerler:** Kategoriler (3 adım), Ürünler (3 adım), BulkOperations

**Standart yapı:**
```html
<div class="steps steps-counter mb-4">
  <a href="#" class="step-item active">Temel Bilgiler</a>
  <a href="#" class="step-item">Özellikler</a>
  <a href="#" class="step-item">Önizleme</a>
</div>
```

**Kurallar:**
- Tabler `steps steps-counter` bileşeni
- Aktif adım: `active` class
- Tamamlanan adımlar tıklanabilir
- HTMX ile adımlar arası geçiş (mevcut pattern korunur)

### 3.7 Grafikler (Charts)
**Kullanıldığı yerler:** Dashboard, Raporlar (ApexCharts)

**Kurallar:**
- Tema uyumlu renk paleti: CSS değişkenleri kullanılır
- Dark/light geçişinde grafik renkleri otomatik güncellenir
- Chart container: `card > card-body > div#chart`
- Renk paleti: `--tblr-primary`, `--tblr-info`, `--tblr-success`, `--tblr-warning`
- Tema değiştiğinde ApexCharts `updateOptions()` ile renk güncellenir

**Tema değişimi hook'u:**
```javascript
// Tema toggle edildiğinde chart'ları güncelle
function onThemeChange(theme) {
  var isDark = theme === 'dark';
  var textColor = isDark ? '#a0aec0' : '#666';
  var gridColor = isDark ? '#2c3e56' : '#e0e0e0';
  
  document.querySelectorAll('[data-apex-chart]').forEach(function(el) {
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

### 3.8 Modaller
**Kullanıldığı yerler:** POS, BranchOffices, onay dialogları

**Kurallar:**
- Mevcut `_ConfirmModal` partial korunur
- `modal-dialog-centered` class eklenir
- HTMX ile yüklenen modaller: mevcut pattern korunur
- Tema ile uyumlu (parent'tan `data-bs-theme` miras alır)

---

## 4. Dosya Değişiklikleri

### Değişecek Dosyalar
| Dosya | Değişiklik |
|---|---|
| `Shared/Views/_Layout.cshtml` | Combo layout yapısı, FOUC script, tema toggle |
| `Shared/Views/_Sidebar.cshtml` | Dinamik alt menü sistemi (grup bazlı) |
| `Shared/Views/_TopBar.cshtml` | Silinir — işlevi `_Navbar.cshtml`'e taşınır |
| `wwwroot/js/site.js` | Tema toggle JS, chart tema hook, arama fonksiyonu |
| `wwwroot/css/site.css` | Tema uyumlu özel stiller (varsa) |

### Yeni Dosyalar
| Dosya | Amaç |
|---|---|
| `Shared/Views/_Navbar.cshtml` | Üst navbar partial (`_TopBar.cshtml`'in yerini alır) |

### Her Feature View'da Yapılacak Ortak Değişiklikler
- Tablolar → `table-vcenter card-table table-hover` + `table-responsive`
- Formlar → kart yapısına al (card > card-header > card-body > card-footer)
- Empty state → Tabler `empty` bileşenine çevir
- Stat kartları → trend göstergesi + renkli avatar + ikon
- Grafikler → tema uyumlu renk paleti

---

## 5. Fazlama ve Uygulama Sırası

| Faz | Kapsam | PR |
|---|---|---|
| **Faz 0** | Layout altyapısı: combo layout, tema toggle, FOUC-free script, navbar grup→sidebar mekanizması | Ayrı PR |
| **Faz 1** | Dashboard: stat kartları (trend + sparkline), haftalık satış grafiği (tema uyumlu), pazaryeri durum, son aktiviteler, hızlı işlemler | Ayrı PR |
| **Faz 2** | Yönetim grubu: Ürünler (list + wizard + detail), Kategoriler (list + wizard + tree), Markalar, Özellikler, Siparişler, Kargo, POS, Satışlar, Toplu İşlemler, Şubeler | Ayrı PR |
| **Faz 3** | Pazaryeri grubu: sync overview, eşleştirme, komisyon oranları, şablonlar | Ayrı PR |
| **Faz 4** | Raporlar + Müşteriler: tüm rapor sayfaları (chart tema uyumu), müşteri list/detail, faturalama | Ayrı PR |
| **Faz 5** | Mağaza + Ayarlar + geri kalan: storefront, ayarlar, kullanıcı yönetimi, chat, profil, bildirimler | Ayrı PR |

---

## 6. Kısıtlar ve Kurallar

1. **Hiçbir faz iş mantığına dokunmaz** — sadece view/CSS/JS değişikliği
2. **Her faz sonunda tüm testler geçmeli** — unit + integration + E2E
3. **Mevcut HTMX pattern'leri korunur** — `hx-get`, `hx-swap`, lazy loading
4. **Permission tag helper'ları aynen kalır** — `require-permission`, `require-role`
5. **ViewData extension'ları korunur** — `SetPageTitle()`, `SetActiveNav()`, `SetBreadcrumb()`
6. **TempData extension'ları korunur** — `SetSuccess()`, `SetError()`, `GetToast()`
7. **Navbar aktif grup ve sidebar aktif menü, ViewData üzerinden belirlenir** — mevcut `GetActiveNav()` genişletilir
8. **Combo tema yapılmaz** — her iki modda da tüm sayfa aynı temayı takip eder
