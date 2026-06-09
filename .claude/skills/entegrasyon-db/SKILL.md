---
name: entegrasyon-db
description: Entegrasyon projesinde EF Core + PostgreSQL veritabanı işleri. Entity/DbContext değişikliği, IEntityTypeConfiguration, migration strict-rule (add → gözden geçir → update → has-pending-model-changes), multi-tenant izolasyon (DB-per-tenant, TenantDbContextFactory, tenant filtreli sorgu), BaseEntity soft-delete + UTC, no-tracking, index/performans, N+1 önleme. Entity ekler/değiştirir, migration üretir, DB şeması/sorgu optimize ederken kullan.
---

# Entegrasyon — Database (EF Core + PostgreSQL)

Bu projede DataAccess katmanı ve migration'larla çalışırken izlenecek kesin kurallar. Genel EF Core dokümanı için `microsoft-docs`; EF kod desenleri MVC bağlamında için `aspnet-mvc-htmx`.

## Migration — STRICT RULE (asla atlama)

Entity veya DbContext değiştiğinde:

```bash
# 1. Migration ekle
dotnet ef migrations add <MigrationName> -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext

# 2. Oluşan dosyayı GÖZDEN GEÇİR — gereksiz/duplicate kolon, beklenmedik drop var mı?

# 3. Dev DB'ye uygula
dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext

# 4. Snapshot senkron mu doğrula (çıktı "No changes" olmalı)
dotnet ef migrations has-pending-model-changes -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext
```

Migration olmadan entity değişikliği **TAMAMLANMIŞ SAYILMAZ**. Uygulanmış migration'ı silme/elle düzenleme — düzeltme gerekiyorsa yeni migration ekle.

## BaseEntity & konvansiyonlar

- Tüm entity'ler `BaseEntity`'den türer: `Id`, `IsDeleted`, `DeletedAt`, `CreatedAt`, `UpdatedAt`.
- Soft-delete: silme = `IsDeleted=true`. Sorgular silinmişleri dışlamalı (global query filter / manuel).
- Default **no-tracking** (okuma). Yazma için tracking gerektiğinde bilinçli aç.
- `SaveChangesAsync()` DateTime'ları otomatik UTC'ye çevirir — kod tarafında tekrar çevirme.
- Yeni entity → `Entegrasyon.DataAccess` altında `IEntityTypeConfiguration<T>` ekle (tablo adı, ilişkiler, index, kısıtlar).

## Multi-tenant (Strict)

- DB-per-tenant: `TenantDbContextFactory` tenant'a göre context üretir. Tenant resolution `HttpTenantContext`.
- **"Bu N tenant ile çalışır mı?"** her şema/sorgu kararında sor. Singleton in-memory state → `ConcurrentDictionary<int,T>` (key = tenantId / MarketPlace.Id). Lock → tenant-başı `SemaphoreSlim`.
- Tenant-specific config DB'den okunur, appsettings'e hardcode edilmez.

## Domain modeli (özet)

- Kategori/özellik: `CategoryAttribute` (Key, Humanized, AllowCustom, Values), `CategoryAttributeCategory` junction (IsRequired, IsVarianter, IsSlicer), `CategoryAttributeValue`, `AttributeKeyValue` (ürün↔değer).
- Marketplace eşleştirme: `CategoryAttributeMarketPlaceMatch`, `CategoryAttributeValueMarketPlaceMatch` (MarketPlaceId=1=Trendyol).
- Müşteri kalıtımı: `Customer` → `RetailCustomer` / `CorporateCustomer`.
- `BarcodeSequence` — ürün barkodu, arka planda temizlenir.

## Performans

- Index'leri migration ile bilinçli ekle. Sık filtrelenen/join'lenen kolonlar + tenant kolonları.
- N+1'den kaçın: gerekli `Include` veya projection (`Select` ile DTO'ya). Tüm entity'yi çekip bellekte filtreleme.
- Toplu update/delete: `ExecuteUpdate`/`ExecuteDelete` — ama soft-delete semantiğini koru (gerçek delete yerine `IsDeleted`).

## Test

Integration testleri Testcontainers ile geçici PostgreSQL açar (Docker gerekli), Respawn ile her test sonrası temizler (seed korunur):
```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```

## Kırmızı çizgiler

- Gerçek/prod DB'de drop/truncate/toplu delete YOK (dev DB ile çalış).
- Migration geçmişini bozma. Connection string/secret'ı log'a/commit'e yazma.
