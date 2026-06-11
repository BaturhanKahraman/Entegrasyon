# EF No-Tracking Persist Bug Audit (#19) — 2026-06-12

**Kaynak:** SWE-Ahmet, gece takım oturumu. Global `NoTracking` (TenantDbContextFactory default) yüzünden, entity'yi **LINQ-load (FirstOrDefault/Single, `AsTracking` YOK) + mutate + `SaveChanges`** deseninde mutasyon **sessizce persist ETMİYOR** (`Update`/`Attach`/`ExecuteUpdate` da yoksa). `FindAsync` track eder → güvenli (Microsoft Docs ile doğrulandı).

98 dosya tarandı → 30 marker'sız şüpheli → aşağıdakiler **doğrulandı**.

## ✅ FİX EDİLDİ (commit c45b3b0d)
- 🔴 `MarketPlaceManager.UpdateCredentialsAsync` — pazaryeri ApiKey/ApiSecret/SellerId/BaseUrl kaydı (#5 credential feature'ın yazma yolu).
- 🔴 `OrderManager.UpdateOrderStatusAsync` + `UpdateOrderByShipmentPackageAsync` — sipariş durumu + kargo takip no.
- Regression guard: `Test/Entegrasyon.IntegrationTest/Business/NoTrackingPersistRegressionTests.cs`.

## 🔴🟠 KALAN — FİX GEREK (solo/ayrı oturum, hepsi tek-satır `.AsTracking()`)

### 🔴 Marketplace sync servisleri — `ProductMarketplace` Status/ExternalProductId/LastSyncedAt persist etmiyor → her sync "hiç gönderilmemiş" gibi davranır (CORE entegrasyon bug'ı)
- `N11RestProductService` (Delete ~satır 246 `pm.Status=Pending` + publish yolları)
- `N11ProductService`
- `TrendyolProductService`
- `AmazonProductService`
- `HepsiburadaProductService`
- `N11RestStockPriceService`
(Her serviste 2–4 metod; `ProductMarketplace` FirstOrDefault ile yüklenip mutate ediliyor.)

### 🟠 Orta
- `ReceiptTemplateManager` — `UpdateAsync` / `UploadLogoAsync` / `DeleteLogoAsync` (fiş şablonu + logo)
- `CartManager.ApplyCouponAsync` / `RemoveCouponAsync` (storefront kupon)

## ✅ TEMİZ (doğrulandı, dokunma)
ProductManager (#75'te AsTracking ile düzeltildi), PricingRuleManager, ReturnReasonManager, ProductSyncManager (`FindAsync`), LabelTemplateManager (`Entry().State=Modified`), AttributeMatch/BrandMatch + import'lar (Add-only/read).

## Fix deseni
İlgili LINQ load'a `.AsTracking()` ekle (düşük risk, mekanik). Her fix'e RED-first integration test (persist doğrula) — `NoTrackingPersistRegressionTests` desenini kullan.

## ÖNERİ — CLAUDE.md kuralı
Tekrarı önlemek için EF bölümüne: "Mutasyon metodunda entity LINQ ile yükleniyorsa **`.AsTracking()` ŞART** (global NoTracking; yoksa `SaveChanges` sessiz no-op). Her mutasyon metoduna **integration-persist testi**." (`FindAsync` track eder, istisna.)
