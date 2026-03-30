# Master Catalog System — Implementation Plan

**Tarih:** 2026-03-30
**Spec:** `docs/superpowers/specs/2026-03-30-master-catalog-system-design.md`
**Durum:** Pending

---

## Genel Bakış

AdminPanel DB'de merkezi master catalog (kategoriler, attribute'lar, marketplace eşleştirmeleri). Tenant'lar "İçe Aktar" ile seçtikleri sektör paketini veya kategorileri kendi DB'lerine kopyalar.

---

## Task 1: Entity'ler + AdminPanelDbContext + Migration

**Hedef:** 10 yeni entity, DbContext güncellemesi, migration.

**Dosyalar oluşturulacak:**
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/MasterCategoryEntities.cs`
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/MasterAttributeEntities.cs`
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/MasterMappingEntities.cs`
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/MarketplaceReferenceEntity.cs`
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/SectorPackageEntities.cs`

**Dosyalar güncellenecek:**
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/AdminPanelDbContext.cs` — DbSet'ler + OnModelCreating konfigürasyonları

**Migration:**
```bash
dotnet ef migrations add AddMasterCatalog -p Application/Entegrasyon.AdminPanel --startup-project Application/Entegrasyon.AdminPanel --context AdminPanelDbContext
dotnet ef database update -p Application/Entegrasyon.AdminPanel --startup-project Application/Entegrasyon.AdminPanel --context AdminPanelDbContext
```

**Adımlar:**
1. [ ] MasterCategoryEntities.cs: `MasterCategory` entity
2. [ ] MasterAttributeEntities.cs: `MasterAttribute` + `MasterAttributeValue` + `MasterCategoryAttribute`
3. [ ] MasterMappingEntities.cs: 3 marketplace mapping entity
4. [ ] MarketplaceReferenceEntity.cs: `MarketplaceReference` + `MarketplaceEntityType` enum
5. [ ] SectorPackageEntities.cs: `SectorPackage` + `SectorPackageCategory`
6. [ ] AdminPanelDbContext güncellemesi: 10 DbSet + OnModelCreating (FK'lar, unique index'ler)
7. [ ] Migration oluştur + uygula

**Tamamlanma Kriteri:** `dotnet build` başarılı, migration DB'ye uygulandı.

---

## Task 2: Trendyol Veri Çekme + JSON Snapshot

**Hedef:** Trendyol stage API'den tüm kategori ağacını ve yaprak attribute'larını çekip snapshot olarak kaydet.

**Dosyalar oluşturulacak:**
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/TrendyolSnapshotService.cs`
- `docs/trendyol/categories-snapshot.json` (runtime'da oluşturulur)

**API Endpoints:**
- `GET https://api.trendyol.com/sapigw/product-categories` — full tree
- `GET https://api.trendyol.com/sapigw/product-categories/{categoryId}/attributes` — per leaf

**Strateji:**
1. İlk run: API çağır, sonuçları JSON'a kaydet (her kategori + attributes birlikte)
2. Seed script: snapshot'tan okur, API'yi tekrar çağırmaz
3. Credentials sağlanana kadar: mock sample data ile çalışır

**Snapshot Format:**
```json
{
  "snapshotDate": "2026-03-30",
  "categories": [
    {
      "id": 1,
      "name": "Kategori Adı",
      "parentId": null,
      "subCategories": [...],
      "attributes": [
        {
          "attributeId": 100,
          "attributeName": "Renk",
          "required": true,
          "varianter": true,
          "slicer": false,
          "allowCustom": false,
          "values": [
            { "id": 10, "name": "Kırmızı" }
          ]
        }
      ]
    }
  ]
}
```

**Adımlar:**
1. [ ] `TrendyolSnapshotService.cs` — API çağrısı + JSON serialize + kaydet
2. [ ] Mock snapshot dosyası (3-5 sample kategori, ~20 attribute) — credentials olmadan test için
3. [ ] Snapshot format doğrulama

**Not:** Trendyol API credentials sağlandığında gerçek snapshot çekilir. Şimdilik mock data yeterli.

---

## Task 3: Seed Script — JSON Snapshot → AdminPanelDb

**Hedef:** Snapshot'tan AdminPanelDb'ye tüm master catalog verisini yükle.

**Dosyalar oluşturulacak:**
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/MasterCatalogSeedService.cs`

**Dosyalar güncellenecek:**
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/SeedData.cs` — `MasterCatalogSeedService.SeedAsync()` çağrısı eklenecek

