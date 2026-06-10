# Branch Office Management — Implementation Plan

**Tarih:** 2026-03-30
**Spec:** `docs/superpowers/specs/2026-03-30-branch-office-management-design.md`
**Durum:** Aktif

---

## Genel Bakis

Depo (BranchOffice) yonetimi icin kapsamli CRUD + soft delete + stok transfer + detay sayfasi (5 tab) + marketplace etki yonetimi.

---

## Mimari Kararlar

### Var Olan Yapi
- `BranchOffice` entity: `Id`, `Name`, `IsDefaultMarketPlaceStock`, `Users` (navigation)
- `BranchOfficeManager`: CRUD, `GetPageableBranchOffices`, hard delete kullaniyor (bug)
- `OfficeStockManager`: `DecreaseStockAtomicAsync`, `IncreaseStockAtomicAsync` var — `TransferStockAsync` eksik
- `AppPermissions.BranchOffices`: View/Create/Edit/Delete zaten tanimli
- `POSSession.Status`: `Open=1, Closed=2, Suspended=3` — aktif oturum kontrolu icin

### Yeni Yapilacaklar
- `TransferItemDto` + `StockTransferResultDto`: `Entity/Dtos/Branches/` altina
- `IOfficeStockManager.TransferStockAsync` + implementasyon
- `BranchOfficeManager.Delete` soft delete'e cevrilecek + 3 engelleme kontrolu
- `IBranchOfficeManager`: `GetBranchStocksAsync`, `GetBranchStockMovementsAsync`, `GetBranchPageDetailAsync` metodlari
- 5 Blazor component: BranchOfficesPage, BranchOfficeDialog, BranchOfficeDetail, StockTransferDialog, BranchOfficeDeleteDialog

### DbSet Referanslari (IntegrationDbContext)
- `BranchOffices` — BranchOffice
- `BranchOfficeStocks` — BranchOfficeStock
- `StockMovements` — StockMovement
- `MarketPlaceWarehouses` — MarketPlaceWarehouse
- `POSSessions` — POSSession
- `Users` — ApplicationUser

---

## Task Listesi

### Task 1: Permissions + NavMenu
**Durum:** AppPermissions.BranchOffices zaten tanimli — sadece NavMenu ve PagePermissionAttributeTests guncellenecek.

Degisiklikler:
1. `NavMenu.razor`: "Yonetim" grubuna `PermissionGuardedNavLink` ekle — `AppPermissions.BranchOffices.View`, `/branch-offices`, `Icons.Material.Filled.Warehouse`, "Depolar"
2. `PagePermissionAttributeTests.cs`: `ExpectedPermissions` sozlugune 2 entry ekle:
   - `"Entegrasyon.Blazor.Features.BranchOffices.BranchOfficesPage"` → `AppPermissions.BranchOffices.View`
   - `"Entegrasyon.Blazor.Features.BranchOffices.BranchOfficeDetail"` → `AppPermissions.BranchOffices.View`

---

### Task 2: Soft Delete Fix + Silme Kontrolleri (TDD)

**Test Dosyasi:** `Test/Entegrasyon.Test/Business/BranchOfficeManagerTests.cs`

**Test Senaryolari:**
1. `Delete_ShouldSetIsDeleted_InsteadOfRemove` — IsDeleted=true, hard delete yok
2. `Delete_ShouldFail_WhenLastActiveBranch` — 1 depo varsa silinemiyor
3. `Delete_ShouldFail_WhenActivePOSSessionExists` — Open POS oturumu varsa silinemiyor
4. `Delete_ShouldFail_WhenIsDefaultMarketPlaceStock` — varsayilan marketplace deposu silinemiyor
5. `Delete_ShouldSucceed_WhenAllChecksPass` — normal silme calisiyor

