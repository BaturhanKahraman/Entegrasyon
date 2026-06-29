# EF No-Tracking Persist Bug Audit (#19) — 2026-06-12

**Kaynak:** SWE-Ahmet, gece takım oturumu. Global `NoTracking` (TenantDbContextFactory default) yüzünden, entity'yi **LINQ-load (FirstOrDefault/Single, `AsTracking` YOK) + mutate + `SaveChanges`** deseninde mutasyon **sessizce persist ETMİYOR** (`Update`/`Attach`/`ExecuteUpdate` da yoksa). `FindAsync` track eder → güvenli (Microsoft Docs ile doğrulandı).

98 dosya tarandı → 30 marker'sız şüpheli → aşağıdakiler **doğrulandı**.

## ✅ FİX EDİLDİ (8 metod — commit c45b3b0d + 2d387d5e)
- 🔴 `MarketPlaceManager.UpdateCredentialsAsync` — pazaryeri ApiKey/ApiSecret/SellerId/BaseUrl kaydı (#5 credential feature'ın yazma yolu). [c45b3b0d]
- 🔴 `OrderManager.UpdateOrderStatusAsync` + `UpdateOrderByShipmentPackageAsync` — sipariş durumu + kargo takip no. [c45b3b0d]
- 🟠 `ReceiptTemplateManager.UpdateAsync` / `UploadLogoAsync` / `DeleteLogoAsync` (fiş şablonu + logo). [2d387d5e]
- 🟠 `CartManager.ApplyCouponAsync` / `RemoveCouponAsync` (storefront kupon). [2d387d5e]
- Regression guard: `Test/Entegrasyon.IntegrationTest/Business/NoTrackingPersistRegressionTests.cs` (temsilci: credentials + order-status, RED→GREEN 2/2).

## ✅ FİX EDİLDİ (15 mutasyon-site, 6 servis — 2026-06-29)

### Marketplace sync servisleri — `ProductMarketplace` Status/ExternalProductId/BatchRequestId/LastSyncedAt artık persist EDİYOR. Fix: mutate edilen her `ProductMarketplace` LINQ-load'una `.AsTracking()` eklendi (read-only load'lara DOKUNULMADI).
- `TrendyolProductService` — PublishProductAsync (BatchRequestId/StatusMessage), DeleteProductAsync (Status=Failed). `UpdateApprovedContentAsync` read-only (`AsNoTracking`) → dokunulmadı.
- `N11RestProductService` — SaveProductAsync (`.Include(VariantOverrides)`'lı), DeleteProductAsync, UpdateProductBasicAsync, SetSellingStatusAsync (Start/Stop).
- `N11ProductService` (SOAP) — SaveProductAsync, DeleteProductAsync, UpdateProductBasicAsync, StartSellingAsync, StopSellingAsync.
- `AmazonProductService` — PublishProductAsync (ExternalProductId/BatchRequestId/Status). `DeleteProductAsync` read-only → dokunulmadı.
- `HepsiburadaProductService` — PublishProductAsync (BatchRequestId).
- `N11RestStockPriceService` — UpdatePriceAsync, UpdateStockAsync (BatchRequestId).

**RED→GREEN guard:** `Test/Entegrasyon.IntegrationTest/Business/MarketplaceSyncPersistRegressionTests.cs` — 6 servis için temsilci persist testi (gerçek no-tracking `IDbContextFactory` + mock'lu dış API). Fix öncesi 6/6 FAIL (sessiz no-op kanıtı), fix sonrası 6/6 PASS. Unit baseline 1825/1825, ilgili integration 11/11 (NoTrackingPersist + ProductSend) korundu.

## ✅ TEMİZ (doğrulandı, dokunma)
ProductManager (#75'te AsTracking ile düzeltildi), PricingRuleManager, ReturnReasonManager, ProductSyncManager (`FindAsync`), LabelTemplateManager (`Entry().State=Modified`), AttributeMatch/BrandMatch + import'lar (Add-only/read).

## Fix deseni
İlgili LINQ load'a `.AsTracking()` ekle (düşük risk, mekanik). Her fix'e RED-first integration test (persist doğrula) — `NoTrackingPersistRegressionTests` desenini kullan.

## ÖNERİ — CLAUDE.md kuralı
Tekrarı önlemek için EF bölümüne: "Mutasyon metodunda entity LINQ ile yükleniyorsa **`.AsTracking()` ŞART** (global NoTracking; yoksa `SaveChanges` sessiz no-op). Her mutasyon metoduna **integration-persist testi**." (`FindAsync` track eder, istisna.)
