# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build Entegrasyon.sln

# Run Blazor app
cd Application/Entegrasyon.Blazor && dotnet run

# Run tests
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj

# Run a single test
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TestClassName"

# Run E2E tests (uygulama debug modda ayakta olmalı — ayrı docker-compose gerekmez)
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj

# Run a single E2E test
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --filter "FullyQualifiedName~TestClassName"

# Apply EF Core migrations
dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor

# Add a new migration
dotnet ef migrations add <MigrationName> -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor
```

**Infrastructure (docker-compose):** PostgreSQL (5432)

**E2E Testleri:** Uygulama genelde debug modda ayaktadır. E2E testleri doğrudan `dotnet test` ile çalıştırılabilir — ayrı bir docker-compose ortamı başlatmaya gerek yoktur. Testler varsayılan olarak `http://localhost:5099` adresine bağlanır (`E2E_BASE_URL` env var ile değiştirilebilir).

## Architecture

Klasik katmanlı mimari — Entity → DataAccess → Business → Blazor:

| Proje | Görev |
|---|---|
| `Entegrasyon.Entity` | Domain modelleri ve DTOlar |
| `Entegrasyon.DataAccess` | EF Core context, migrations, entity configurations |
| `Entegrasyon.Business` | Business managers (Abstract + Concrete) |
| `Entegrasyon.ApplicationBootstrap` | DI container kurulumu |
| `Entegrasyon.Blazor` | Blazor Server UI (MudBlazor) |

## Key Patterns

