# User Management & Permission Restrictions - Implementasyon Plani

## Mevcut Durum Analizi

### Auth/Permission Altyapisi (MEVCUT VE CALISIYOR)

| Dosya | Yol | Durum |
|---|---|---|
| AppPermissions | `Application/Entegrasyon.ApplicationBootstrap/Security/AppPermissions.cs` | 17 modül, 62 permission tanimli |
| PermissionRequirement | `Application/Entegrasyon.ApplicationBootstrap/Security/TenantFeatureAuthorizationHandler.cs` | IAuthorizationRequirement implementasyonu |
| TenantFeatureAuthorizationHandler | `Application/Entegrasyon.ApplicationBootstrap/Security/TenantFeatureAuthorizationHandler.cs` | 2 katmanli: tenant feature + user permission |
| FeatureService | `Application/Entegrasyon.Business/Tenants/FeatureService.cs` | Tenant-level feature kontrolu |
| FeatureGate component | `Application/Entegrasyon.Blazor/Components/Shared/FeatureGate.razor(.cs)` | UI'da feature gating |
| CustomAuthenticationStateProvider | `Application/Entegrasyon.Blazor/Services/CustomAuthenticationStateProvider.cs` | Claims-based auth, ProtectedLocalStorage |
| Program.cs auth config | `Application/Entegrasyon.Blazor/Program.cs` (satir 59-81) | Cookie auth + policy registration |

### User Management Sayfalari (MEVCUT VE CALISIYOR)

| Sayfa | Yol | Route | Authorize |
|---|---|---|---|
| Users listesi | `Features/Users/Users.razor(.cs)` | `/users` | `AppPermissions.Users.View` |
| User edit | `Features/Users/UserEdit.razor(.cs)` | `/users/edit/{Id:guid}` | `AppPermissions.Users.View` |
| User dialog (add) | `Features/Users/UserDialog.razor(.cs)` | Dialog | - |
| Role management | `Features/Admin/RoleManagement.razor(.cs)` | `/roles` | `AppPermissions.Roles.View` |
| Role dialog | `Features/Admin/RoleDialog.razor(.cs)` | Dialog | - |

### Business Layer

| Servis | Interface | Concrete |
|---|---|---|
| User management | `Business/Abstract/IApplicationUserManager.cs` | `Business/Concrete/Auth/ApplicationUserManager.cs` |
| Role management | `Business/Concrete/Auth/IRoleService.cs` | `Business/Concrete/Auth/RoleService.cs` |
| Auth service | `Business/Abstract/IAuthService.cs` | `Business/Concrete/Auth/AuthService.cs` |

### NavMenu

Yol: `Application/Entegrasyon.Blazor/Components/Shared/NavMenu.razor(.cs)`

Mevcut permission kontrolleri:
- Urunler: `AuthorizeView Policy=AppPermissions.Products.View`
- Musteriler: `AuthorizeView Policy=AppPermissions.Customers.View`
- Kullanici Yonetimi grubu: `AuthorizeView Policy=AppPermissions.Users.View`
  - Kullanicilar: `AuthorizeView Policy=AppPermissions.Users.View`
  - Roller: `AuthorizeView Policy=AppPermissions.Roles.View`

Permission kontrolu OLMAYAN menu ogerleri (toplam ~35 oge):
- Kategoriler, Ozellikler, Markalar, POS Terminal, Satis Gecmisi, Satislar ve Siparişler
- Kargo Takip, Toplu Islem
- Tum Pazaryeri alt menuleri
- Faturalar, E-Fatura
- Tum Raporlar
- Tum Magaza alt menuleri
- Tum Ayarlar alt menuleri

### Entity Modeli

- `ApplicationUser`: `Application/Entegrasyon.Entity/User/ApplicationUser.cs`
- `Role`: `Application/Entegrasyon.Entity/User/Role.cs` (Id, Name, RoleClaims, UsersRoles)
- `RolesClaims`: `Application/Entegrasyon.Entity/User/RolesClaims.cs` (RoleId, Permission string)
- `UsersClaims`: `Application/Entegrasyon.Entity/User/UsersClaims.cs` (ApplicationUserId, Permission string)
- `PermissionModel`: `Application/Entegrasyon.Blazor/Models/PermissionModel.cs` (UI display model)

### Mevcut Testler

