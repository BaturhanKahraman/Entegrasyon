-- =============================================================================
-- IntegrationDb_Dev — Demo Seed
-- =============================================================================
-- UYGULAMA ZAMANI: IntegrationDb_Dev'e tüm migration'lar uygulandıktan SONRA.
-- ÇALIŞTIRMA:
--   docker exec -i postgres_db psql -U baturhan -d IntegrationDb_Dev < integration_dev_seed.sql
-- IDEMPOTENT: Her INSERT ON CONFLICT DO NOTHING ile korunuyor.
--   Tekrar çalıştırılabilir, mevcut veriyi bozmaz.
-- KIRMIZI ÇİZGİ: IntegrationDb, IntegrationDb_Stage'e DOKUNMA.
-- =============================================================================

-- NOT: Admin login (admin / 123456789) bu SQL'de DEĞİL — boot-time C# seeder'da:
--   Application/Entegrasyon.MVC/Infrastructure/DevMode/DevAdminSeeder.cs
-- Sebep: C# seeder her boot'ta çalışır (deploy + local-debug), idempotent-koruyucudur
-- (geliştiricinin app üzerinden elle değiştirdiği parolayı EZMEZ). Bu dosya yalnızca
-- DEMO İÇERİK (kategori/ürün vb.) sağlar — tek sorumluluk ayrımı.

BEGIN;

-- ─────────────────────────────────────────────
-- 1. Kategoriler
-- ─────────────────────────────────────────────
INSERT INTO "Categories" ("Id", "Name", "SuperCategoryId", "ImportSource", "IsImported",
    "IsFavorite", "IsDeleted", "DefaultVatRate",
    "CreatedAt", "UpdatedAt", "DeletedAt")
