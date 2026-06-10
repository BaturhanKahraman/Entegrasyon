# Depo (BranchOffice) Yonetimi — Tam CRUD + Stok Devri + Marketplace Etkisi

**Tarih:** 2026-03-30
**Durum:** Onaylandi
**Oncelik:** High

## Amac

Depo yonetimi icin kapsamli bir sistem: CRUD UI, soft delete + silme kontrolleri, depolar arasi stok transfer, depo detay sayfasi (5 tab), marketplace etki yonetimi.

---

## Parca 1: Depo CRUD + Soft Delete

### Route ve Navigasyon

- Liste: `/branch-offices`
- Detay: `/branch-offices/{Id}`
- NavMenu: "Yonetim" grubuna "Depolar" linki, permission: `AppPermissions.BranchOffices.View` (yeni permission)

### Liste Sayfasi (BranchOfficesPage)

`MudDataGrid` ile:

| Sutun | Kaynak |
|-------|--------|
| Ad | BranchOffice.Name |
| Kullanici Sayısı | Users.Count() |
| Toplam Stok | BranchOfficeStocks.Sum(CurrentStock) |
| Bagli Marketplace'ler | MarketPlaceWarehouse join → marketplace adlari chip |
| Olusturma Tarihi | CreatedAt |
| Aksiyonlar | Duzenle, Detay, Sil |

"Yeni Depo Ekle" butonu → dialog (ad + IsDefaultMarketPlaceStock checkbox).

### Soft Delete Fix

Mevcut `BranchOfficeManager.Delete()` `Remove()` kullaniyor — hard delete. Degisiklik:

```csharp
// Eski:
dbContext.BranchOffices.Remove(new BranchOffice { Id = id });

// Yeni:
var branch = await dbContext.BranchOffices.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
branch.IsDeleted = true;
branch.DeletedAt = DateTimeOffset.UtcNow;
await dbContext.SaveChangesAsync();
```

### Silme Oncesi Kontroller (Sirali)

| # | Kontrol | Hata Mesaji | Aksiyon |
|---|---------|-------------|---------|
| 1 | Son aktif depo mu? | "En az 1 aktif depo olmalidir." | Engelle |
| 2 | Aktif POS oturumu var mi? | "Bu depoda acik POS oturumu var. Lutfen once oturumu kapatin." | Engelle |
| 3 | IsDefaultMarketPlaceStock mi? | "Bu depo varsayilan marketplace deposudur. Once baska bir depoyu varsayilan yapin." | Engelle |
| 4 | Depoda stok var mi? | Stok devri dialog'u tetiklenir | Dialog |
| 5 | Marketplace'e bagli mi? | "Bu depo X marketplace stok hesaplamasinda kullaniliyor. Silme sonrasi stok miktarlari degisecek." | Uyari + onay |

### Stok Devri Dialog'u (Silme Sirasinda)

Adim adim dialog:

**Adim 1 — Etki Ozeti:**
"Bu depoda X urun, Y toplam stok var. Z marketplace baglidir."

**Adim 2 — Secenek:**
- "Stoklari {hedef depo} deposuna transfer et" (dropdown — aktif depolar, mevcut haric)
- "Stoklari sifirla (ManualAdjustment ile audit trail)"
- "Iptal"

**Adim 3 — Onay:**
"Bu islem geri alinamaz. Devam etmek istiyor musunuz?"

Islem sirasi: stok transfer/sifirla → soft delete → marketplace event publish

---

## Parca 2: Depo Detay Sayfasi

### Route: `/branch-offices/{Id}`

### Tab Yapisi

| Tab | Icerik |
|-----|--------|
| **Genel Bilgiler** | Ad (duzenlenebilir inline), IsDefaultMarketPlaceStock toggle, olusturma tarihi, kullanici Sayısı |
| **Stok Durumu** | MudDataGrid — Urun adi, Varyant, Ilk Stok, Satilan, Mevcut Stok. Filtre: stoklu/stoksuz. "Secilenleri Transfer Et" butonu |
| **Stok Hareketleri** | MudDataGrid — Tarih, Urun, Hareket Tipi (chip renkleri: Sale=blue, Transfer=orange, Return=green, Adjustment=grey), Miktar (+/-), Onceki→Sonraki, Referans. Tarih araligi filtresi |
| **Marketplace Baglantilari** | Bu deponun bagli oldugu marketplace'ler. Ekle (dropdown) / kaldir butonlari. MarketPlaceWarehouse tablosundan |
| **Kullanicilar** | DefaultBranchOfficeId = bu depo olan kullanicilar. Read-only liste |

---

## Parca 3: Stok Transfer

### Akis (Stok Durumu Tab'indan)

1. Kullanici urunleri multi-select ile secer
2. "Secilenleri Transfer Et" butonu
3. Dialog acilir:
   - Hedef depo dropdown (mevcut depo haric, aktif depolar)
   - Secilen urunler listesi — her biri icin transfer miktari input (varsayilan: mevcut stok)
   - "Transfer Et" butonu
4. Islem (tek transaction):
   - `BeginTransactionAsync()`
   - Her urun icin: kaynak `DecreaseStockAtomicAsync(StockMovementType.Transfer)`
   - Her urun icin: hedef `IncreaseStockAtomicAsync(StockMovementType.Transfer)`
   - `CommitAsync()` — hata durumunda `RollbackAsync()`
   - `StockPriceChangedEvent` publish (kaynak + hedef depolar icin)
