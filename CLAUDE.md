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

# Apply EF Core migrations
dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor

# Add a new migration
dotnet ef migrations add <MigrationName> -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor
```

**Infrastructure (docker-compose):** PostgreSQL (5432)

## Architecture

Klasik katmanlı mimari — Entity → DataAccess → Business → Blazor:

| Proje | Görev |
|---|---|
| `Entegrasyon.Entity` | Domain modelleri ve DTOlar |
| `Entegrasyon.DataAccess` | EF Core context, migrations, entity configurations |
| `Entegrasyon.Business` | Business managers (Abstract + Concrete) |
| `Entegrasyon.ApplicationBootstrap` | DI container kurulumu |
| `Entegrasyon.Blazor` | Blazor Server UI (MudBlazor) |
| `Shared/` | MinIO, Serilog, ImageSharp yardımcı servisleri |

## Key Patterns

**Primary Constructor DI** (C# 12):
```csharp
public class ProductManager(IntegrationDbContext dbContext) : IProductService
```

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

## Test Yapısı

xUnit + Moq + FluentAssertions. Test base class: `BaseTest.cs`. EF Core in-memory DB kullanılır.