- `Test/Entegrasyon.Test/Business/RoleServiceTests.cs` - AddRole, EditRole validasyon ve business rule testleri
- `Test/Entegrasyon.Test/Business/ValidationRules/AddRoleDtoValidatorTests.cs`
- `Test/Entegrasyon.Test/Business/ValidationRules/EditRoleDtoValidatorTests.cs`
- `Test/Entegrasyon.Test/Tenants/TenantFeatureAuthorizationHandlerTests.cs`
- `Test/Entegrasyon.Test/Tenants/FeatureServiceTests.cs`

---

## KRITIK BUG: Permission'lar Login Sirasinda Yuklenmiyor

**Dosya:** `Application/Entegrasyon.Business/Concrete/Auth/AuthService.cs`, satir 22-23

```csharp
var user = await context.Users
    .Include(u => u.Roles)  // RoleClaims INCLUDE EDILMIYOR!
    .FirstOrDefaultAsync(...);
```

**Etki:** `Login.razor.cs` satir 40-52'de `role.RoleClaims` her zaman null/empty donuyor. Bu yuzden `UserSession.Permissions` bos liste oluyor. Sonuc: `TenantFeatureAuthorizationHandler` satir 28'deki `context.User.HasClaim("Permission", ...)` her zaman false donuyor.

**Neden "Admin" rolu ile calisiyormus gibi gorunuyor:** `TenantFeatureAuthorizationHandler` satir 28'de `context.User.IsInRole("Admin")` kontrolu var. Admin rolu olan kullanici icin bu `true` donuyor ve permission check bypass ediliyor. Ama Admin olmayan roller icin hicbir permission-based sayfa gorunmuyor.

**Duzeltme:**
```csharp
var user = await context.Users
    .Include(u => u.Roles)
        .ThenInclude(r => r.RoleClaims)
    .FirstOrDefaultAsync(...);
```

---

## Task 1: User Management Settings UX

### Sorun Analizi

Kullanici yonetimi sayfasi **mevcuttur** ve calisir durumdadir:
- `/users` - Kullanici listesi
- `/users/edit/{Id:guid}` - Kullanici duzenleme (detayli sayfa)
- `/roles` - Rol yonetimi

NavMenu'de "Kullanici Yonetimi" grubu `AuthorizeView Policy=AppPermissions.Users.View` ile korunuyor. **Kritik bug nedeniyle** Admin olmayan kullanicilar icin bu grup gorunmuyor (cunku permission'lar session'a yuklenmiyor).

### Adimlar

