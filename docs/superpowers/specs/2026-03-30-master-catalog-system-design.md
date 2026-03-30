# Master Catalog System — Tasarım Dokümanı

**Tarih:** 2026-03-30
**Durum:** Draft

---

## 1. Genel Bakış

### Hedef

Merkezi bir master catalog sistemi: AdminPanel DB'de kategoriler, attribute'lar, value'lar ve marketplace eşleştirmeleri tutulur. Tenant'lar "İçe Aktar" ile master'dan kendi DB'lerine seçili kategorileri ve ilgili tüm veriyi kopyalar.

### Motivasyon

- Trendyol kategori/attribute verisini her tenant'ın ayrı ayrı API'den çekmesi yerine, tek bir merkezi kaynaktan sağlanır.
- Marketplace güncellemeleri (yeni kategori, attribute değişikliği) tek noktada yönetilir.
- Sektör paketleri ile onboarding hızlanır: tenant ilk kurulumda sektörünü seçer, tüm kategori ağacı hazır gelir.
- İleride N11, Hepsiburada, Pazarama gibi marketplacelar da aynı altyapıya eklenir.

---

## 2. Entity Modeli (AdminPanelDb)

Tüm master catalog entity'leri `AdminPanelDbContext` içinde yaşar. AdminPanel'in `BaseEntity`'si (`Id`, `CreatedAt`, `UpdatedAt`) kullanılır; `IsDeleted`/`DeletedAt` soft-delete gerektiğinde eklenir.

### 2.1 MasterCategory

```
MasterCategory
├── Id (int, PK)
├── Name (string)
├── ParentId (int?, FK → MasterCategory.Id, self-ref)
├── SortOrder (int, default 0)
├── IsActive (bool, default true)
├── IsLeaf (bool) — yaprak düğüm = ürün atanabilir
├── OriginalMarketplaceId (int?) — hangi marketplace'den geldi
├── OriginalExternalId (string?) — marketplace'deki ID
├── CreatedAt, UpdatedAt
```

**İlişkiler:**
- `Parent` → `MasterCategory` (self-ref)
- `Children` → `ICollection<MasterCategory>`
- `CategoryAttributes` → `ICollection<MasterCategoryAttribute>`
- `MarketplaceMappings` → `ICollection<MasterCategoryMarketplaceMapping>`
- `SectorPackageCategories` → `ICollection<SectorPackageCategory>`

### 2.2 MasterAttribute

```
MasterAttribute
├── Id (int, PK)
├── Key (string) — makine okuyabilir key (örn: "Renk")
├── HumanizedName (string) — kullanıcıya gösterilen isim
├── AllowCustom (bool) — özel değer girişine izin verir mi
├── IsActive (bool, default true)
├── CreatedAt, UpdatedAt
```

**İlişkiler:**
- `Values` → `ICollection<MasterAttributeValue>`
- `CategoryLinks` → `ICollection<MasterCategoryAttribute>`
- `MarketplaceMappings` → `ICollection<MasterAttributeMarketplaceMapping>`

### 2.3 MasterAttributeValue

```
MasterAttributeValue
├── Id (int, PK)
├── MasterAttributeId (int, FK → MasterAttribute.Id)
├── Name (string)
├── IsActive (bool, default true)
├── CreatedAt, UpdatedAt
```

**İlişkiler:**
- `MasterAttribute` → `MasterAttribute`
- `MarketplaceMappings` → `ICollection<MasterValueMarketplaceMapping>`

### 2.4 MasterCategoryAttribute (junction)

```
MasterCategoryAttribute
├── Id (int, PK)
├── MasterCategoryId (int, FK → MasterCategory.Id)
├── MasterAttributeId (int, FK → MasterAttribute.Id)
├── IsRequired (bool)
├── IsVarianter (bool)
├── IsSlicer (bool)
```

**Unique constraint:** (MasterCategoryId, MasterAttributeId)

### 2.5 MasterCategoryMarketplaceMapping

```
MasterCategoryMarketplaceMapping
├── Id (int, PK)
├── MasterCategoryId (int, FK → MasterCategory.Id)
├── MarketplaceId (int) — 1=Trendyol, 2=N11, 3=Hepsiburada ...
├── ExternalCategoryId (string)
├── ExternalCategoryName (string)
```

