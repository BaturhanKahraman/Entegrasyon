# EF No-Tracking Persist Bug Audit (#19) — 2026-06-12

**Kaynak:** SWE-Ahmet, gece takım oturumu. Global `NoTracking` (TenantDbContextFactory default) yüzünden, entity'yi **LINQ-load (FirstOrDefault/Single, `AsTracking` YOK) + mutate + `SaveChanges`** deseninde mutasyon **sessizce persist ETMİYOR** (`Update`/`Attach`/`ExecuteUpdate` da yoksa). `FindAsync` track eder → güvenli (Microsoft Docs ile doğrulandı).

98 dosya tarandı → 30 marker'sız şüpheli → aşağıdakiler **doğrulandı**.

## ✅ FİX EDİLDİ (8 metod — commit c45b3b0d + 2d387d5e)
- 🔴 `MarketPlaceManager.UpdateCredentialsAsync` — pazaryeri ApiKey/ApiSecret/SellerId/BaseUrl kaydı (#5 credential feature'ın yazma yolu). [c45b3b0d]
- 🔴 `OrderManager.UpdateOrderStatusAsync` + `UpdateOrderByShipmentPackageAsync` — sipariş durumu + kargo takip no. [c45b3b0d]
- 🟠 `ReceiptTemplateManager.UpdateAsync` / `UploadLogoAsync` / `DeleteLogoAsync` (fiş şablonu + logo). [2d387d5e]
- 🟠 `CartManager.ApplyCouponAsync` / `RemoveCouponAsync` (storefront kupon). [2d387d5e]
- Regression guard: `Test/Entegrasyon.IntegrationTest/Business/NoTrackingPersistRegressionTests.cs` (temsilci: credentials + order-status, RED→GREEN 2/2).

## 🔴 KALAN — FİX GEREK (solo/ayrı oturum; SABAH İLK İŞ)

### Marketplace sync servisleri — `ProductMarketplace` Status/ExternalProductId/LastSyncedAt persist etmiyor → her sync "hiç gönderilmemiş" gibi davranır (CORE entegrasyon bug'ı). API-coupled → WireMock-başarı-senaryosu persist testi gerek.
- `N11RestProductService` (~214 Delete `pm.Status=Pending`, ~86, ~276)
- `N11ProductService` (~111,152,212,283,331)
- `TrendyolProductService` (~122,256,276)
- `AmazonProductService` (~41,121)
- `HepsiburadaProductService` (~117)
- `N11RestStockPriceService` (~38,99)
(Fix yine tek-satır `.AsTracking()`; her metodun mutated-mı doğrulanmalı.)

## ✅ TEMİZ (doğrulandı, dokunma)
ProductManager (#75'te AsTracking ile düzeltildi), PricingRuleManager, ReturnReasonManager, ProductSyncManager (`FindAsync`), LabelTemplateManager (`Entry().State=Modified`), AttributeMatch/BrandMatch + import'lar (Add-only/read).

## Fix deseni
İlgili LINQ load'a `.AsTracking()` ekle (düşük risk, mekanik). Her fix'e RED-first integration test (persist doğrula) — `NoTrackingPersistRegressionTests` desenini kullan.

## ÖNERİ — CLAUDE.md kuralı
Tekrarı önlemek için EF bölümüne: "Mutasyon metodunda entity LINQ ile yükleniyorsa **`.AsTracking()` ŞART** (global NoTracking; yoksa `SaveChanges` sessiz no-op). Her mutasyon metoduna **integration-persist testi**." (`FindAsync` track eder, istisna.)