#### Adim 1.1: Login Permission Bug Fix (ONCELIKLI)
- **Dosya:** `Application/Entegrasyon.Business/Concrete/Auth/AuthService.cs`
- **Degisiklik:** `.Include(u => u.Roles)` -> `.Include(u => u.Roles).ThenInclude(r => r.RoleClaims)`
- **Test:** Unit test yazilacak (LoginAsync ile permission'larin UserLoginSuccessDto icerisinde dondugunun dogrulanmasi)

#### Adim 1.2: RoleService.AddRole - Permission Kaydi Eksik
- **Dosya:** `Application/Entegrasyon.Business/Concrete/Auth/RoleService.cs`, `AddRole` metodu (satir 23-44)
- **Sorun:** Yeni rol eklenirken `dto.PermissionNames` RoleClaims olarak kaydedilmiyor. Sadece Role entity'si ekleniyor, permission'lar ignored.
- **Duzeltme:** `AddRole` icinde `dto.PermissionNames` uzerinden `RolesClaims` olusturup role'e eklemek gerekiyor.
- **Ayni sorun `UpdateRole` icin de gecerli:** Mevcut RoleClaims guncellenmesi yapilmiyor.

#### Adim 1.3: User Management UX Iyilestirmeleri
- **Mevcut durum:** Temel CRUD islemleri var (listeleme, ekleme, duzenleme, silme, sifre sifirlama)
- **Eksikler:**
  - Kullanici listesinde rol bilgisi gorunmuyor
  - Filtreleme sadece client-side (QuickFilter yok, MudDataGrid'de FilterFunc bagli degil)
  - UserDialog'da coklu rol secimi var ama UserEdit'te tekli Select kullaniliyor (tutarsizlik)
  - Kullanici durumu (aktif/pasif) toggle'i listeden yapilemiyor
  - Son giris tarihi bilgisi yok

#### Adim 1.4: Test Yazimi
- AuthService.LoginAsync permission yukleme testi
- RoleService.AddRole permission kaydi testi
- RoleService.UpdateRole permission guncelleme testi
- Blazor bUnit: NavMenu permission-based gorunum testi

---

## Task 2: Permission Restrictions

### Mevcut Durum

**Sayfa bazli `[Authorize(Policy=...)]` olan sayfalar (7/60):**
1. `/users` - Users.View
2. `/users/edit/{Id:guid}` - Users.View
3. `/roles` - Roles.View
4. `/customers` - Customers.View
5. `/products` - Products.View
6. `/settings/*` (4 sayfa) - Settings.View / Integrations.View
7. `/categories/edit/{Id:int}` - Categories.View

**Sayfa bazli `[Authorize(Policy=...)]` OLMAYAN sayfalar (~53 sayfa):**
Tum diger sayfalar sadece genel `[Authorize]` attribute'u ile korunuyor (_Imports.razor'dan). Bu sayfalar giris yapmis herkes tarafindan erisilebilir.

**NavMenu'de permission kontrolu olan ogeler (3/~35):**
- Urunler, Musteriler, Kullanici Yonetimi grubu

### Implementasyon Stratejisi

#### Faz 2.1: Tum Sayfalara Permission Policy Ekleme

Her sayfa icin uygun `AppPermissions` policy'si eklenecek:

| Sayfa Grubu | Policy | Sayfalar |
|---|---|---|
| Kategoriler | Categories.View | `/categories`, `/categories/edit/*`, `/categories/import` |
| Markalar | Brands.View | `/brands` |
| Ozellikler | Categories.View | `/attributes` (kategori ozelliklerini yonetiyor) |
| POS | Sales.Create | `/pos` |
| Satis Gecmisi | Sales.View | `/sales` |
| Siparişler | Orders.View | `/orders`, `/marketplace/orders`, `/marketplace/orders/*` |
| Kargo | Cargo.View | `/shipping` |
| Toplu Islem | Products.Edit | `/bulk-operations` |
| Faturalar | Orders.View | `/invoices`, `/invoicing` |
| Raporlar | Reports.View | `/reports/*` (6 sayfa) |
| Pazaryeri | Marketplace.View | `/marketplace/sync/*`, `/marketplace/matching/*`, `/marketplace/commission-rates`, `/matched-entities` |
| Magaza | Settings.View | `/settings/storefront/*`, `/storefront/*` (tum storefront sayfalari) |
| Ayarlar | Settings.View | `/settings/general`, `/settings/notifications`, `/settings/printing`, `/settings/desktop` |
| Bildirimler | Notifications.View | `/notifications` |
| Dashboard | (herkese acik) | `/` |
| Profil | (herkese acik) | `/profile` |

#### Faz 2.2: NavMenu Tam Permission Korumasi

Tum NavMenu ogelerine `AuthorizeView` eklenecek. Uc farkli gosterim modu:

1. **Gizli (Hidden):** Permission yoksa menu ogesi gorunmez
   ```razor
   <AuthorizeView Policy="@AppPermissions.Products.View">
       <Authorized>
           <MudNavLink ... />
       </Authorized>
   </AuthorizeView>
   ```

2. **Pasif (Disabled) + Pop-up:** Modul satin alinmamissa (tenant feature) menu ogesi gorunur ama tiklanamiyor, tiklaninca bilgilendirme pop-up'i acar
   ```razor
   <FeatureGate Permission="@AppPermissions.Products.View">
       <ChildContent>
           <AuthorizeView Policy="@AppPermissions.Products.View">
               <Authorized>
                   <MudNavLink ... />
               </Authorized>
           </AuthorizeView>
       </ChildContent>
       <FallbackContent>
           <MudNavLink ... Disabled="true" OnClick="@(() => ShowUpgradeDialog("Urunler"))" />
       </FallbackContent>
   </FeatureGate>
   ```

3. **Herkese acik:** Dashboard, Profil gibi temel sayfalar

#### Faz 2.3: UpgradePromptDialog Componenti

Yeni component: `Components/Shared/UpgradePromptDialog.razor(.cs)`

Pasif menu ogesine tiklandiginda acilacak dialog:
- Baslik: "Bu Ozellik Paketinize Dahil Degil"
- Aciklama: "Bu ozelligi kullanmak icin paketinizi yukseltmeniz gerekiyor."
- Buton: "Paket Bilgisi" (gelecekte admin paneline yonlendirecek)

#### Faz 2.4: PermissionGuardedNavLink Componenti

Tekrar eden AuthorizeView + FeatureGate pattern'ini sarmallayan yeniden kullanilabilir component:

```
Components/Shared/PermissionGuardedNavLink.razor(.cs)
```

Parametreler:
- `string Permission` - AppPermissions sabiti
- `string Href` - Sayfa yolu
- `string Icon` - MudBlazor icon
- `string Title` - Menu metni
- `NavLinkMatch Match` - Default: Prefix
- `bool ShowWhenDisabled` - Pasif gosterim (tenant feature yok)

Bu component NavMenu'deki tekrarlayan kodu ortadan kaldirir.

#### Faz 2.5: Unauthorized Redirect/Fallback

`App.razor` veya `Routes.razor` icerisinde `AuthorizeRouteView` icin `NotAuthorized` fallback:
- Giris yapilmamissa: `/auth/login`'e yonlendir
- Giris yapilmis ama yetkisizse: "Erisim Yetkiniz Yok" sayfasi goster

#### Faz 2.6: Server-Side Endpoint Korumasi

Blazor Server'da sayfalar server-side calistigi icin `[Authorize(Policy=...)]` yeterli. Ancak ek guvenlik icin:
- Business layer'da kritik islemler icin permission kontrolu (opsiyonel, cunku Blazor Server'da client-side bypass riski dusuk)
- API endpoint'leri varsa (SignalR Hub, minimal API) bunlara da `[Authorize]` eklenmeli