**Unique constraint:** (MasterCategoryId, MarketplaceId)

### 2.6 MasterAttributeMarketplaceMapping

```
MasterAttributeMarketplaceMapping
├── Id (int, PK)
├── MasterAttributeId (int, FK → MasterAttribute.Id)
├── MarketplaceId (int)
├── ExternalAttributeId (string)
├── ExternalAttributeName (string)
```

**Unique constraint:** (MasterAttributeId, MarketplaceId)

### 2.7 MasterValueMarketplaceMapping

```
MasterValueMarketplaceMapping
├── Id (int, PK)
├── MasterAttributeValueId (int, FK → MasterAttributeValue.Id)
├── MarketplaceId (int)
├── ExternalValueId (string)
├── ExternalValueName (string)
```

**Unique constraint:** (MasterAttributeValueId, MarketplaceId)

### 2.8 MarketplaceReference

Ham marketplace verisi — senkronizasyon ve karşılaştırma için:

```
MarketplaceReference
├── Id (int, PK)
├── MarketplaceId (int)
├── EntityType (enum: Category, Attribute, Value)
├── ExternalId (string) — marketplace'deki ID
├── Name (string)
├── ParentExternalId (string?) — üst düğüm ID'si
├── RawJson (string?) — API'den gelen ham JSON
├── LastSyncedAt (DateTimeOffset)
├── IsActive (bool)
├── CreatedAt, UpdatedAt
```

**Unique constraint:** (MarketplaceId, EntityType, ExternalId)

**Kullanım:** Trendyol'dan çekilen ham veri buraya yazılır. Master tablolarla karşılaştırılarak delta bulunur.

### 2.9 SectorPackage

```
SectorPackage
├── Id (int, PK)
├── Name (string) — "Giyim", "Elektronik" vb.
├── Description (string?)
├── IconName (string?) — MudBlazor icon adı
├── IsActive (bool, default true)
├── CreatedAt, UpdatedAt
```

**İlişkiler:**
- `Categories` → `ICollection<SectorPackageCategory>`

### 2.10 SectorPackageCategory (junction)

```
SectorPackageCategory
├── Id (int, PK)
├── SectorPackageId (int, FK → SectorPackage.Id)
├── MasterCategoryId (int, FK → MasterCategory.Id)
```

---

## 3. Veri Akışları

### 3.1 İlk Yükleme (Development)

```
Trendyol API
    → GET /product-categories (tüm ağaç)
    → GET /product-categories/{id}/attributes (yaprak kategoriler için)
    → docs/trendyol/categories-snapshot.json (JSON snapshot kaydedilir)

Seed Script (CLI veya startup)
    → MarketplaceReference tablosunu doldur
    → MasterCategory ağacını oluştur
    → MasterAttribute + MasterAttributeValue
    → MasterCategoryAttribute (junction)
    → MasterCategoryMarketplaceMapping
    → MasterAttributeMarketplaceMapping
    → MasterValueMarketplaceMapping
    → SectorPackage + SectorPackageCategory seed
```

### 3.2 Günlük Güncelleme (Production Cron)

```
MasterCatalogSyncService (Background Service)
    → Her gün 02:00 UTC
    → Trendyol API'den tüm kategorileri çek
    → MarketplaceReference ile karşılaştır
    → Yeni: MarketplaceReference + MasterCategory ekle
    → Değişmiş: güncelle
    → Kaybolmuş: IsActive = false
    → Değişiklik logu yaz
```

### 3.3 Tenant Import

```
Tenant "İçe Aktar" isteği
    ├── Sektör paketi seçimi → ilgili MasterCategory ID listesi
    │   veya
    ├── Manuel seçim → checkbox tree (MasterCategory ağacı)

IMasterCatalogImportService.ImportFromMasterAsync(tenantId, masterCategoryIds)
    → AdminPanelDb'den seçili kategorileri ve ilgili veriyi oku
    → Tenant IntegrationDb'ye yaz:
        MasterCategory → Category
        MasterAttribute → CategoryAttribute
        MasterAttributeValue → CategoryAttributeValue
        MasterCategoryAttribute → CategoryAttributeCategory
        MasterCategoryMarketplaceMapping → CategoryMarketPlaceMatch
        MasterAttributeMarketplaceMapping → CategoryAttributeMarketPlaceMatch
        MasterValueMarketplaceMapping → CategoryAttributeValueMarketPlaceMatch
    → Duplicate detection: ExternalCategoryId üzerinden
    → ImportResultDto dön
```