**Primary Constructor DI** (C# 12):
```csharp
public class ProductManager(IntegrationDbContext dbContext) : IProductService
```
**Business Layer Method Structure (Strict Rule):**
Tüm Business Manager (`XxxManager.cs`) metodları KESİNLİKLE aşağıdaki 3 adımlı "Pipeline" akışını takip etmelidir. Asla bu sırayı bozma:

1. **Validation:** İşleme başlamadan önce `FluentValidation` ile DTO/objeyi doğrula. Başarısızsa, validasyon hatalarını içeren bir sonuç (Result) dön.
2. **Business Rules:** Validasyon geçerse, iş kurallarını (örn: stok kontrolü, benzersiz isim kontrolü vb.) `LogicRunner` ile kontrol et. Başarısızsa iş kuralı hatalarını dön.
3. **Execution:** Sadece üstteki iki adım başarılı olursa asıl işleme geç.


**Manager/Interface pattern:** Her business servisi için `Abstract/IXxxManager.cs` + `Concrete/XxxManager.cs`. Tümü `ApplicationDependencyExtension.cs` üzerinden DI'a kaydedilir.

**MudBlazor namespace çakışması:** `MudBlazor.CategoryAttribute` ile domain `CategoryAttribute` çakışır — Blazor dosyalarında alias kullan:
```csharp
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;
```

**MudDataGrid:** `Items` parametresi `IEnumerable<T>` ister — `List<T>` üzerinde `.AsEnumerable()` çağır.

**EF Core:** Default olarak no-tracking. `SaveChangesAsync()` otomatik UTC dönüşümü yapar. Tüm entity'ler `BaseEntity`'den türer (`IsDeleted`, `DeletedAt`, `CreatedAt`, `UpdatedAt`).

**Mapster:** DTO mapping için kullanılır. Profiller `Business/MapperProfiles/MappingConfig.cs` içinde.

**Event Channel pattern:** Background servisler arası iletişim için:
```csharp
EventChannel<CategoryUpdatedEvent> // publisher → subscriber
```

## Blazor & UI Development Standards

**Maximize .NET 8 & Blazor Features:** Sürekli olarak .NET 8'in sunduğu en modern özellikleri kullan. Etkileşimli render modlarını (InteractiveServer, InteractiveWebAssembly, InteractiveAuto) ve SSR (Server-Side Rendering) özelliklerini senaryoya en uygun ve performanslı olacak şekilde seç.

**Code-Behind Pattern (Strict Rule):** Hiçbir zaman `.razor` dosyalarının içine uzun C# kodları yazma. UI (HTML/Razor) ve iş mantığı kesinlikle ayrılmalıdır. Her `.razor` dosyasının mutlaka bir `.razor.cs` (code-behind) dosyası olmalıdır.

**Componentization:** Uzun ve karmaşık kodlardan kaçın. Temiz ve okunabilir bir altyapı için, küçük iş mantıklarını ve UI parçalarını alt component'lara (child components) ayır. Tek kullanımlık bile olsa, kodu modüler hale getirmek için component oluşturmaktan çekinme.

**Feature-Based Folder Structure:** Dosyaları teknik rollerine göre (`Pages`, `Components`) ayırmak yerine, ait oldukları özelliğe göre (`Features`) grupla. Örneğin bir kategoriye ait sayfa ve o sayfada kullanılan tek kullanımlık bileşenler aynı klasör dizininde (`Features/Categories/`) yer almalıdır. Sadece birden fazla feature tarafından ortak kullanılan yapıları (Layouts, genel buton componentları, genel dialoglar) Shared klasöründe tut.



## Domain Özeti

**Kategori & Özellik modeli:**
- `CategoryAttribute` — özellik tanımı (Key, Humanized, AllowCustom, Values)
- `CategoryAttributeCategory` — junction tablosu (IsRequired, IsVarianter, IsSlicer)
- `CategoryAttributeValue` — önceden tanımlı değerler
- `AttributeKeyValue` — ürün ↔ özellik değeri bağlantısı
- MarketPlace eşleştirmeleri: `CategoryAttributeMarketPlaceMatch`, `CategoryAttributeValueMarketPlaceMatch` (MarketPlaceId=1 = Trendyol)

**Müşteri kalıtımı:** `Customer` → `RetailCustomer` / `CorporateCustomer`

**Ürün barkod:** `BarcodeSequence` tablosu, arka planda temizlenir.

## DI Kayıt Yapısı

`ApplicationDependencyExtension.cs` içindeki extension methodlar:
- `AddApplicationDependencies()` — tüm manager'lar, event channel'lar, validator'lar, Mapster
- `AddCustomDbContext()` — PostgreSQL, no-tracking
- `AddBackgroundServices()` — Trendyol import, product publish
- `AddStorageServices()` — MinIO + ImageSharp
- `AddSignalRSettings()` + `AddNotification()` — real-time bildirimler

## Blazor Sayfa Yapısı

- Pages: category, product (çok adımlı ekleme), attributes, customers, users, admin/role-management, sales, marketplace-sync, brand
- Components/Dialogs: `CategoryDialog`, `CustomerDialog`, `RoleDialog`, `UserDialog`, `ProductImageUploadDialog`
- Components/Shared: layout, nav, category tree, attribute detail paneli
- SignalR hub: `/NotificationHub`

## Development Workflow (Strict Rule)

  **TDD-First:** Her yeni özellik ve bug fix için KESİNLİKLE şu sıra izlenir:
  1. Önce testi yaz (RED)
  2. Testi çalıştır, başarısız olduğunu doğrula
  3. Minimum kodu implement et (GREEN)
  4. Testi çalıştır, geçtiğini doğrula
  5. Refactor et (gerekiyorsa)
  6. Tüm testleri çalıştır: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
  7. Entegrasyon testini çalıştır (eklenecek)
  8. E2E testini çalıştır: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj`

  Test olmadan özellik tamamlanmış SAYILMAZ. "Testleri sonra yazarız" KABUL EDİLMEZ.

## Multi-Tenant Design (Strict Rule)

  Bu sistem ileride **multi-tenant** yapılacak. Tüm yeni geliştirmelerde tenant izolasyonunu göz önünde bulundur:

  - **Singleton servislerde in-memory state:** Tek bir field yerine `ConcurrentDictionary<int, T>` kullan (key = tenantId veya MarketPlace.Id). Özellikle OAuth token cache'leri bu kurala TABİ.
  - **SemaphoreSlim:** Tenant başına izole lock mekanizması kullan, global tek lock değil.
  - **DB query'leri:** Tüm sorgularda tenant filtresi uygulanabilir olmalı.
  - **Configuration:** Tenant-specific config'ler DB'den okunmalı, appsettings.json'a hardcode edilmemeli.
  - **Tek tenant için çalışıyor ≠ multi-tenant'ta çalışacak.** Tasarımda her zaman "bu N tenant ile çalışır mı?" sorusunu sor.