---

## Bagimllik Sirasi

```
1. Login Permission Bug Fix (Adim 1.1)          -- HER SEYIN ONKOŞULU
   |
2. RoleService Permission Kaydi Fix (Adim 1.2)  -- Roller duzgun kaydedilmeli
   |
3. PermissionGuardedNavLink Component (Faz 2.4) -- Yeniden kullanilabilir component
   |
4. NavMenu Permission Korumasi (Faz 2.2)        -- PermissionGuardedNavLink kullanarak
   |
5. Sayfa Permission Attribute'leri (Faz 2.1)    -- Tum sayfalara [Authorize(Policy=...)]
   |
6. UpgradePromptDialog (Faz 2.3)                -- Pasif gosterim icin dialog
   |
7. Unauthorized Fallback (Faz 2.5)              -- Yetkisiz erisim sayfasi
   |
8. User Management UX (Adim 1.3)                -- Son adim, iyilestirmeler
```

---

## Test Stratejisi

### Unit Testler (TDD-First)

1. **AuthService.LoginAsync Permission Test**
   - Verify: Login sonucunda `UserLoginSuccessDto.Roles[].RoleClaims` dolu geliyor
   - Dosya: `Test/Entegrasyon.Test/Business/AuthServiceTests.cs` (yeni veya mevcut)

2. **RoleService.AddRole Permission Kaydi Test**
   - Verify: AddRole cagirildiginda RoleClaims DB'ye kaydediliyor
   - Dosya: `Test/Entegrasyon.Test/Business/RoleServiceTests.cs` (mevcut, genisletilecek)

3. **RoleService.UpdateRole Permission Guncelleme Test**
   - Verify: UpdateRole cagirildiginda eski RoleClaims silinip yenileri ekleniyor
   - Dosya: `Test/Entegrasyon.Test/Business/RoleServiceTests.cs`

4. **TenantFeatureAuthorizationHandler Testleri**
   - Verify: Admin olmayan kullanici + dogru permission = Succeed
   - Verify: Admin olmayan kullanici + yanlis permission = Fail (Succeed cagirilmamis)
   - Dosya: `Test/Entegrasyon.Test/Tenants/TenantFeatureAuthorizationHandlerTests.cs` (mevcut, genisletilecek)

### Integration Testler

5. **Login -> Permission Propagation E2E**
   - Senaryo: Kullanici login olur, belirli permission'a sahip rolu vardir, korunmus sayfaya erisir
   - Dosya: `Test/Entegrasyon.IntegrationTest/Auth/PermissionIntegrationTests.cs`

### bUnit Testler

6. **PermissionGuardedNavLink Render Test**
   - Verify: Permission varsa link gorunur
   - Verify: Permission yoksa link gorunmez
   - Verify: Feature disabled ise pasif gorunur
   - Dosya: `Test/Entegrasyon.BunitTest/PermissionGuardedNavLinkTests.cs`