5. Sonuc: Snackbar "X urun basariyla transfer edildi"

### Yeni Service Metodu

`IOfficeStockManager.TransferStockAsync`:

```
TransferStockAsync(int sourceBranchId, int targetBranchId, List<TransferItemDto> items)
```

`TransferItemDto`: ProductVariantId (Guid), Quantity (int)

Transaction wrapper icinde:
1. Kaynak decrease (atomic)
2. Hedef increase (atomic)
3. Her iki taraf icin StockMovement kaydi (ReferenceType = "Transfer", ReferenceId = diger tarafin hareketi)
4. StockPriceChangedEvent publish

### Transfer Is Kurallari

- Transfer miktari <= mevcut stok (DecreaseStockAtomicAsync zaten kontrol eder)
- Kaynak ve hedef depo ayni olamaz
- Hedef depo aktif olmali (IsDeleted = false)
- Miktar > 0 olmali

---

## Parca 4: Marketplace Etkisi

### Transfer Sonrasi

- `StockPriceChangedEvent` publish edilir — her iki depo icin
- Eger kaynak veya hedef depo bir marketplace'e bagliysa (MarketPlaceWarehouse), background service otomatik sync yapar

### Silme Sonrasi

- Stok transfer/sifirla yapildiktan sonra soft delete
- MarketPlaceWarehouse kayitlari CASCADE ile silinir (DB FK davranisi)
- `StockPriceChangedEvent` tetiklenir
- Marketplace'teki stoklar otomatik guncellenir

### Marketplace Tab'inda Baglanti Yonetimi

- Marketplace ekleme: dropdown'dan marketplace sec → MarketPlaceWarehouse kaydi olustur
- Marketplace kaldirma: onay dialog'u → "Bu marketplace'in stok hesaplamasi degisecek" → kaydi sil
- Her iki islemde `StockPriceChangedEvent` publish

---

## Parca 5: Yeni Permission'lar

| Permission | Kullanim |
|-----------|---------|
| `AppPermissions.BranchOffices.View` | Liste + detay sayfasi |
| `AppPermissions.BranchOffices.Edit` | Ekle, duzenle, transfer |
| `AppPermissions.BranchOffices.Delete` | Silme |

---

## Etkilenen Dosyalar

### Yeni Dosyalar

| Dosya | Sorumluluk |
|-------|-----------|
| `Blazor/Features/BranchOffices/BranchOfficesPage.razor[.cs]` | Liste sayfasi |
| `Blazor/Features/BranchOffices/BranchOfficeDialog.razor[.cs]` | Ekle/duzenle dialog |
| `Blazor/Features/BranchOffices/BranchOfficeDeleteDialog.razor[.cs]` | Stok devri + silme dialog |
| `Blazor/Features/BranchOffices/BranchOfficeDetail.razor[.cs]` | Detay sayfasi (5 tab) |
| `Blazor/Features/BranchOffices/StockTransferDialog.razor[.cs]` | Transfer dialog |
| `Entity/Dtos/Branches/TransferItemDto.cs` | Transfer DTO |
| `Entity/Dtos/Branches/StockTransferResultDto.cs` | Transfer sonuc DTO |
| `Test/Business/BranchOfficeManagerTests.cs` | Soft delete + kontrol testleri |
| `Test/Business/StockTransferTests.cs` | Transfer testleri |

### Degisecek Dosyalar

| Dosya | Degisiklik |
|-------|-----------|
| `Business/Concrete/BranchOfficeManager.cs` | Delete → soft delete + kontroller |
| `Business/Abstract/IBranchOfficeManager.cs` | Yeni metodlar (GetBranchDetailForPage vb.) |
| `Business/Concrete/OfficeStockManager.cs` | TransferStockAsync |
| `Business/Abstract/IOfficeStockManager.cs` | TransferStockAsync interface |
| `ApplicationBootstrap/Security/AppPermissions.cs` | BranchOffices permission grubu |
| `Blazor/Components/Shared/NavMenu.razor` | "Depolar" linki |
| `Test/Security/PagePermissionAttributeTests.cs` | Yeni sayfa permission kaydi |

---

## Test Stratejisi

### Unit Testler

- Soft delete: IsDeleted = true, DeletedAt set
- Son depo silinemez kontrolu
- POS oturumu kontrolu
- Default marketplace depo kontrolu
- TransferStockAsync: basarili transfer, yetersiz stok hatasi, ayni depo hatasi
- StockMovement kayitlari dogrulama (source + target)

### Integration Testler

- Transfer full flow: kaynak decrease + hedef increase + movement kayitlari
- Silme full flow: kontroller + stok devri + soft delete
- MarketPlaceWarehouse cascade silme

---

## Uygulama Sirasi

1. Permission'lar + NavMenu linki
2. Soft delete fix + silme kontrolleri (TDD)
3. TransferStockAsync service metodu (TDD)
4. Liste sayfasi (BranchOfficesPage + BranchOfficeDialog)
5. Detay sayfasi (BranchOfficeDetail — 5 tab)
6. Stok transfer dialog (StockTransferDialog)
7. Silme dialog (BranchOfficeDeleteDialog — stok devri)
8. Marketplace tab yonetimi
9. Final verification + testler