**Algoritma:**
```
1. Snapshot dosyası var mı? → yoksa mock data kullan
2. MarketplaceReference tablosunu temizle (idempotent)
3. MasterCategory ağacını recursive oluştur (BFS/DFS)
4. Her yaprak kategori için:
   a. MasterAttribute oluştur (key bazında deduplicate)
   b. MasterAttributeValue oluştur
   c. MasterCategoryAttribute junction ekle
5. Marketplace mapping'leri ekle (Trendyol MarketplaceId=1)
6. MarketplaceReference kayıtlarını ekle
```

**Adımlar:**
1. [ ] `MasterCatalogSeedService.cs` — async seed methodu
2. [ ] Recursive kategori ağacı build
3. [ ] Attribute deduplication mantığı (aynı ExternalAttributeId → tek MasterAttribute)
4. [ ] SeedData.cs entegrasyonu
5. [ ] Seed sonrası log: "X kategori, Y attribute, Z value yüklendi"

**Tamamlanma Kriteri:** AdminPanelDb'de mock data görebiliyoruz, build başarılı.

---

## Task 4: MasterCatalogSyncService (Cron Job)

**Hedef:** Günlük Trendyol API sync background service.

**Dosyalar oluşturulacak:**
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/MasterCatalogSyncService.cs`

**Dosyalar güncellenecek:**
- `Application/Entegrasyon.AdminPanel/Program.cs` — `AddHostedService<MasterCatalogSyncService>()`

**Algoritma:**
```
BackgroundService (günlük 02:00 UTC)
    → Trendyol API'den kategorileri çek
    → MarketplaceReference ile ExternalId bazında karşılaştır
    → Yeni kayıtlar: MarketplaceReference + MasterCategory ekle
    → Değişmiş kayıtlar: Name güncelle, LastSyncedAt güncelle
    → Kaybolmuş kayıtlar: IsActive = false
    → ILogger ile değişiklik sayıları logla
```

**Adımlar:**
1. [ ] `MasterCatalogSyncService.cs` — `BackgroundService` kalıtımı
2. [ ] Timer logic (günlük çalışma)
3. [ ] Delta comparison logic
4. [ ] Program.cs kaydı

**Not:** Cron job production'da çalışır. Development'ta devre dışı veya manual trigger ile çalıştırılabilir.

---

## Task 5: IMasterCatalogImportService (Tenant Import)

**Hedef:** AdminPanelDb → Tenant IntegrationDb kopyalama servisi.

**Dosyalar oluşturulacak:**
- `Application/Entegrasyon.Business/Abstract/IMasterCatalogImportService.cs`
- `Application/Entegrasyon.Business/Concrete/MasterCatalogImportService.cs`
- `Application/Entegrasyon.Entity/Dtos/MasterCatalog/ImportResultDto.cs`

**Dosyalar güncellenecek:**
- `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` — DI kaydı

**Interface:**
```csharp
public interface IMasterCatalogImportService
{
    Task<ImportResultDto> ImportFromMasterAsync(
        int tenantId,
        IList<int> masterCategoryIds,
        CancellationToken ct = default);

    Task<IList<MasterCategoryTreeDto>> GetMasterCategoryTreeAsync(CancellationToken ct = default);
    Task<IList<SectorPackageDto>> GetSectorPackagesAsync(CancellationToken ct = default);
    Task<IList<int>> GetSectorPackageCategoryIdsAsync(int sectorPackageId, CancellationToken ct = default);
}
```

**Import Algoritması:**
```
1. AdminPanelDb'den seçili kategorileri çek (recursive — üst kategoriler dahil)
2. Her kategori için IntegrationDb'de ExternalCategoryId kontrolü (duplicate skip)
3. Category → kategori ağacını yeniden oluştur (SuperCategoryId linkler)
4. MasterAttribute → CategoryAttribute (ImportId = MasterAttribute.Id bazında deduplicate)
5. MasterAttributeValue → CategoryAttributeValue
6. MasterCategoryAttribute → CategoryAttributeCategory
7. MasterCategoryMarketplaceMapping → CategoryMarketPlaceMatch
8. MasterAttributeMarketplaceMapping → CategoryAttributeMarketPlaceMatch
9. MasterValueMarketplaceMapping → CategoryAttributeValueMarketPlaceMatch
10. SaveChangesAsync() — tek transaction
11. ImportResultDto dön
```

**AdminPanelDbContext Erişimi:**
```csharp
public class MasterCatalogImportService(
    IDbContextFactory<AdminPanelDbContext> adminDbFactory,
    IDbContextFactory<IntegrationDbContext> integrationDbFactory,
    ILogger<MasterCatalogImportService> logger) : IMasterCatalogImportService