---

## 4. Tenant Import Giriş Noktaları

| Giriş Noktası | Tetikleyici | Hedef Kullanıcı |
|---|---|---|
| Onboarding Dialog | İlk giriş, kategori yoksa | Yeni tenant |
| Kategoriler sayfası "İçe Aktar" butonu | Manuel | Tüm tenant'lar |
| Admin Panel toggle | Admin | Tüm tenant'lar (admin kontrolü) |

---

## 5. Import Sonuç Dialog

```
MudDialog: "İçe Aktarma Tamamlandı"
├── Kategoriler: 45 eklendi, 3 atlandı (zaten mevcut)
├── Özellikler: 120 eklendi, 8 atlandı
├── Değerler: 890 eklendi, 45 atlandı
├── Marketplace Eşleştirmeleri: 165 eklendi
└── [Kapat] butonu
```

**ImportResultDto:**
```csharp
public record ImportResultDto(
    int CategoriesImported,
    int AttributesImported,
    int ValuesImported,
    int MappingsImported,
    int CategoriesSkipped,
    int AttributesSkipped,
    int ValuesSkipped
);
```

---

## 6. Sektör Paketleri (İlk Seed)

| Paket | Açıklama | Kapsam |
|---|---|---|
| Giyim & Moda | Kadın, erkek, çocuk giyim | ~20 yaprak kategori |
| Elektronik | Telefon, bilgisayar, beyaz eşya | ~15 yaprak kategori |
| Kozmetik & Bakım | Cilt bakımı, makyaj, parfüm | ~12 yaprak kategori |
| Ev & Yaşam | Mobilya, dekor, mutfak | ~18 yaprak kategori |
| Spor & Outdoor | Spor malzemeleri, kamp | ~10 yaprak kategori |

---

## 7. Teknik Kısıtlamalar & Kararlar

### 7.1 AdminPanelDb SQLite mi PostgreSQL mı?

AdminPanel `appsettings.json`'a bakıldığında hem SQLite hem PostgreSQL destekleniyor. Üretim ortamı PostgreSQL kullanıyor (192.168.1.78). Master catalog tabloları AdminPanel'in PostgreSQL DB'sine eklenir.

### 7.2 Tenant Import — Çift DbContext Erişimi

`IMasterCatalogImportService` hem AdminPanelDb hem IntegrationDb'ye erişmek zorunda. Çözüm:
- `IDbContextFactory<AdminPanelDbContext>` → AdminPanelDb okuma
- `IDbContextFactory<IntegrationDbContext>` → Tenant DB yazma
- Her ikisi de Business katmanında inject edilir.

### 7.3 Duplicate Detection

Tenant'ın DB'sindeki mevcut kategoriler `ExternalCategoryId` üzerinden kontrol edilir. Aynı ExternalCategoryId varsa, kategori atlanır. Bu şekilde import idempotent olur.

### 7.4 Transaction Stratejisi

Tenant import tek bir transaction içinde yapılır. Herhangi bir hata durumunda tüm import geri alınır. Kısmi import durumu oluşmaz.

### 7.5 Multi-Tenant Uyumluluk

Import servisi `tenantId` parametresi alır ancak mevcut yapıda tek bir `IntegrationDbContext` var. İleride multi-tenant geçişte her tenant'ın kendi `IntegrationDb` bağlantısı olacak ve `tenantId` → connection string çözümlemesi eklenecek. Şimdilik `tenantId` parametre olarak hazır tutulur ama tek DB üzerinde çalışır.

---

## 8. Scope Dışı (Bu Fazda)

- N11, Hepsiburada vb. marketplace sync (sadece Trendyol)
- Attribute değeri önerileri (AI destekli)
- Master catalog'dan tenant güncelleme (sadece ilk import, update değil)
- Admin Panel UI — master catalog yönetim ekranları