**Implementasyon:**
`BranchOfficeManager.Delete()`:
```
1. id <= 0 → hata
2. Aktif depo Sayısı (IsDeleted=false) <= 1 → "En az 1 aktif depo olmalidir."
3. Open POS oturumu var mi (POSSessions.Any(s => s.BranchOfficeId == id && s.Status == Open)) → engelle
4. IsDefaultMarketPlaceStock == true → "Once baska bir depoyu varsayilan yapin."
5. Soft delete: IsDeleted=true, DeletedAt=UtcNow
```

**Not:** `IBranchOfficeManager` interfaceine yeni metodlar eklenecek.

---

### Task 3: TransferStockAsync (TDD)

**Yeni DTO'lar:**
- `Entity/Dtos/Branches/TransferItemDto.cs`: `ProductVariantId (Guid)`, `Quantity (int)`
- `Entity/Dtos/Branches/StockTransferResultDto.cs`: `TransferredCount (int)`, `SourceMovements (List<StockMovement>)`, `TargetMovements (List<StockMovement>)`

**Interface Guncellemesi:** `IOfficeStockManager.TransferStockAsync`:
```csharp
Task<IDataResult<StockTransferResultDto>> TransferStockAsync(
    int sourceBranchId, int targetBranchId, List<TransferItemDto> items);
```

**Test Senaryolari:**
1. `TransferStock_ShouldSucceed_AndDecreaseSource_IncreaseTarget`
2. `TransferStock_ShouldFail_WhenInsufficientStock`
3. `TransferStock_ShouldFail_WhenSameBranch`
4. `TransferStock_ShouldFail_WhenQuantityIsZeroOrNegative`
5. `TransferStock_ShouldFail_WhenTargetBranchIsDeleted`

**Implementasyon:** `OfficeStockManager.TransferStockAsync`:
```
1. sourceBranchId == targetBranchId → "Kaynak ve hedef depo ayni olamaz."
2. Miktar <= 0 → "Transfer miktari 0'dan buyuk olmalidir."
3. Hedef depo aktif mi kontrol et
4. BeginTransactionAsync()
5. Her item icin DecreaseStockAtomicAsync (StockMovementType.Transfer)
6. başarısızsa RollbackAsync(), ErrorDataResult don
7. Her item icin IncreaseStockAtomicAsync (StockMovementType.Transfer)
8. CommitAsync()
9. StockPriceChangedEvent publish (kaynak + hedef)
10. SuccessDataResult<StockTransferResultDto> don
```

**Not:** TransferStockAsync, mevcut Atomic metodlari kendi DbContext'leriyle cagiriyor, transaction icin IDbContextFactory.CreateDbContext() ve BeginTransactionAsync kullaniyor. Ancak mevcut Atomic metodlar kendi context'lerini olusturuyor — bunlari transaction-aware yapmak icin yeni private metodlar olusturulabilir veya transaction koordinasyonu manager seviyesinde yapilabilir.

---

### Task 4: BranchOfficesPage (Liste)

**Dosyalar:**
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficesPage.razor`
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficesPage.razor.cs`

**Route:** `/branch-offices`
**Auth:** `[Authorize(Policy = AppPermissions.BranchOffices.View)]`
**RenderMode:** `@rendermode InteractiveServer`

**UI:**
- `MudDataGrid<BranchListDetailDto>` (mevcut pageable DTO kullan)
- Kolonlar: Ad, Kullanici Sayısı, Toplam Stok (BranchOfficeStocks.Sum), Olusturma Tarihi, Aksiyonlar
- "Yeni Depo Ekle" butonu → BranchOfficeDialog ac
- Row aksiyonlar: Duzenle (BranchOfficeDialog edit mode), Detay (NavigationManager → /branch-offices/{id}), Sil (BranchOfficeDeleteDialog)

**Yeni IBranchOfficeManager metodu:** `GetPageBranchListAsync()` — stocks ve marketplace bilgisini de getiren DTO:
```csharp
Task<IDataResult<List<BranchOfficePageListDto>>> GetPageBranchListAsync();
```
`BranchOfficePageListDto`: Id, Name, UserCount, TotalStock, MarketPlaceNames, CreatedAt

---