VALUES
  (1001, 'Çocuk Giyim',        NULL, 0, false, true,  false, 10,
   NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00'),
  (1002, 'Bebek Giyim',        1001, 0, false, false, false, 10,
   NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00'),
  (1003, 'Oyuncak',            NULL, 0, false, false, false, 18,
   NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00')
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────
-- 2. Marka
-- ─────────────────────────────────────────────
INSERT INTO "Brands" ("Id", "Name", "NormalizedName", "IsDeleted",
    "CreatedAt", "UpdatedAt", "DeletedAt")
VALUES
  (1001, 'DemoBrand', 'DEMOBRAND', false,
   NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00')
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────
-- 3. Ürünler (MainProducts TPH)
-- ─────────────────────────────────────────────
-- Product.Id = UUID, CategoryId referansı, BrandId nullable
-- SearchVector = generated column (tsvector), INSERT'e dahil edilmez; DB otomatik üretir.
INSERT INTO "MainProducts"
  ("Id", "Title", "CategoryId", "BrandId", "Description",
   "IsDeleted", "IsPublished", "IsPreOrder",
   "StockCode", "Season", "Year",
   "CreatedAt", "UpdatedAt", "DeletedAt")
VALUES
  ('a0000000-0000-0000-0000-000000000001',
   'Demo Ürün A – Kız Elbise',
   1001, 1001,
   'Geliştirme ortamı için örnek ürün. Pamuklu, baskılı kız çocuğu elbisesi.',
   false, true, false,
   'DEMO-A', '2025 İlkbahar', '2025',
   NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00'),
  ('a0000000-0000-0000-0000-000000000002',
   'Demo Ürün B – Erkek Şort',
   1001, 1001,
   'Geliştirme ortamı için örnek ürün. Penye kumaş erkek çocuğu şortu.',
   false, true, false,
   'DEMO-B', '2025 İlkbahar', '2025',
   NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00'),
  ('a0000000-0000-0000-0000-000000000003',
   'Demo Ürün C – Bebek Tulum',
   1002, 1001,
   'Geliştirme ortamı için örnek ürün. Organik pamuklu bebek tulumu.',
   false, false, false,
   'DEMO-C', '2025 Sonbahar', '2025',
   NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00')
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────
-- 4. Ürün Varyantları (ProductVariants)
-- ─────────────────────────────────────────────
-- Id = UUID, ProductId FK, Barcode unique, CurrencyType zorunlu
INSERT INTO "ProductVariants"
  ("Id", "ProductId", "Name", "Barcode",
   "ListPrice", "SalePrice", "ECommercePrice", "CostPrice",
   "VatRate", "DimensionalWeight", "CurrencyType",
   "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt")
VALUES
  -- Ürün A varyantları (S, M)
  ('b0000000-0000-0000-0000-000000000001',
   'a0000000-0000-0000-0000-000000000001',
   'Demo Ürün A – Beden S', 'DEMO-A-S-001',
   249.90, 199.90, 199.90, 90.00,
   10, 0.3, 'TRY',
   false, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00'),
  ('b0000000-0000-0000-0000-000000000002',
   'a0000000-0000-0000-0000-000000000001',
   'Demo Ürün A – Beden M', 'DEMO-A-M-001',
   249.90, 199.90, 199.90, 90.00,
   10, 0.35, 'TRY',
   false, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00'),
  -- Ürün B varyantı (tek)
  ('b0000000-0000-0000-0000-000000000003',
   'a0000000-0000-0000-0000-000000000002',
   'Demo Ürün B – Standart', 'DEMO-B-STD-001',
   149.90, 119.90, 119.90, 55.00,
   10, 0.25, 'TRY',
   false, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00'),
  -- Ürün C varyantı (yayınlanmamış)
  ('b0000000-0000-0000-0000-000000000004',
   'a0000000-0000-0000-0000-000000000003',
   'Demo Ürün C – 0-3 Ay', 'DEMO-C-0-3-001',
   189.90, 159.90, 159.90, 70.00,
   10, 0.2, 'TRY',
   false, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC', '0001-01-01 00:00:00+00')
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────
-- 5. Stok (BranchOfficeStocks)
-- ─────────────────────────────────────────────
-- BranchOffice Id=1 migration HasData ile geliyor (Merkez Ofis)
-- CurrentStock computed column (FirstTotalStock - SoldQuantity)
INSERT INTO "BranchOfficeStocks"
  ("BranchOfficeId", "ProductVariantId", "FirstTotalStock", "SoldQuantity")
VALUES
  (1, 'b0000000-0000-0000-0000-000000000001', 50, 5),
  (1, 'b0000000-0000-0000-0000-000000000002', 40, 2),
  (1, 'b0000000-0000-0000-0000-000000000003', 30, 8),
  (1, 'b0000000-0000-0000-0000-000000000004', 20, 0)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────
-- 6. Sipariş (Orders + OrderItems)
-- ─────────────────────────────────────────────
-- MarketPlaceId: migration HasData'dan 1 = Trendyol varsayılıyor
-- MarketplaceOrderStatus, OrderNumber, CustomerFirstName/LastName anonim
INSERT INTO "Orders"
  ("Id", "OrderNumber", "MarketPlaceId",
   "CustomerFirstName", "CustomerLastName", "CustomerEmail",
   "OrderDate",
   "MarketplaceOrderStatus",
   "IsDeleted", "IsFastDelivery", "IsGiftWrapped", "HideInvoice", "IsMicro",
   "CreatedAt", "UpdatedAt", "DeletedAt")
VALUES
  ('c0000000-0000-0000-0000-000000000001',
   'DEMO-ORD-0001', 1,
   'Demo', 'Müşteri', 'demo@example.local',
   NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days',
   'Created',
   false, false, false, false, false,
   NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days',
   NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days',
   '0001-01-01 00:00:00+00'),
  ('c0000000-0000-0000-0000-000000000002',
   'DEMO-ORD-0002', 1,
   'Test', 'Alıcı', 'test@example.local',
   NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
   'Picking',
   false, true, false, false, false,
   NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
   NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
   '0001-01-01 00:00:00+00')
ON CONFLICT DO NOTHING;

-- OrderItems: PK serial, unique constraint yok → ON CONFLICT yeterli değil.
-- EXISTS kontrolüyle idempotency sağlıyoruz (OrderId + Barcode kombinasyonu).
INSERT INTO "OrderItems"
  ("OrderId", "ProductId", "Barcode", "MerchantSku",
   "Quantity", "ReturnedQuantity", "UnitPrice",
   "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt")
SELECT
  'c0000000-0000-0000-0000-000000000001',
  'b0000000-0000-0000-0000-000000000001',
  'DEMO-A-S-001', 'DEMO-A',
  2, 0, 199.90,
  false,
  NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days',
  NOW() AT TIME ZONE 'UTC' - INTERVAL '2 days',
  '0001-01-01 00:00:00+00'
WHERE NOT EXISTS (
  SELECT 1 FROM "OrderItems"
  WHERE "OrderId" = 'c0000000-0000-0000-0000-000000000001'
    AND "Barcode" = 'DEMO-A-S-001'
);

INSERT INTO "OrderItems"
  ("OrderId", "ProductId", "Barcode", "MerchantSku",
   "Quantity", "ReturnedQuantity", "UnitPrice",
   "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt")
SELECT
  'c0000000-0000-0000-0000-000000000002',
  'b0000000-0000-0000-0000-000000000003',
  'DEMO-B-STD-001', 'DEMO-B',
  1, 0, 119.90,
  false,
  NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
  NOW() AT TIME ZONE 'UTC' - INTERVAL '1 day',
  '0001-01-01 00:00:00+00'
WHERE NOT EXISTS (
  SELECT 1 FROM "OrderItems"
  WHERE "OrderId" = 'c0000000-0000-0000-0000-000000000002'
    AND "Barcode" = 'DEMO-B-STD-001'
);

COMMIT;

-- =============================================================================
-- DOĞRULAMA SORGULARI (isteğe bağlı, çalıştır ve kontrol et)
-- =============================================================================
-- SELECT COUNT(*) AS categories FROM "Categories" WHERE "Id" >= 1001;
-- SELECT COUNT(*) AS products FROM "MainProducts" WHERE "StockCode" LIKE 'DEMO%';
-- SELECT COUNT(*) AS variants FROM "ProductVariants" WHERE "Barcode" LIKE 'DEMO%';
-- SELECT COUNT(*) AS orders FROM "Orders" WHERE "OrderNumber" LIKE 'DEMO%';
