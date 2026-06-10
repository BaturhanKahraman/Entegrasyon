# Category Wizard + Unsaved Changes Guard Design

**Tarih:** 2026-03-29
**Durum:** Onaylandi
**Oncelik:** High

## Amac

1. Kategori ekleme/duzenleme işlemi modal'dan cikarilip 3 adimli wizard sayfasina tasiniyor
2. Tum form sayfalari icin yeniden kullanilabilir "unsaved changes guard" altyapisi olusturuluyor
3. AllowCustom toggle'i kategori bazindan kaldirilip attribute bazinda global kalacak sekilde duzeltiliyor
4. Marketplace eslestirmesi sonrasi attribute sayfasina deep link ile yonlendirme ekleniyor

---

## Parca 1: Unsaved Changes Guard Component

### Component

**Konum:** `Components/Shared/UnsavedChangesGuard.razor` + `.razor.cs`

**Parametreler:**
- `IsDirty` (bool) — form degisti mi
- `Message` (string) — default: "Kaydedilmemis degisiklikleriniz var. Sayfadan ayrilmak istediginize emin misiniz?"

### Davranis

**Uygulama ici navigasyon:**
- `NavigationLock` component'i ile `OnBeforeInternalNavigation` event'i yakalanir
- `IsDirty == true` ise mevcut `ConfirmDialog` component'i gosterilir (MudBlazor tutarli gorunum)
- Kullanici "Evet" derse navigasyon devam eder, "Hayir" derse iptal

**Tarayici seviyesi:**
- `NavigationLock ConfirmExternalNavigation="IsDirty"` — tab kapatma, URL degistirme, F5 icin native dialog
- Ek olarak JS interop ile `window.beforeunload` event kaydedilir/kaldirilir
- `OnParametersSetAsync` — `IsDirty` degistiginde JS listener gunceller

**Temizlik:**
- `IDisposable` — dispose'da `beforeunload` listener temizlenir

### JS Interop

**Dosya:** `wwwroot/js/unsaved-changes.js`

Minimal JS dosyasi:
- `addBeforeUnloadListener()` — `beforeunload` event ekler
- `removeBeforeUnloadListener()` — event kaldirir

### Kullanim Ornegi

```razor
<UnsavedChangesGuard IsDirty="_isDirty" />

<MudTextField @bind-Value="_model.Name" @oninput="() => _isDirty = true" />
<MudButton OnClick="Save">Kaydet</MudButton>

@code {
    private bool _isDirty;

    private async Task Save()
    {
        await SaveLogic();
        _isDirty = false; // Guard deaktive olur
        NavigationManager.NavigateTo("/list");
    }
}
```

### Scope

Bu asamada sadece component olusturulur. Mevcut form sayfalarina (AddProduct, ProductEdit, NotificationSettings vb.) entegrasyon ayri bir task olarak yapilacak. Kategori wizard'a entegrasyon bu spec kapsaminda.

### Dosyalar

| Dosya | Islem |
|-------|-------|
| `Components/Shared/UnsavedChangesGuard.razor` | Olustur |
| `Components/Shared/UnsavedChangesGuard.razor.cs` | Olustur |
| `wwwroot/js/unsaved-changes.js` | Olustur |

---

## Parca 2: Kategori Wizard Sayfasi

### Route'lar

- `/categories/add` — yeni kategori ekleme
- `/categories/edit/{Id:int}` — mevcut kategori duzenleme (mevcut route korunuyor)

### Component Yapisi

**Ana component:** `Features/Categories/CategoryWizard.razor` + `.razor.cs`

**Adim component'lari:**

| Adim | Component | Icerik |
|------|-----------|--------|
| 1 | `CategoryWizardGeneralStep.razor` | Ad, ust kategori secimi, favori, KDV orani |
| 2 | `CategoryWizardAttributesStep.razor` | Attribute ekleme/cikarma, varianter/slicer/required. AllowCustom read-only chip |
| 3 | `CategoryWizardMarketplaceStep.razor` | Marketplace secimi + kategori eslestirme. Opsiyonel — atlanabilir |

### MudStepper Kullanimi

Mevcut `AddProduct.razor` pattern'i ile ayni:
- `<MudStepper @bind-ActiveIndex="stepIndex">`
- Ekleme modunda: `NonLinear="false"` (sirali navigasyon)
- Duzenleme modunda: `NonLinear="true"` (serbest adim gecisi, pre-filled)

### Adim Gecis Validasyonu

- **Adim 1 -> 2:** Form valid mi? (isim zorunlu, ust kategori leaf kurallarina uygun mu)
- **Adim 2 -> 3:** Varianter/slicer kurallari saglaniliyor mu? (max 1 varianter, max 1 slicer, mutual exclusivity)
- **Adim 3:** Opsiyonel — tamamen atlanabilir

### Duzenleme Modu

- URL'den `Id` parametresi gelirse edit modu aktif
- Tum adimlar mevcut verilerle doldurulur
- `GetCategoryEditPageData(id)` ile yuklenir (mevcut metod)
- Step navigasyonu serbest — kullanici istediyi adima atlayabilir

### Guard Entegrasyonu

```razor
<UnsavedChangesGuard IsDirty="_isDirty" />
```
- Her input degisikliginde `_isDirty = true`
- Kaydet sonrasi `_isDirty = false` ve navigasyon

### Tamamlanma Akisi

Kaydet'e basildiginda:

1. Kategori kaydedilir (AddCategory veya UpdateCategory)
2. Eger 3. adimda marketplace eslestirme yapildiysa:
   - `ConfirmDialog` gosterilir: "Attribute eslestirmesine gecmek ister misiniz?"
   - Evet → `/attributes?categoryId=X&marketplaceId=Y`
   - Hayir → `/categories`