### Task 5: BranchOfficeDialog (Ekle/Duzenle)

**Dosyalar:**
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficeDialog.razor`
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficeDialog.razor.cs`

**Parametreler:** `IsEditMode`, `BranchId (int)`, `BranchName (string)`, `IsDefaultMarketPlaceStock (bool)`

**Form Alanlari:**
- Name: MudTextField, required
- IsDefaultMarketPlaceStock: MudCheckBox

**Actions:**
- Add: `IBranchOfficeManager.AddBranch`
- Edit: `IBranchOfficeManager.Update`

---

### Task 6: BranchOfficeDetail (5-tab Detay Sayfasi)

**Dosyalar:**
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficeDetail.razor`
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficeDetail.razor.cs`

**Route:** `/branch-offices/{Id:int}`
**Auth:** `[Authorize(Policy = AppPermissions.BranchOffices.View)]`

**5 Tab:**

**Tab 1 — Genel Bilgiler:**
- Ad (inline duzenlenebilir MudTextField)
- IsDefaultMarketPlaceStock (MudSwitch)
- Olusturma tarihi
- Kullanici Sayısı
- "Kaydet" butonu → IBranchOfficeManager.Update

**Tab 2 — Stok Durumu:**
- MudDataGrid<BranchStockItemDto>: Urun adi, Varyant, Ilk Stok, Satilan, Mevcut Stok
- Filtre: stoklu/stoksuz toggle
- Multi-select
- "Secilenleri Transfer Et" butonu → StockTransferDialog ac

**Tab 3 — Stok Hareketleri:**
- MudDataGrid<StockMovementDto>: Tarih, Urun, Hareket Tipi (chip), Miktar, Onceki Stok, Sonraki Stok, Referans
- Tarih araligi filtresi (MudDateRangePicker)
- StockMovementType chip renkleri: Sale=blue, Transfer=orange, Return=green, Adjustment=grey, MarketplaceSale=teal

**Tab 4 — Marketplace Baglantilari:**
- MarketPlaceWarehouse kayitlari listesi
- Ekle: MarketPlace dropdown → AddMarketPlaceWarehouseAsync
- Kaldir: onay dialog'u → RemoveMarketPlaceWarehouseAsync

**Tab 5 — Kullanicilar:**
- ApplicationUser listesi (DefaultBranchOfficeId == Id)
- Read-only MudDataGrid

**Yeni IBranchOfficeManager metodlari:**
- `GetBranchStocksAsync(int branchId)` → `IDataResult<List<BranchStockItemDto>>`
- `GetBranchStockMovementsAsync(int branchId, DateTimeOffset? from, DateTimeOffset? to)` → `IDataResult<List<StockMovementViewDto>>`
- `GetBranchMarketPlacesAsync(int branchId)` → `IDataResult<List<MarketPlaceWarehouse>>`
- `AddMarketPlaceWarehouseAsync(int branchId, int marketPlaceId)` → `IResult`
- `RemoveMarketPlaceWarehouseAsync(int warehouseId)` → `IResult`

**Yeni DTO'lar:**
- `BranchStockItemDto`: ProductName, VariantName, FirstTotalStock, SoldQuantity, CurrentStock, ProductVariantId
- `StockMovementViewDto`: MovedAt, ProductName, VariantName, MovementType, Quantity, StockBefore, StockAfter, ReferenceType, ReferenceId

---

### Task 7: StockTransferDialog

**Dosyalar:**
- `Application/Entegrasyon.Blazor/Features/BranchOffices/StockTransferDialog.razor`
- `Application/Entegrasyon.Blazor/Features/BranchOffices/StockTransferDialog.razor.cs`

**Parametreler:** `SourceBranchId (int)`, `SelectedItems (List<BranchStockItemDto>)`

**UI:**
- Hedef depo dropdown: aktif depolar (source haric)
- Secilen urunler listesi: MudTable ile her urun icin transfer miktari input
- "Transfer Et" butonu → IOfficeStockManager.TransferStockAsync
- Basarili: Snackbar "X urun basariyla transfer edildi", dialog kapat