```

**Adımlar:**
1. [ ] `ImportResultDto.cs` entity
2. [ ] `MasterCategoryTreeDto.cs` + `SectorPackageDto.cs`
3. [ ] `IMasterCatalogImportService.cs` interface
4. [ ] `MasterCatalogImportService.cs` — import logic
5. [ ] Unit test: `Test/Entegrasyon.Test/Business/MasterCatalogImportServiceTests.cs`
6. [ ] DI kaydı

**Tamamlanma Kriteri:** Unit testler yeşil, build başarılı.

---

## Task 6: Sektör Paketleri Seed

**Hedef:** 5 sektör paketi ve ilgili master kategori bağlantıları.

**Dosyalar güncellenecek:**
- `Application/Entegrasyon.AdminPanel/Infrastructure/Data/MasterCatalog/MasterCatalogSeedService.cs` — `SeedSectorPackages()` metodu

**Paketler:**
```
1. Giyim & Moda — icon: Icons.Material.Filled.Checkroom
2. Elektronik — icon: Icons.Material.Filled.Devices
3. Kozmetik & Bakım — icon: Icons.Material.Filled.Spa
4. Ev & Yaşam — icon: Icons.Material.Filled.Home
5. Spor & Outdoor — icon: Icons.Material.Filled.SportsSoccer
```

**Adımlar:**
1. [ ] `SeedSectorPackages()` metodu — mock data veya snapshot'a dayalı
2. [ ] Seed sonrası doğrulama

---

## Task 7: Tenant Import UI (Blazor)

**Hedef:** Kategoriler sayfasına "İçe Aktar" butonu + dialog.

### 7.1 MasterCatalog Import Dialog

**Dosyalar oluşturulacak:**
- `Application/Entegrasyon.Blazor/Features/Categories/MasterCatalogImportDialog.razor`
- `Application/Entegrasyon.Blazor/Features/Categories/MasterCatalogImportDialog.razor.cs`

**UI Yapısı:**
```
MudDialog: "Master Catalog'dan İçe Aktar"
├── MudTabs
│   ├── Tab: "Sektör Paketleri"
│   │   └── MudGrid — her paket bir kart (ikon + isim + açıklama + checkbox)
│   └── Tab: "Özel Seçim"
│       └── MudTreeView — MasterCategory ağacı (checkbox'lı)
├── [İptal] [İçe Aktar] butonları
```

### 7.2 Import Result Dialog

**Dosyalar oluşturulacak:**
- `Application/Entegrasyon.Blazor/Features/Categories/ImportResultDialog.razor`
- `Application/Entegrasyon.Blazor/Features/Categories/ImportResultDialog.razor.cs`

**UI:**
```
MudDialog: "İçe Aktarma Tamamlandı"
├── Kategoriler: X eklendi, Y atlandı
├── Özellikler: X eklendi, Y atlandı
├── Değerler: X eklendi, Y atlandı
├── Marketplace Eşleştirmeleri: X eklendi
└── [Kapat]
```

### 7.3 Categories Sayfasına "İçe Aktar" Butonu

**Dosyalar güncellenecek:**
- `Application/Entegrasyon.Blazor/Features/Categories/Categories.razor` — toolbar'a buton
- `Application/Entegrasyon.Blazor/Features/Categories/Categories.razor.cs` — dialog açma logic

**Adımlar:**
1. [ ] `MasterCatalogImportDialog.razor` + `.cs`
2. [ ] `ImportResultDialog.razor` + `.cs`
3. [ ] Categories sayfasına "İçe Aktar" butonu
4. [ ] Dialog akışı: import → result göster → kategorileri yenile

---

## Task 8: Build + Test + Commit

**Adımlar:**
1. [ ] `dotnet build Entegrasyon.sln` — temiz build
2. [ ] `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj` — tüm unit testler yeşil
3. [ ] AdminPanel migration applied — `dotnet ef migrations has-pending-model-changes ...`
4. [ ] Commit: `feat(master-catalog): add master catalog system with tenant import`

---

## Bağımlılık Grafiği

```
Task 1 (Entity + Migration)
    ↓
Task 3 (Seed Script) ← Task 2 (Snapshot)
    ↓
Task 5 (Import Service) ← Task 6 (Sector Packages)
    ↓
Task 7 (Blazor UI)
    ↓
Task 8 (Build + Test)

Task 4 (Cron Job) — Task 1'e bağlı, diğerlerinden bağımsız
```

---

## Notlar

- **Scope:** Bu fazda sadece Trendyol (MarketplaceId=1) desteklenir.
- **AdminPanelDb:** PostgreSQL (üretim: 192.168.1.78). Migration AdminPanel projesine eklenir.
- **Çift DbContext:** `MasterCatalogImportService` hem `AdminPanelDbContext` hem `IntegrationDbContext` kullanır. `IDbContextFactory<T>` ile inject edilir.
- **Idempotency:** Seed ve import işlemleri tekrar çalıştırılabilir; duplicate oluşmaz.
- **TDD:** Task 5 için önce test yazılır, sonra implement edilir.