3. Eger 3. adim atlandiysa → `/categories`

### Silinecek / Degisecek Dosyalar

| Dosya | Islem |
|-------|-------|
| `Features/Categories/CategoryDialog.razor` + `.razor.cs` | Silinecek |
| `Features/Categories/CategoryEdit.razor` + `.razor.cs` | Wizard'a donusturulecek |
| `Features/Categories/CategoryWizard.razor` + `.razor.cs` | Olusturulacak (CategoryEdit yerine) |
| `Features/Categories/CategoryWizardGeneralStep.razor` + `.razor.cs` | Olusturulacak |
| `Features/Categories/CategoryWizardAttributesStep.razor` + `.razor.cs` | Olusturulacak |
| `Features/Categories/CategoryWizardMarketplaceStep.razor` + `.razor.cs` | Olusturulacak |
| `Features/Categories/Categories.razor.cs` | "Ekle" butonu NavigateTo("/categories/add") olacak |
| Kategoriler sayfasindaki dialog cagrilari | Kaldirilacak |

---

## Parca 3: AllowCustom Duzeltmesi

### Sorun

`AllowCustom` entity'de global (`CategoryAttribute.AllowCustom`) ama UI'da kategori bazinda `MudSwitch` ile degistirilebiliyor. Bu, ayni attribute'in bir kategoride "serbest deger" digerinde "sadece liste" olmasina yol aciyor.

### Cozum

**Wizard'in 2. adiminda (Ozellikler):**
- `MudSwitch` yorum satirina alinacak
- Ustune aciklama:

```razor
@* AllowCustom: Kategori bazinda degil, attribute bazinda global ayarlanir.
   Gerekirse /attributes sayfasindan yonetilir.
   Ileride kategori bazinda ihtiyac olursa bu blok acilabilir. *@
```

- Attribute'in mevcut `AllowCustom` degeri read-only `MudChip` olarak gosterilecek:
  - `AllowCustom == true` → `<MudChip Color="Color.Info">Serbest deger: Evet</MudChip>`
  - `AllowCustom == false` → `<MudChip Color="Color.Default">Serbest deger: Hayir</MudChip>`

**Kaydetme:**
- DTO'da `AllowCustom` alanıattribute'in mevcut degerinden okunacak
- Kullanici tarafindan degistirilmeyecek

**Mevcut CategoryDialog ve CategoryEdit'teki switch'ler:**
- Ayni sekilde yorum satirina alinacak (wizard'a tasinmadan once)
- Wizard'da da yorum satiri olarak kalacak

### Etkilenen Dosyalar

| Dosya | Degisiklik |
|-------|-----------|
| `CategoryDialog.razor:132` | MudSwitch yorum satiri + aciklama |
| `CategoryEdit.razor:175` | MudSwitch yorum satiri + aciklama |
| Wizard `CategoryWizardAttributesStep.razor` | AllowCustom read-only chip |
| `CategoryEdit.razor.cs` | AllowCustom'i attribute'dan oku, degistirme |
| `CategoryDialog.razor.cs` | Ayni |

---

## Parca 4: Attribute Sync Yonlendirme

### Akis

1. Wizard'in 3. adiminda marketplace eslestirme yapilir
2. `_selectedMarketplaceId` wizard state'inde saklanir
3. Kaydet sonrasi ConfirmDialog gosterilir (Parca 2'de aciklandi)
4. Kullanici "Evet" derse → `/attributes?categoryId=X&marketplaceId=Y`

### Attributes Sayfasi Degisiklikleri

`Features/Attributes/AttributesPage.razor.cs` guncellenir:

- `[SupplyParameterFromQuery] public int? CategoryId { get; set; }`
- `[SupplyParameterFromQuery] public int? MarketplaceId { get; set; }`
- `OnInitializedAsync`'de:
  - `CategoryId` varsa sol panelde ilgili kategori otomatik secilir
  - `MarketplaceId` varsa sag panelde ilgili marketplace tab'i aktif olur
- Halihazirda eslesmis attribute'lar (ornegin "Renk" <-> N11 "Renk") goruntulenir
  - Bu zaten mevcut attribute matching UI'inda var — sadece dogru tab'in acilmasi yeterli

### Etkilenen Dosyalar

| Dosya | Degisiklik |
|-------|-----------|
| `Features/Attributes/AttributesPage.razor.cs` | Query parametreleri + otomatik secim |
| `Features/Attributes/AttributeDetailPanel.razor.cs` | MarketplaceId ile tab secimi |

---

## Test Stratejisi

### Unit testler
- `UnsavedChangesGuard` — `IsDirty` true/false state degisimleri
- Kategori wizard validasyon — adim gecis kurallari
- AllowCustom read-only davranisi — DTO'da degistirilmedigini dogrula

### bUnit testler
- `UnsavedChangesGuard` component rendering — NavigationLock mevcudiyeti
- Wizard adim gecisleri — MudStepper entegrasyonu

### E2E testler
- Kategori ekleme wizard akisi (3 adim)
- Duzenleme modu — pre-filled veriler
- Unsaved changes guard — navigasyon engelleme
- Marketplace eslestirme sonrasi attribute sayfasina yonlendirme

---

## Uygulama Sirasi

1. UnsavedChangesGuard component + JS interop
2. Kategori wizard sayfasi (3 adim component)
3. AllowCustom duzeltmesi (yorum satiri + read-only chip)
4. Attribute sync yonlendirme (query parametreleri)
5. Mevcut CategoryDialog silme + referanslari temizleme
6. Testler