---

### Task 8: BranchOfficeDeleteDialog

**Dosyalar:**
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficeDeleteDialog.razor`
- `Application/Entegrasyon.Blazor/Features/BranchOffices/BranchOfficeDeleteDialog.razor.cs`

**Parametreler:** `BranchId (int)`, `BranchName (string)`

**3-Adim Dialog:**

**Adim 1 — Etki Ozeti:**
- "Bu depoda X urun, Y toplam stok var. Z marketplace baglidir."
- "Devam" butonu

**Adim 2 — Secenek:**
- Radio: "Stoklari [hedef] deposuna transfer et" + hedef depo dropdown
- Radio: "Stoklari sifirla (audit trail ile)"
- "Iptal" butonu

**Adim 3 — Onay:**
- "Bu islem geri alinamaz. Devam etmek istiyor musunuz?"
- "Sil" butonu → stok transfer/sifirla → IBranchOfficeManager.Delete

---

### Task 9: Final Verification

- `dotnet build Entegrasyon.sln`
- `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --verbosity quiet`
- NavMenu linki gorsel dogrulama
- Commit: plan dosyasi

---

## Dosya Degisiklik Ozeti

### Yeni Dosyalar
| Dosya | Task |
|-------|------|
| `Entity/Dtos/Branches/TransferItemDto.cs` | 3 |
| `Entity/Dtos/Branches/StockTransferResultDto.cs` | 3 |
| `Entity/Dtos/Branches/BranchOfficePageListDto.cs` | 4 |
| `Entity/Dtos/Branches/BranchStockItemDto.cs` | 6 |
| `Entity/Dtos/Branches/StockMovementViewDto.cs` | 6 |
| `Blazor/Features/BranchOffices/BranchOfficesPage.razor[.cs]` | 4 |
| `Blazor/Features/BranchOffices/BranchOfficeDialog.razor[.cs]` | 5 |
| `Blazor/Features/BranchOffices/BranchOfficeDetail.razor[.cs]` | 6 |
| `Blazor/Features/BranchOffices/StockTransferDialog.razor[.cs]` | 7 |
| `Blazor/Features/BranchOffices/BranchOfficeDeleteDialog.razor[.cs]` | 8 |
| `Test/Business/BranchOfficeManagerTests.cs` | 2 |

### Degisen Dosyalar
| Dosya | Task | Degisiklik |
|-------|------|-----------|
| `AppPermissions.cs` | 1 | Zaten var — dokunulmayacak |
| `NavMenu.razor` | 1 | "Depolar" link ekleme |
| `PagePermissionAttributeTests.cs` | 1 | 2 entry ekleme |
| `IBranchOfficeManager.cs` | 2,4,6 | Yeni metodlar |
| `BranchOfficeManager.cs` | 2,4,6 | Soft delete + yeni metodlar |
| `IOfficeStockManager.cs` | 3 | TransferStockAsync ekleme |
| `OfficeStockManager.cs` | 3 | TransferStockAsync implementasyon |

---

## Risk ve Dikkat Noktalari

1. **Transaction koordinasyonu:** `TransferStockAsync` icin Atomic metodlari transaction-aware yapmak gerekiyor. Mevcut metodlar kendi context'lerini olusturuyor — yeni private metodlar eklenecek veya direkt context ile cagri yapilacak.
2. **POSSession DbSet:** `POSSessions` DbSet'i DbContext'te mevcut — Delete kontrolunde kullanilabilir.
3. **BranchOffice.IsDeleted:** `BaseEntity`'den geliyor, EF Core soft-delete filter uygulandigini kontrol et.
4. **MarketPlaceWarehouse cascade:** Spec'e gore CASCADE ile siliniyor — EF Core FK konfigurasyonunda kontrol et.
5. **ApplicationUser.DefaultBranchOfficeId:** `Users` tab'i icin bu property var mi kontrol edilmeli.
