# Integration Test Infrastructure Design

**Tarih:** 2026-03-23
**Durum:** Onaylandı
**Branch:** develop

---

## 1. Motivasyon

Mevcut test altyapısı:
- **Unit testler** (`Test/Entegrasyon.Test/`) — xUnit + Moq + FluentAssertions, ~97 test. Mock DbContext ile çalışır, gerçek DB davranışını (constraint, cascade, collation, migration) test etmez.
- **bUnit testler** (`Test/Entegrasyon.BunitTest/`) — Blazor component testleri, 7 test.
- **E2E testler** (`Test/Entegrasyon.E2E/`) — Playwright, 25 test. Tam uygulama gerektirir.
- **AdminPanel testler** (`Test/Entegrasyon.AdminPanel.Test/`) — MVC controller testleri, 39 test.

**Eksik olan:** Unit ve E2E testleri arasındaki katman — gerçek PostgreSQL ile business manager akışlarını, DI container bütünlüğünü, migration tutarlılığını ve marketplace servis entegrasyonlarını test eden **integration testleri**.

---

## 2. Mimari Kararlar

### 2.1 Testcontainers (PostgreSQL)
Her test run'ında otomatik olarak bir PostgreSQL 16 container'ı ayağa kalkar. Geliştirici makinesinde sadece Docker gerekir — önceden bir DB kurulumu yapılmasına gerek yoktur.

**Neden Testcontainers?**
- Gerçek PostgreSQL davranışı (collation, advisory lock, materialized view)
- CI/CD'de de aynı şekilde çalışır
- Container yaşam döngüsü test framework tarafından yönetilir

### 2.2 WebApplicationFactory
`Microsoft.AspNetCore.Mvc.Testing` ile `Program.cs` üzerine kurulu tam DI container'ı. Background service'ler test ortamında devre dışı bırakılır.

### 2.3 Respawn
Her test sonrası DB'yi `TRUNCATE` ile temizler. Migration'ı yeniden çalıştırmaktan 10-50x daha hızlı. Seed data korunur (configurable skip tabloları).

### 2.4 Shared PostgreSQL Container
Tek bir PostgreSQL container tüm test class'ları tarafından paylaşılır (`ICollectionFixture`). Her test class'ı kendi veritabanını oluşturabilir veya Respawn ile aynı DB'yi temizler.

---

## 3. Proje Yapısı

```
Test/Entegrasyon.IntegrationTest/
├── Entegrasyon.IntegrationTest.csproj
├── Usings.cs
├── Fixtures/
│   ├── PostgreSqlFixture.cs          # Testcontainers PostgreSQL lifecycle
│   ├── IntegrationTestWebAppFactory.cs # WebApplicationFactory<Program> override
│   └── IntegrationTestBase.cs         # IAsyncLifetime — Respawn reset
├── Collections/
│   └── IntegrationTestCollection.cs   # xUnit collection definition
├── DiContainerTests.cs                # DI resolve testleri
├── MigrationTests.cs                  # EF Core migration testleri
├── Business/
│   ├── ProductManagerIntegrationTests.cs
│   ├── CategoryManagerIntegrationTests.cs
│   └── OrderImportIntegrationTests.cs
└── appsettings.IntegrationTest.json
```

---

## 4. NuGet Paketleri

| Paket | Versiyon | Amaç |
|---|---|---|
| `Testcontainers.PostgreSql` | 4.* | PostgreSQL container yönetimi |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0.* | WebApplicationFactory |
| `Respawn` | 6.* | Test-arası DB temizleme |
| `FluentAssertions` | 6.12.0 | Assert (mevcut ile tutarlı) |
| `xunit` | 2.7.0 | Test framework (mevcut ile tutarlı) |
| `Microsoft.NET.Test.Sdk` | 17.9.0 | Test runner |

---

## 5. Test Kategorileri

### 5.1 DI Container Testleri
Tüm registered servisler (`AddApplicationDependencies`, `AddValidators`, `AddEventChannels`, `AddStorageServices`, `AddNotification`) resolve edilebiliyor mu?

### 5.2 EF Core Migration Testleri
- Tüm migration'lar boş DB'ye sorunsuz uygulanıyor mu?
- `HasData` seed verileri doğru mu?

### 5.3 Business Manager Testleri (Gerçek DB)
- **ProductManager:** AddProduct -> DB'de product + variant + stock doğrula, duplicate StockCode kontrolü
- **CategoryManager:** AddCategory, SoftDelete (ürünü olan kategori silinemez), Update
- **OrderManager:** GetOrdersAsync, import akışı

### 5.4 Marketplace Servis Testleri (gelecekte)
- Mock HTTP + gerçek DB ile sipariş/kategori import akışları
- Bu spec'te temel altyapı hazırlanır, marketplace testleri ayrı PR'da eklenecek

---

## 6. Teknik Detaylar

### 6.1 Background Service Devre Dışı Bırakma
`IntegrationTestWebAppFactory` içinde tüm `IHostedService` kayıtları kaldırılır. Bu, test ortamında polling/sync servislerinin çalışmasını önler.

### 6.2 Mock Marketplace
Tüm marketplace servisleri `UseMock: true` ile çalışır (appsettings override). Gerçek API çağrısı yapılmaz.

### 6.3 MinIO / Redis Mock
- MinIO: `IMinioFileStorage` mock'lanır (NullObject pattern)
- Redis: `AddDistributedMemoryCache()` zaten in-memory fallback sağlar

### 6.4 Collation
PostgreSQL `icu` collation (`CaseInsensitive`) kullanılır. Testcontainers'daki PostgreSQL 16 Alpine bu collation'ı destekler.

### 6.5 Materialized View
`mv_category_summary` ve `mv_product_stock_summary` — migration'larda `CREATE MATERIALIZED VIEW` olarak tanımlanmıştır. Migration testleri bu view'ların oluşturulduğunu doğrular.

---

## 7. Docker Gerekliliği

Testler `[Trait("Category", "Integration")]` ile etiketlenir. Docker mevcut değilse:
```bash
dotnet test --filter "Category!=Integration"
```
ile skip edilebilir.

---

## 8. CI/CD Entegrasyonu (gelecek)

GitHub Actions workflow'una eklenecek adım:
```yaml
- name: Integration Tests
  run: dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
  services:
    # Testcontainers kendi container'ını yönetir — ek servis gerekmez
```

---

## 9. Komutlar

```bash
# Tüm entegrasyon testlerini çalıştır
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj

# Sadece DI testleri
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~DiContainerTests"

# Integration testleri hariç tüm testler
dotnet test --filter "Category!=Integration"
```