### E2E Testler

7. **NavMenu Permission Visibility**
   - Admin kullanici: Tum menuler gorunur
   - Kisitli kullanici: Sadece yetkili menuler gorunur
   - Dosya: `Test/Entegrasyon.E2E/Auth/PermissionE2ETests.cs`

---

## Risk Analizi

### Yuksek Risk

| Risk | Etki | Azaltma |
|---|---|---|
| Login bug fix sonrasi Admin kullanicinin permission'lari bos | Admin rolu IsInRole("Admin") bypass ile calisiyor, ama diger roller kirilabilir | Admin rol icin tum permission'lari seed etmek veya "Admin" bypass'ini korumak |
| RoleService permission kaydi fix'i mevcut rolleri etkiler | DB'deki roller permission'siz kalabilir | Migration ile mevcut Admin rolune tum permission'lari ekleyen seed |
| FeatureService tenant check'i basarisiz olursa tum sayfalar kitlenir | `IsFeatureEnabledAsync` false donerse `TenantFeatureAuthorizationHandler` Fail donuyor | Default tenant icin tum feature'lari enabled yapan fallback |

### Orta Risk

| Risk | Etki | Azaltma |
|---|---|---|
| NavMenu'ye cok fazla AuthorizeView eklemek performans etkisi | Her menu ogesi icin async permission check | PermissionGuardedNavLink icinde permission cache'lemesi |
| _Imports.razor'daki `[Authorize]` + sayfa-level `[Authorize(Policy=...)]` cakismasi | Cift kontrol overhead | _Imports.razor'daki genel `[Authorize]` korunmali (giris kontrolu), sayfa-level policy ek katman |
| Mevcut test admin kullanicisi (admin/123456789) permission'siz kalabilir | E2E testler kirilir | Test seed'inde admin kullaniciya tum permission'lari ver |

### Dusuk Risk

| Risk | Etki | Azaltma |
|---|---|---|
| UserSession'daki Permissions listesi cok buyurse ProtectedLocalStorage limiti | Browser storage limiti | 62 permission string icin ~2KB, sorun olmaz |
| FeatureGate + AuthorizeView ic ice kullanim karmasikligi | Bakim zorlasmasi | PermissionGuardedNavLink ile sarmalama |

---

## Dosya Degisiklik Ozeti

### Degistirilecek Dosyalar

1. `Application/Entegrasyon.Business/Concrete/Auth/AuthService.cs` - ThenInclude(RoleClaims)
2. `Application/Entegrasyon.Business/Concrete/Auth/RoleService.cs` - AddRole/UpdateRole permission kaydi
3. `Application/Entegrasyon.Blazor/Components/Shared/NavMenu.razor` - Tum menulere permission kontrolu
4. ~53 `.razor` dosyasi - `[Authorize(Policy=...)]` ekleme

### Yeni Dosyalar

5. `Application/Entegrasyon.Blazor/Components/Shared/PermissionGuardedNavLink.razor(.cs)` - Yeniden kullanilabilir nav component
6. `Application/Entegrasyon.Blazor/Components/Shared/UpgradePromptDialog.razor(.cs)` - Pasif ozellik dialog
7. `Test/Entegrasyon.Test/Business/AuthServiceLoginPermissionTests.cs` - Login permission testleri
8. `Test/Entegrasyon.BunitTest/PermissionGuardedNavLinkTests.cs` - bUnit testler

### Genisletilecek Test Dosyalari

9. `Test/Entegrasyon.Test/Business/RoleServiceTests.cs` - Permission kaydi testleri
10. `Test/Entegrasyon.Test/Tenants/TenantFeatureAuthorizationHandlerTests.cs` - Ek senaryolar

---

## Tahmini Is Yukleri

| Adim | Karmasiklik | Tahmini Sure |
|---|---|---|
| Login Bug Fix + Test | Dusuk | 30 dk |
| RoleService Permission Fix + Test | Orta | 1 saat |
| PermissionGuardedNavLink Component | Orta | 1 saat |
| NavMenu Tam Permission | Orta | 1 saat |
| 53 Sayfa Permission Attribute | Dusuk (mekanik, Ollama'ya offload) | 30 dk |
| UpgradePromptDialog | Dusuk | 30 dk |
| Unauthorized Fallback | Dusuk | 15 dk |
| User Management UX | Orta | 1.5 saat |
| **TOPLAM** | | **~6.5 saat** |
