# Entegrasyon

Turkiye pazaryeri entegrasyon ve e-ticaret yonetim platformu. Trendyol, Hepsiburada, Amazon, N11, Pazarama, PttAVM, Ciceksepeti ve Temu gibi pazaryerlerini tek bir panelden yonetmeye olanak tanir. Urun, siparis, stok, fiyat, fatura, kargo ve musteri islemlerini merkezi olarak yurutur. Ayrica musteri odakli bir Storefront (e-ticaret vitrini) ve cok saticilik (multi-vendor / marketplace) altyapisi icerir.

---

## Icindekiler

- [Teknoloji Stack'i](#teknoloji-stacki)
- [Mimari Genel Bakis](#mimari-genel-bakis)
- [Proje Yapisi](#proje-yapisi)
- [Gereksinimler](#gereksinimler)
- [Kurulum ve Calistirma](#kurulum-ve-calistirma)
- [Gelistirme Kurallari](#gelistirme-kurallari)
- [Test Altyapisi](#test-altyapisi)
- [Marketplace Entegrasyonlari](#marketplace-entegrasyonlari)
- [Storefront (Musteri Paneli)](#storefront-musteri-paneli)
- [Veritabani](#veritabani)
- [Konfigurasyon](#konfigurasyon)
- [DI Kayit Yapisi](#di-kayit-yapisi)
- [CI/CD Pipeline](#cicd-pipeline)
- [Monitoring](#monitoring)
- [Katkida Bulunma](#katkida-bulunma)

---

## Teknoloji Stack'i

| Katman | Teknoloji |
|---|---|
| Runtime | .NET 8 (LTS), C# 12 |
| Web UI (Admin) | Blazor Server, MudBlazor 8.x |
| Web UI (Storefront) | ASP.NET Core MVC, Tailwind CSS |
| Desktop | .NET MAUI / Blazor Hybrid |
| ORM | Entity Framework Core 8 (PostgreSQL - Npgsql) |
| Veritabani | PostgreSQL 16 |
| Cache | Redis (StackExchange.Redis) |
| Object Storage | MinIO + SixLabors.ImageSharp |
| Mapping | Mapster 7.x |
| Validation | FluentValidation 11.x |
| Loglama | Serilog (Console, File, Grafana Loki) |
| Real-time | SignalR |
| Odeme | iyzico |
| E-Fatura | Parasut entegrasyonu |
| Etiket/Fis | ZPL (BinaryKits.Zpl.Label), ESC/POS |
| Excel | ClosedXML |
| 2FA | Otp.NET (TOTP) |
| Test | xUnit, Moq, FluentAssertions, bUnit, Testcontainers, Respawn, Playwright (NUnit) |
| CI/CD | GitHub Actions |
| Containerization | Docker, Docker Compose |
| Monitoring | Grafana, Loki, Uptime Kuma |

---

## Mimari Genel Bakis

Klasik katmanli mimari kullanilir. Bagimlilik yonu her zaman asagidan yukariya dogrudur:

```
+-------------------------------------------------------------------+
|                      Presentation Layer                           |
|  +------------------+  +------------------+  +------------------+ |
|  | Entegrasyon      |  | Entegrasyon      |  | Entegrasyon      | |
|  | .Blazor          |  | .Storefront      |  | .Desktop         | |
|  | (Admin Panel)    |  | (Musteri Vitrini)|  | (MAUI Hybrid)    | |
|  | Blazor Server    |  | MVC + Tailwind   |  |                  | |
|  | MudBlazor        |  |                  |  |                  | |
|  +--------+---------+  +--------+---------+  +--------+---------+ |
|           |                      |                     |           |
+-------------------------------------------------------------------+
            |                      |                     |
            v                      v                     v
+-------------------------------------------------------------------+
|                   Entegrasyon.ApplicationBootstrap                 |
|              (DI Container, Konfigurasyon, Storage)                |
+-------------------------------------------------------------------+
            |
            v
+-------------------------------------------------------------------+
|                     Business Layer                                 |
|  +------------------+  +------------------+  +------------------+ |
|  | Entegrasyon      |  | BackgroundServices|  | Notifications   | |
|  | .Business        |  | (27 hosted svc)  |  | (SignalR, Email) | |
|  | (144 interface)  |  |                  |  |                  | |
|  +--------+---------+  +--------+---------+  +--------+---------+ |
+-------------------------------------------------------------------+
            |
            v
+-------------------------------------------------------------------+
|                    Data Access Layer                               |
|               Entegrasyon.DataAccess                               |
|     (IntegrationDbContext, Migrations, EntityConfigurations)       |
+-------------------------------------------------------------------+
            |
            v
+-------------------------------------------------------------------+
|                      Entity Layer                                  |
|                 Entegrasyon.Entity                                  |
|          (Domain modelleri, DTOlar, BaseEntity)                    |
+-------------------------------------------------------------------+
```

Ek projeler:

| Proje | Gorev |
|---|---|
| `Entegrasyon.PrintAgent` | Bagimsiz etiket/fis yazici servisi (API Key auth ile korunan REST API) |
| `Entegrasyon.PrintAgent.Contracts` | PrintAgent ile paylasilan kontrat arayuzleri |
| `Entegrasyon.AdminPanel` | MVC + SQLite ile musteri/tenant yonetim paneli (vertical slice mimari) |

---

## Proje Yapisi

```
Entegrasyon/
|
|-- Application/
|   |-- Entegrasyon.Entity/                  # Domain modelleri ve DTOlar
|   |   |-- Common/                          #   BaseEntity, Pageable, Results, FilterParameter
|   |   |-- Core/                            #   Address, BranchOffice, CargoCompany, MarketPlace, Image
|   |   |-- Categories/                      #   CategoryAttribute, CategoryAttributeCategory, CategoryAttributeValue
|   |   |-- Products/                        #   Product, AttributeKeyValue, BarcodeSequence
|   |   |-- Customers/                       #   Customer -> RetailCustomer / CorporateCustomer
|   |   |-- Orders/                          #   Order, OrderItem
|   |   |-- Sales/                           #   Sale, SaleItem
|   |   |-- Brands/                          #   Brand, BrandImport
|   |   |-- Invoices/                        #   Fatura modelleri
|   |   |-- Shipping/                        #   Kargo modelleri
|   |   |-- Marketplace/                     #   MarketPlace eslesme modelleri
|   |   |-- Matches/                         #   Kategori/marka/ozellik eslestirme
|   |   |-- User/                            #   ApplicationUser, Role, Claim
|   |   |-- Storefront/                      #   Musteri paneli entity'leri
|   |   |   |-- Auth/                        #     StorefrontCustomerAuth, LoginHistory, PushSubscription, 2FA Recovery
|   |   |   |-- Commerce/                    #     Cart, CartItem, Wallet, WalletTransaction, GiftCard, PaymentConfig
|   |   |   |-- Content/                     #     StorefrontSettings, Banner, Page, SizeGuide, DomainMapping
|   |   |   |-- Engagement/                  #     Review, Wishlist, Contact, Return, ProductQuestion
|   |   |   |-- Marketing/                   #     Newsletter, Loyalty, Referral, StockNotification, EmailCampaign, AbandonedCartEmail
|   |   |   |-- Marketplace/                 #     Seller, SellerProduct, Commission, Payout, Balance, Transaction
|   |   |-- Dtos/                            #   Tum DTO'lar (Storefront, Marketplace, vb.)
|   |   |-- Labels/, Logs/, Notifications/   #   Diger domain modelleri
|   |   |-- Settings/, Templates/, POS/      #   Uygulama ayarlari, sablonlar
|   |
|   |-- Entegrasyon.DataAccess/              # EF Core katmani
|   |   |-- Concrete/EntityFrameworkCore/
|   |       |-- Contexts/
|   |       |   |-- IntegrationDbContext.cs   #   Ana DbContext
|   |       |   |-- Seed/                    #   Seed data
|   |       |-- EntityConfigurations/        #   Fluent API konfigurasyonlari
|   |       |-- Migrations/                  #   77 migration dosyasi
|   |
|   |-- Entegrasyon.Business/               # Is mantigi katmani
|   |   |-- Abstract/                        #   144 interface (IXxxManager / IXxxService)
|   |   |-- Concrete/                        #   Manager implementasyonlari
|   |   |   |-- Trendyol/                    #     Trendyol REST API, mapper, import, e-Fatura
|   |   |   |-- Hepsiburada/                 #     Hepsiburada REST API, listing, claim, Q&A
|   |   |   |-- Amazon/                      #     Amazon SP-API, OAuth, Feeds, Listings
|   |   |   |-- N11/                         #     N11 SOAP istemcisi, urun, siparis, iade
|   |   |   |-- Pazarama/                    #     Pazarama REST + OAuth2, marka, siparis, iade
|   |   |   |-- Pttavm/                      #     PttAVM iki API istemcisi (Catalog + Shipment)
|   |   |   |-- Ciceksepeti/                 #     Ciceksepeti REST + API Key, fatura, Q&A
|   |   |   |-- Temu/                        #     Temu API istemcisi, kategori import
|   |   |   |-- Storefront/                  #     30 storefront servisi (auth, cart, checkout, wallet, vb.)
|   |   |   |-- Kargo/                       #     Yurtici, Surat, Aras kargo entegrasyonlari + mock'lar
|   |   |   |-- Shipping/                    #     Kargo takip adapterleri (ICargoTrackingAdapter)
|   |   |   |-- Invoicing/                   #     E-Fatura (Parasut), Trendyol e-Fatura
|   |   |   |-- POS/                         #     POS oturum yonetimi
|   |   |   |-- BulkOperations/              #     Excel import/export, toplu islemler
|   |   |   |-- Import/                      #     Kategori, marka, urun import servisleri
|   |   |   |-- Auth/                        #     JWT auth, rol yonetimi
|   |   |-- BackgroundServices/              #   27 hosted background service
|   |   |-- Channels/                        #   EventChannel<T> pattern (pub/sub)
|   |   |-- Validation/FluentValidation/     #   FluentValidation validator'lari
|   |   |-- MapperProfiles/                  #   Mapster mapping konfigurasyonlari (MappingConfig.cs)
|   |   |-- Notifications/                   #   SignalR + Email bildirim servisleri
|   |   |-- Labels/                          #   ZPL etiket (BinaryKits) ve ESC/POS fis uretimi
|   |   |-- FileStorage/                     #   MinIO + ImageSharp abstraction
|   |   |-- Utility/                         #   Yardimci siniflar, sabitler (StringConstants)
|   |
|   |-- Entegrasyon.ApplicationBootstrap/    # DI container kurulumu
|   |   |-- ApplicationDependencyExtension.cs  # Tum servis kayitlari (~500 satir)
|   |   |-- FileStorage/                     #   MinIO options ve storage abstraction
|   |
|   |-- Entegrasyon.Blazor/                 # Admin paneli (Blazor Server + MudBlazor)
|   |   |-- Features/                        #   Feature-based klasor yapisi (24 feature)
|   |   |   |-- Admin/                       #     Rol yonetimi
|   |   |   |-- Attributes/                  #     Ozellik yonetimi (split view: liste + detay paneli)
|   |   |   |-- Auth/                        #     Giris/kayit sayfalari
|   |   |   |-- Brands/                      #     Marka yonetimi
|   |   |   |-- BulkOperations/              #     Toplu islemler (Excel import/export)
|   |   |   |-- Categories/                  #     Kategori yonetimi (tree view)
|   |   |   |-- CategoryImport/              #     Marketplace kategori import
|   |   |   |-- Customers/                   #     Musteri yonetimi (Bireysel/Kurumsal)
|   |   |   |-- Dashboard/                   #     Anasayfa dashboard
|   |   |   |-- Invoicing/                   #     Fatura yonetimi (e-Fatura, e-Arsiv)
|   |   |   |-- MarketplaceSync/             #     Marketplace senkronizasyon
|   |   |   |-- MatchedEntityImport/         #     Eslestirilmis entity import
|   |   |   |-- Notifications/               #     Bildirim yonetimi
|   |   |   |-- Orders/                      #     Siparis yonetimi
|   |   |   |-- POS/                         #     POS satis noktasi
|   |   |   |-- Printing/                    #     Etiket/fis yazdirma
|   |   |   |-- Products/                    #     Urun yonetimi (cok adimli ekleme)
|   |   |   |-- Profile/                     #     Kullanici profili
|   |   |   |-- Reports/                     #     Raporlama
|   |   |   |-- Sales/                       #     Satis yonetimi
|   |   |   |-- Settings/                    #     Uygulama ayarlari
|   |   |   |-- Shipping/                    #     Kargo yonetimi ve takip
|   |   |   |-- Storefront/                  #     Storefront yonetim sayfalari (20 sayfa)
|   |   |   |-- Users/                       #     Kullanici yonetimi
|   |   |-- Components/Shared/              #   Layout, NavMenu, ortak bilesenler
|   |   |-- ViewModels/                      #   Blazor'a ozel view model'ler
|   |   |-- wwwroot/                         #   Statik dosyalar, stiller
|   |
|   |-- Entegrasyon.Storefront/             # Musteri vitrini (MVC + Tailwind CSS)
|   |   |-- Controllers/                     #   20 controller (Auth, Cart, Checkout, Catalog, vb.)
|   |   |-- Views/                           #   Razor view'lari
|   |   |-- ViewComponents/                  #   Tekrar kullanilabilir UI parcalari
|   |   |-- Middleware/                       #   Tenant, auth middleware
|   |   |-- Infrastructure/                  #   Storefront altyapi servisleri
|   |   |-- Styles/ + tailwind.config.js     #   Tailwind CSS konfigurasyonu
|   |   |-- TagHelpers/                      #   Custom tag helper'lar
|   |
|   |-- Entegrasyon.AdminPanel/             # Musteri/Tenant yonetim paneli (MVC + SQLite)
|   |-- Entegrasyon.Desktop/                # Desktop uygulamasi (MAUI Blazor Hybrid)
|
|-- Agent/
|   |-- Entegrasyon.PrintAgent/             # Bagimsiz yazici servisi (API Key auth)
|   |-- Entegrasyon.PrintAgent.Contracts/   # Paylasilan kontratlar
|
|-- Test/
|   |-- Entegrasyon.Test/                   # Unit testler (xUnit + Moq + FluentAssertions)
|   |-- Entegrasyon.IntegrationTest/        # Entegrasyon testleri (Testcontainers + Respawn)
|   |-- Entegrasyon.BunitTest/              # bUnit (Blazor component) testleri
|   |-- Entegrasyon.AdminPanel.Test/        # AdminPanel MVC controller testleri
|   |-- Entegrasyon.E2E/                    # E2E testler (Playwright + NUnit)
|
|-- docs/                                    # Marketplace API dokumanlari
|   |-- trendyol/                            #   Trendyol API (~108 endpoint)
|   |-- trendyol_efatura/                    #   Trendyol e-Fatura (27 endpoint)
|   |-- n11/                                 #   N11 SOAP API (187 sayfa, 15 servis)
|   |-- hepsiburada/                         #   Hepsiburada REST API (10 dosya, 4 base URL)
|   |-- pazarama/                            #   Pazarama API (10 dosya, OAuth2)
|   |-- pttavm/                              #   PttAVM API (5 dosya, 2 base URL)
|   |-- ciceksepeti/                         #   Ciceksepeti API (10 dosya, ~22 endpoint)
|
|-- docker-compose.yml                       # Gelistirme ortami (PostgreSQL, Redis, pgAdmin)
|-- docker-compose.stage.yml                 # Staging deploy
|-- docker-compose.prod.yml                  # Production deploy
|-- docker-compose.e2e.yml                   # E2E test ortami (PostgreSQL + MinIO + Blazor)
|-- docker-compose.monitoring.yml            # Grafana + Loki + Uptime Kuma
|-- .github/workflows/                       # CI/CD pipeline tanimlari (4 workflow)
|-- CLAUDE.md                                # Claude Code AI asistani rehberi
```

---

## Gereksinimler

| Gereksinim | Minimum Surum | Not |
|---|---|---|
| .NET SDK | 8.0.x | `dotnet --version` ile kontrol edin |
| Docker & Docker Compose | Docker Engine 20+ | PostgreSQL, Redis, MinIO icin |
| Node.js | 18+ | Yalnizca Storefront Tailwind CSS build icin |
| PostgreSQL | 16 | docker-compose ile otomatik gelir |
| Redis | Alpine | docker-compose ile otomatik gelir |

---

## Kurulum ve Calistirma

### 1. Depoyu Klonla

```bash
git clone <repo-url> Entegrasyon
cd Entegrasyon
```

### 2. Altyapi Servislerini Baslat

```bash
# PostgreSQL (5432), Redis (6379), pgAdmin (5050) ayaga kalkar
docker compose up -d
```

Servis detaylari:

| Servis | Port | Amac |
|---|---|---|
| PostgreSQL 16 | 5432 | Ana veritabani |
| Redis | 6379 | Distributed cache |
| pgAdmin 4 | 5050 | Veritabani yonetim arayuzu |

### 3. Veritabani Migration'larini Uygula

```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor
```

### 4. Uygulamayi Calistir

```bash
# Admin paneli (Blazor Server)
cd Application/Entegrasyon.Blazor && dotnet run

# Storefront (MVC) -- ayri bir terminal
cd Application/Entegrasyon.Storefront && dotnet run
```

### 5. Storefront Tailwind CSS (Gelistirme Modu)

```bash
cd Application/Entegrasyon.Storefront
npm install
npx tailwindcss -i ./Styles/input.css -o ./wwwroot/css/site.css --watch
```

### 6. Tum Solution'i Build Et

```bash
dotnet build Entegrasyon.sln
```

---

## Gelistirme Kurallari

### TDD-First (Zorunlu Kural)

Her yeni ozellik ve bug fix icin kesinlikle su sira izlenir:

1. **RED** -- Testi yaz, calistir, basarisiz oldugunu dogrula
2. **GREEN** -- Minimum kodu implement et, testi gecir
3. **REFACTOR** -- Gerekiyorsa kodu iyilestir
4. **VERIFY** -- Tum test suite'lerini calistir

```bash
# Sira ile calistirilmasi gereken test komutlari
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj
```

Test olmadan ozellik tamamlanmis **sayilmaz**. "Testleri sonra yazariz" kabul **edilmez**.

### Business Layer Pipeline (Zorunlu Kural)

Tum Business Manager metotlari kesinlikle su 3 adimli akisi takip eder:

```
1. Validation      ->  FluentValidation ile DTO/obje dogrulama
                         Basarisiz? -> Validasyon hatalarini iceren Result don
2. Business Rules  ->  LogicRunner ile is kurallari kontrolu
                         Basarisiz? -> Is kurali hatalarini don
3. Execution       ->  Sadece 1 ve 2 basarili ise asil islem gerceklesir
```

Ornek implementasyon:

```csharp
public class ProductManager(
    IntegrationDbContext dbContext,
    IValidator<CreateProductDto> validator) : IProductService
{
    public async Task<IResult> AddProduct(CreateProductDto dto)
    {
        // 1. Validation
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return new ErrorResult(validationResult.Errors);

        // 2. Business Rules
        var rulesResult = LogicRunner.Run(
            () => CheckStockAvailability(dto),
            () => CheckUniqueName(dto));
        if (!rulesResult.Success)
            return rulesResult;

        // 3. Execution
        var product = dto.Adapt<Product>();
        await dbContext.Products.AddAsync(product);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Urun eklendi.");
    }
}
```

### Code-Behind Pattern (Zorunlu Kural)

Blazor `.razor` dosyalarinda uzun C# kodu **yazilmaz**. Her `.razor` dosyasinin mutlaka bir `.razor.cs` (code-behind) dosyasi olmalidir:

```
ProductsPage.razor      --> Sadece HTML/Razor markup
ProductsPage.razor.cs   --> Tum C# kodu, event handler'lar, state yonetimi
```

### Feature-Based Folder Structure (Zorunlu Kural)

Dosyalar teknik rollerine gore degil, ait olduklari ozellige gore gruplanir:

```
Features/
  Products/
    ProductsPage.razor
    ProductsPage.razor.cs
    ProductDialog.razor          <-- tek kullanimlik bile olsa buraya
    ProductDialog.razor.cs
  Categories/
    CategoriesPage.razor
    CategoriesPage.razor.cs
    CategoryDialog.razor
    CategoryDialog.razor.cs
```

Yalnizca birden fazla feature tarafindan paylasilan yapilar `Components/Shared/` altina konulur.

### Primary Constructor DI (C# 12)

Tum servisler primary constructor ile DI alir:

```csharp
public class ProductManager(
    IntegrationDbContext dbContext,
    IValidator<CreateProductDto> validator,
    IImageManager imageManager) : IProductService
{
    // dbContext, validator, imageManager dogrudan kullanilir
}
```

### Multi-Tenant Tasarim (Zorunlu Kural)

Sistem ileride multi-tenant yapilacak. Tum yeni gelistirmelerde tenant izolasyonu goz onunde bulundurulmalidir:

- **Singleton servislerde in-memory state:** Tek bir field yerine `ConcurrentDictionary<int, T>` kullan (key = tenantId veya MarketPlaceId). Ozellikle OAuth token cache'leri bu kurala tabidir.
- **SemaphoreSlim:** Tenant basina izole lock mekanizmasi kullan, global tek lock degil.
- **DB query'leri:** Tum sorgularda tenant filtresi uygulanabilir olmali.
- **Configuration:** Tenant-specific config'ler DB'den okunmali, appsettings.json'a hardcode edilmemeli.
- **Test sorusu:** Tasarimda her zaman "Bu N tenant ile calisir mi?" sorusunu sor.

### Diger Onemli Kurallar

| Kural | Aciklama |
|---|---|
| **DbContext Presentation'da yok** | Blazor'da asla DbContext inject edilmez, her zaman business layer interface'leri kullanilir |
| **Task.WhenAll yasak (DbContext)** | Ayni scoped DbContext'i paylasan servis cagrilari paralel calistirilmaz |
| **MudBlazor alias** | `MudBlazor.CategoryAttribute` cakmasi icin `using AppCategoryAttribute = ...` kullan |
| **MudDataGrid** | `Items` parametresi `IEnumerable<T>` ister, `List<T>` uzerinde `.AsEnumerable()` cagir |
| **Error Handling** | Data-loading'te try-catch yazma (ErrorBoundary yakalar), sadece user-action handler'larda tut |
| **Layout + DB** | Layout component'lari DB erisiminde `IServiceScopeFactory` kullanmalidir |
| **Exception yasak (Utility)** | Utility/calculator siniflarinda exception firlatma, dogrulama business katmaninda yapilir |
| **Cift loglama** | `IApplicationLogManager` (Turkce, admin icin) + `ILogger<T>` (teknik, gelistirici icin) birlikte kullanilir |

---

## Test Altyapisi

### Genel Bakis

| Tur | Framework | Proje | Test Sayisi |
|---|---|---|---|
| Unit | xUnit + Moq + FluentAssertions | `Test/Entegrasyon.Test/` | ~97 |
| Integration | xUnit + Testcontainers + Respawn + WebApplicationFactory | `Test/Entegrasyon.IntegrationTest/` | -- |
| bUnit | bUnit + MudBlazor | `Test/Entegrasyon.BunitTest/` | 7 |
| AdminPanel | xUnit (MVC controller) | `Test/Entegrasyon.AdminPanel.Test/` | 39 |
| E2E | NUnit + Playwright | `Test/Entegrasyon.E2E/` | 25 |

### Unit Testler

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj

# Tek test calistirma
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ProductManagerTests"
```

- **Araclar:** xUnit, Moq, FluentAssertions, `Microsoft.EntityFrameworkCore.InMemory`
- **Kapsam:** Business manager'lar, calculator'lar, mapper'lar, validator'lar, marketplace servisleri
- **In-Memory DB:** Test icin InMemory provider kullanilir

### Entegrasyon Testleri

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```

- **Araclar:** xUnit, Testcontainers.PostgreSql, Respawn, Microsoft.AspNetCore.Mvc.Testing
- **Nasil Calisir:**
  1. Testcontainers ile gecici PostgreSQL container'i otomatik olusturulur
  2. Migration'lar uygulanir
  3. `WebApplicationFactory` uzerinden gercek DI container'i kullanilir
  4. Respawn her test sonrasi DB'yi temizler (seed tablolar korunur)
- **On Kosul:** Docker daemon calisir olmali
- **Ozel Testler:** DI container kayit testi (`DiContainerTests.cs`), migration tutarliligi testi (`MigrationTests.cs`)

### bUnit Testleri

```bash
dotnet test Test/Entegrasyon.BunitTest/Entegrasyon.BunitTest.csproj
```

- **Araclar:** bUnit + MudBlazor
- **Kapsam:** Blazor component rendering, event handling
- **Onemli Notlar:**
  - `MudPopoverProvider` standalone `RenderComponent<>()` ile render edilmeli
  - `JSInterop.Mode = JSRuntimeMode.Loose` gerekli
  - `Services.AddSingleton<ISnackbar>(mock)` `AddMudServices()` sonrasina eklenmeli (son kazanir)

### AdminPanel Testleri

```bash
dotnet test Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj
```

- **Kapsam:** MVC controller testleri (SQLite in-memory)

### E2E Testleri

```bash
# Uygulama debug modda ayakta olmali (veya docker-compose.e2e.yml)
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj
```

- **Araclar:** NUnit + Playwright
- **Varsayilan URL:** `http://localhost:5099` (`E2E_BASE_URL` environment variable ile degistirilebilir)
- **Playwright Kurulumu:**
  ```bash
  pwsh Test/Entegrasyon.E2E/bin/Debug/net8.0/playwright.ps1 install --with-deps chromium
  ```
- **Docker ile E2E:**
  ```bash
  docker compose -f docker-compose.e2e.yml up -d --build --wait
  dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj
  docker compose -f docker-compose.e2e.yml down -v
  ```

---

## Marketplace Entegrasyonlari

Her pazaryeri icin `UseMock` konfigurasyonu mevcuttur. Gelistirme ortaminda varsayilan olarak mock servisler aktiftir (`UseMock: true`). Production'da gercek API istemcileri devreye girer.

### Desteklenen Pazaryerleri

| Pazaryeri | MarketPlaceId | Protokol | Ozellikler | Durum |
|---|---|---|---|---|
| **Trendyol** | 1 | REST | Urun CRUD, stok/fiyat sync, siparis polling, batch status, e-Fatura (27 endpoint), kategori/marka import | Tam |
| **N11** | 2 | SOAP (WSDL) | Urun, stok/fiyat sync, siparis polling, iade | Tam |
| **Hepsiburada** | 3 | REST | Urun, listing, stok/fiyat sync, siparis, iade/claim, soru-cevap | Tam |
| **Amazon** | 4 | REST (SP-API) | OAuth2 token, catalog, listings, feeds, siparis, stok/fiyat sync (FBM, TR+EU) | Tam |
| **Pazarama** | 5 | REST + OAuth2 | Urun, stok/fiyat sync, siparis, iade, batch status, kategori/marka import | Tam |
| **PttAVM** | 7 | REST + Token | Urun, stok/fiyat sync, siparis, kargo, fatura (iki ayri API: Catalog + Shipment) | Tam |
| **Ciceksepeti** | 8 | REST + API Key | Urun, stok/fiyat sync, siparis, fatura, iade, soru-cevap, kategori import | Tam |
| **Temu** | - | REST | API istemcisi, kategori import | Baslangic |

### Her Pazaryeri Icin Ortak Bilesenler

| Bilesen | Amac |
|---|---|
| `XxxApiClient` / `XxxSoapClient` | HTTP/SOAP istemcisi |
| `XxxProductMapper` | Ic urun modeli <-> marketplace modeli donusumu |
| `XxxProductService` | Urun CRUD islemleri |
| `XxxStockPriceService` | Stok ve fiyat senkronizasyonu |
| `XxxOrderService` | Siparis cekme ve durum guncelleme |
| `XxxMappingValidator` | Marketplace'e gonderilmeden once veri dogrulama |
| `XxxCategoryImporter` | Marketplace kategori agacini import etme |
| `MockXxx...` | Gercek API olmadan gelistirme icin mock implementasyon |

### Background Servisler (Marketplace)

27 adet hosted background service aktiftir:

| Kategori | Servisler |
|---|---|
| **Stok & Fiyat Sync** | Trendyol, Hepsiburada, N11, Amazon, Pazarama, PttAVM, Ciceksepeti (7 servis) |
| **Siparis Polling** | Her pazaryeri icin bir adet (7 servis) |
| **Batch Status Polling** | Trendyol, Hepsiburada, Ciceksepeti, Pazarama, Amazon (listing + feed) |
| **Urun Publish** | `TrendyolProductPublishBackgroundService` |
| **Kategori Import** | `CategoryImportBackgroundService` |
| **Dashboard** | `DashboardRefreshService` |
| **E-Fatura** | `TrendyolEFaturaStatusPollingService` |
| **Kargo Takip** | `ShipmentStatusUpdateService` |
| **Terk Edilen Sepet** | `AbandonedCartBackgroundService` |

### Kargo Entegrasyonlari

| Kargo Firmasi | Ozellikler | Mock |
|---|---|---|
| Yurtici Kargo | Gonderi olusturma, takip | Var |
| Surat Kargo | Gonderi olusturma, takip | Var |
| Aras Kargo | Gonderi olusturma, takip | Var |

Tum kargo servisleri `ICargoTrackingAdapter` interface'i uzerinden birlestirilen takip mekanizmasina sahiptir (`ShipmentTrackingManager`).

### EventChannel Deseni

Marketplace senkronizasyonu `EventChannel<T>` deseni ile calisir:

```csharp
// Stok/fiyat degisikligi -> tum marketplace sync servisleri tetiklenir
EventChannel<StockPriceChangedEvent>

// Yeni urun -> marketplace'lere yayinlama
EventChannel<ProductCreatedForMarketplaceEvent>

// Kategori guncelleme -> re-sync
EventChannel<CategoryUpdatedEvent>
```

---

## Storefront (Musteri Paneli)

`Entegrasyon.Storefront` projesi, musteriye yonelik e-ticaret vitrinidir. ASP.NET Core MVC + Tailwind CSS ile insa edilmistir. Google ve Facebook ile sosyal giris destekler.

### Storefront Controller'lari

| Controller | Sorumluluk |
|---|---|
| `AuthController` | Giris, kayit, sifre sifirlama |
| `AccountController` | Profil, adres, siparis gecmisi, KVKK export |
| `CatalogController` | Kategori listeleme, filtreleme |
| `ProductController` | Urun detay, varyant secimi |
| `CartController` | Sepet yonetimi |
| `CheckoutController` | Odeme akisi |
| `WishlistController` | Istek listesi |
| `CompareController` | Urun karsilastirma |
| `ContactController` | Iletisim formu |
| `NewsletterController` | Newsletter abonelik |
| `GiftCardController` | Hediye karti kullanimi |
| `TrackingController` | Siparis takibi |
| `PushController` | Push notification abonelik |
| `StockNotificationController` | Stok bildirimi |
| `SellerController` | Satici profil ve kayit |
| `SellerProductController` | Satici urun yonetimi |
| `SeoController` | Sitemap, robots.txt |
| `ErrorController` | Hata sayfalari |
| `HomeController` | Ana sayfa |
| `PageController` | Statik sayfalar |

### Storefront Ozellikleri (Detayli)

**Kimlik Dogrulama ve Hesap**
- E-posta/sifre ile kayit ve giris
- Google ve Facebook ile sosyal giris
- 2FA (TOTP) destegi, recovery code'lar
- Sifre sifirlama (e-posta ile)
- E-posta dogrulama
- Giris gecmisi takibi
- Push notification aboneligi

**Urun ve Katalog**
- Urun listeleme, filtreleme, siralama
- Arama otomatik tamamlama (debounced)
- Urun karsilastirma
- Beden rehberi (size guide)
- Stok bildirimi (urun gelince haber ver)
- Urun soru-cevap (Q&A)

**Sepet ve Satin Alma**
- Sepet yonetimi (ekleme, cikarma, adet degistirme)
- Kupon/indirim kodu uygulama
- Hediye karti kullanimi
- Dijital cuzdan (wallet) ile odeme
- iyzico odeme entegrasyonu
- Checkout akisi (adres, kargo, odeme)
- Kayitli sepet ogesi (save for later)
- Tekrar satin al (buy again)
- Terk edilmis sepet e-postalari (abandoned cart, background service ile)

**Siparis Yonetimi**
- Siparis gecmisi ve detay goruntuleme
- Siparis takibi (kargo entegrasyonu)
- Fatura goruntuleme
- Siparis iptali
- Iade talepleri

**Musteri Deneyimi**
- Urun degerlendirme ve yorum (review)
- Istek listesi (wishlist)
- Iletisim formu
- Newsletter aboneligi
- E-posta kampanyalari (otomatik)
- Sadakat puani (loyalty points) ve islemler
- Referans programi (referral)
- Son arananlar gecmisi
- KVKK veri export'u

**Multi-Vendor / Marketplace Altyapisi**
- Satici kayit ve profil yonetimi
- Satici urun yonetimi
- Satici siparis yonetimi
- Komisyon hesaplama (SellerCommission)
- Odeme/payout yonetimi (PayoutRequest)
- Satici bakiye ve islem takibi (SellerBalance, SellerTransaction)

**Icerik Yonetimi (Admin Blazor Panelinden)**
- Banner yonetimi (pozisyon bazli: BannerPosition enum)
- Statik sayfa yonetimi (StorefrontPage)
- Yasal metinler (KVKK, mesafeli satis sozlesmesi)
- Odeme konfigurasyonu (StorefrontPaymentConfig)
- Storefront genel ayarlari (StorefrontSettings)
- Kampanya yonetimi
- Satici yonetimi ve onaylama
- Iade talepleri yonetimi
- Musteri yorumlari moderasyonu

**E-posta Servisi**
- SMTP transactional e-postalar
- E-posta dogrulama
- Sifre sifirlama e-postasi
- Siparis onay e-postasi
- Terk edilmis sepet hatirlatma e-postasi
- Kampanya e-postalari

---

## Veritabani

### Genel Bilgiler

| Ozellik | Deger |
|---|---|
| Veritabani | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Provider | Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11 |
| Tracking | Varsayilan no-tracking (`QueryTrackingBehavior.NoTracking`) |
| Query Splitting | Aktif (`SplitQuery`) |
| Migration Sayisi | 77 |
| Soft Delete | `IsDeleted` flag ile |

### BaseEntity

Tum entity'ler `BaseEntity` sinifindan turemektedir:

```csharp
public class BaseEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

`SaveChangesAsync()` cagirildiginda `CreatedAt` ve `UpdatedAt` alanlari otomatik olarak UTC'ye donusturulur.

### Migration Komutlari

```bash
# Mevcut migration'lari uygula
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor

# Yeni migration olustur
dotnet ef migrations add <MigrationName> \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor
```

### Onemli Domain Modelleri

**Kategori ve Ozellik Sistemi:**
```
Category --< CategoryAttributeCategory >-- CategoryAttribute --< CategoryAttributeValue
               (IsRequired, IsVarianter, IsSlicer)    (Key, Humanized, AllowCustom)

Product --< AttributeKeyValue (CategoryAttributeId, AttributeValueId?, CustomValue)
```

**Marketplace Eslestirme:**
```
CategoryAttribute --< CategoryAttributeMarketPlaceMatch (pazaryerine ozel ozellik eslestirmesi)
CategoryAttributeValue --< CategoryAttributeValueMarketPlaceMatch (pazaryerine ozel deger eslestirmesi)
```

**Musteri Kalitimi:**
```
Customer (base)
  |-- RetailCustomer (bireysel)
  |-- CorporateCustomer (kurumsal)
```

**Storefront Entity Gruplari:**
- **Auth:** `StorefrontCustomerAuth`, `StorefrontLoginHistory`, `StorefrontPushSubscription`, `StorefrontTwoFactorRecoveryCode`
- **Commerce:** `Cart`, `CartItem`, `StorefrontWallet`, `StorefrontWalletTransaction`, `StorefrontGiftCard`, `StorefrontSavedCartItem`
- **Content:** `StorefrontSettings`, `StorefrontBanner`, `StorefrontPage`, `StorefrontSizeGuide`, `StorefrontDomainMapping`
- **Engagement:** `StorefrontReview`, `StorefrontWishlistItem`, `StorefrontContactMessage`, `StorefrontReturnRequest`, `StorefrontProductQuestion`
- **Marketing:** `StorefrontNewsletter`, `StorefrontLoyaltyPoints`, `StorefrontReferral`, `StorefrontStockNotification`, `StorefrontEmailCampaign`, `StorefrontAbandonedCartEmail`
- **Marketplace:** `Seller`, `SellerProduct`, `SellerCommission`, `PayoutRequest`, `SellerBalance`, `SellerTransaction`

---

## Konfigurasyon

### appsettings.json Yapisi

```jsonc
{
  // Veritabani baglantilari
  "ConnectionStrings": {
    "Main": "Host=localhost;Port=5432;Database=IntegrationDb;Username=...;Password=...",
    "Redis": "localhost:6379"
  },

  // JWT Token
  "JwtTokenOptions": {
    "SecurityKey": "...",
    "Audience": "www.entegrasyon.com",
    "Issuer": "www.entegrasyon.com",
    "AccessTokenExpiration": 30
  },

  // MinIO Object Storage
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "...",
    "SecretKey": "...",
    "UseSSL": false,
    "BucketName": "products",
    "PublicBaseUrl": "http://localhost:9000"
  },

  // Marketplace Mock/Real secimi
  "Trendyol":       { "UseMock": true, "DefaultMarketPlaceId": 1 },
  "Hepsiburada":    { "UseMock": true, "DefaultMarketPlaceId": 3 },
  "Amazon":         { "UseMock": true, "DefaultMarketPlaceId": 5, "MarketplaceIds": ["A33AVAJ2PDY3EV"] },
  "N11":            { "UseMock": true },
  "Pazarama":       { "UseMock": true },
  "Pttavm":         { "UseMock": true },
  "Ciceksepeti":    { "UseMock": true },
  "Temu":           { "UseMock": true },

  // Kargo Mock/Real secimi
  "YurticiKargo":   { "UseMock": true },
  "SuratKargo":     { "UseMock": true },
  "ArasKargo":      { "UseMock": true },

  // E-Fatura
  "TrendyolEFatura": { "UseMock": true },
  "EInvoice":        { "UseMock": true },

  // Serilog loglama
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [{ "Name": "File", "Args": { "path": "Logs/log.txt", "rollingInterval": "Day" } }]
  },

  // Background servis ayarlari
  "BackgroundServices": { "TrendyolCategoryImport": 10 },
  "Barcode": { "ClearDelay": 10 },

  // Storefront sosyal giris
  "Authentication": {
    "Google": { "ClientId": "", "ClientSecret": "" },
    "Facebook": { "AppId": "", "AppSecret": "" }
  }
}
```

### Ortam Dosyalari

| Dosya | Amac |
|---|---|
| `appsettings.json` | Temel/varsayilan ayarlar (tum ortamlar icin) |
| `appsettings.Development.json` | Gelistirme ortami (baglanti bilgileri, detayli loglama, EF SQL loglama) |
| `appsettings.Staging.json` | Staging ortami |
| `appsettings.Production.json` | Production ortami |
| `appsettings.Testing.json` | Test ortami (E2E docker-compose icin) |
| `appsettings.e2e.json` | E2E test konfigurasyonu |
| `appsettings.IntegrationTest.json` | Entegrasyon test konfigurasyonu |

### Environment Variables (Docker/CI)

Production ve staging ortamlarinda hassas bilgiler environment variable olarak aktarilir:

```bash
# Veritabani
ConnectionStrings__Main="Host=db;Port=5432;Database=IntegrationDb;..."
ConnectionStrings__Redis="redis:6379"

# MinIO
Minio__Endpoint="minio:9000"
Minio__AccessKey="..."
Minio__SecretKey="..."
Minio__PublicBaseUrl="http://cdn.example.com"

# JWT
JwtTokenOptions__SecurityKey="..."

# Marketplace (production'da false)
Trendyol__UseMock=false
Hepsiburada__UseMock=false
```

---

## DI Kayit Yapisi

Tum bagimlilik kayitlari `ApplicationBootstrapExtensions` sinifindaki extension method'lar uzerinden yapilir.

Dosya yolu: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

| Method | Sorumluluk | Detay |
|---|---|---|
| `AddApplicationDependencies(config)` | Tum business manager'lar | 144 interface, marketplace servisleri (mock/real secimi config'e gore), kargo, e-fatura, event channel'lar, validator'lar, Mapster profilleri |
| `AddCustomDbContext(config)` | PostgreSQL baglantisi | `IDbContextFactory<IntegrationDbContext>`, no-tracking, split query, DEBUG modda sensitive data logging |
| `AddBackgroundServices()` | Background servisler | 27 hosted service (polling, sync, import, dashboard, abandoned cart) |
| `AddStorageServices(config)` | File storage | MinIO client (`IMinioFileStorage`) + ImageSharp (`IImageProcessingService`) |
| `AddClients()` | Named HttpClient'lar | Trendyol API, Hepsiburada API base URL'leri |
| `AddSignalRSettings()` | SignalR | Custom `IUserIdProvider` |
| `AddNotification()` | Bildirim | `ISignalRNotificationSender` + `IEmailSender` |
| `AddStorefrontServices()` | Storefront | 30+ servis: auth, cart, checkout, wallet, seller, Q&A, abandoned cart vb. |
| `AddConfigurations()` | API davranisi | `SuppressModelStateInvalidFilter` |

### Mock / Real Servis Secimi Mekanizmasi

Her marketplace ve kargo entegrasyonu icin `UseMock` flag'i kontrol edilir. `true` ise mock implementasyon, `false` ise gercek API istemcisi DI'a kaydedilir:

```csharp
var useMock = configuration.GetValue<bool>("Trendyol:UseMock", true);
if (useMock)
{
    services.AddScoped<ITrendyolProductService, MockTrendyolProductService>();
    services.AddScoped<ITrendyolStockPriceService, MockTrendyolStockPriceService>();
    services.AddScoped<ITrendyolOrderService, MockTrendyolOrderService>();
}
else
{
    services.AddScoped<ITrendyolProductService, TrendyolProductService>();
    services.AddScoped<ITrendyolStockPriceService, TrendyolStockPriceService>();
    services.AddScoped<ITrendyolOrderService, TrendyolOrderService>();
}
```

Bu desen tum pazaryerleri (Trendyol, Hepsiburada, Amazon, N11, Pazarama, PttAVM, Ciceksepeti, Temu) ve kargo firmalari (Yurtici, Surat, Aras) icin tekrarlanir.

---

## CI/CD Pipeline

GitHub Actions ile 4 farkli workflow tanimlidir. Tum workflow dosyalari `.github/workflows/` altindadir.

### 1. CI (ci.yml)

- **Tetikleme:** `push` (main, stage, develop) ve `pull_request` (main, stage)
- **Adimlar:** .NET 8 Setup -> Restore -> Build (Release) -> Unit Test
- **Amac:** Her push ve PR'da temel build ve test kontrolu

### 2. Deploy to Stage (deploy-stage.yml)

- **Tetikleme:** `push` (stage branch)
- **Adimlar:**
  1. Build + Unit Test
  2. SSH ile sunucuya baglan
  3. `git pull origin stage`
  4. `docker compose -f docker-compose.stage.yml up --build -d`
  5. Health check (port 8081)

### 3. Deploy to Production (deploy-prod.yml)

- **Tetikleme:** `push` (main branch)
- **Ortam:** `production` (GitHub environment korumalari aktif -- onay gerekebilir)
- **Adimlar:**
  1. Build + Unit Test
  2. SSH ile sunucuya baglan
  3. `git pull origin main`
  4. `docker compose -f docker-compose.prod.yml up --build -d`
  5. Health check (port 8080)

### 4. E2E Tests (e2e.yml)

- **Tetikleme:** `pull_request` (main, stage) ve `workflow_dispatch` (elle tetikleme)
- **Adimlar:**
  1. Unit Test (on kosul)
  2. `docker compose -f docker-compose.e2e.yml up -d --build --wait`
  3. Uygulama hazir olana kadar bekle (30 deneme, 5 saniye aralik)
  4. .NET 8 Setup + E2E proje build
  5. Playwright chromium install
  6. E2E testleri calistir
  7. Test artifact'larini upload et (14 gun sakla)
  8. Ortami temizle (`docker compose down -v`)

---

## Monitoring

Monitoring stack'i ayri bir docker-compose dosyasiyla yonetilir:

```bash
docker compose -f docker-compose.monitoring.yml up -d
```

| Servis | Port | Amac |
|---|---|---|
| Grafana | 3000 | Dashboard ve gorselestirme (log sorgulama, metrik panelleri) |
| Loki | 3100 | Log aggregation (Serilog.Sinks.Grafana.Loki ile entegre) |
| Uptime Kuma | 3001 | Uptime monitoring, health check, alert bildirimleri |

Monitoring servisleri `entegrasyon_entegrasyon-net` external network'une baglidir, boylece uygulama container'lariyla ayni agda calisir.

---

## Katkida Bulunma

### Branch Stratejisi

```
main (master)   <-- Production (deploy-prod.yml tetiklenir)
  |
  stage          <-- Staging (deploy-stage.yml tetiklenir)
    |
    develop      <-- Aktif gelistirme branch'i
      |
      feature/xxx  <-- Ozellik branch'leri (develop'tan acilar)
```

### Gelistirme Akisi

1. `develop` branch'inden yeni bir `feature/xxx` branch'i ac
2. TDD-First: Once testi yaz, sonra implement et
3. Tum testleri calistir:
   ```bash
   dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
   dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
   dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj
   ```
4. `develop` branch'ine PR ac
5. CI otomatik calisir (build + unit test)
6. Code review sonrasi merge et
7. `develop` -> `stage` merge ile staging'e deploy edilir
8. Staging ortaminda test et
9. `stage` -> `main` merge ile production'a deploy edilir

### Commit Mesaj Formati

Conventional Commits kullanilir:

```
feat(storefront): add wallet payment support
feat(trendyol): implement batch status polling
fix(hepsiburada): correct order status mapping
refactor(entity): reorganize entity folders
chore(deps): update MudBlazor to 8.15.0
test(business): add product manager validation tests
docs: update API documentation
```

Format: `<type>(<scope>): <description>`

Turler: `feat`, `fix`, `refactor`, `chore`, `test`, `docs`, `perf`, `style`

### PR Sureci

1. PR acildiginda CI otomatik calisir (build + unit test)
2. `main` veya `stage` hedefli PR'larda E2E testleri de calisir
3. En az bir reviewer onayi gerekir
4. Tum CI check'leri gecmeli
5. Merge sonrasi ilgili branch'e deploy otomatik tetiklenir

### Onemli Uyarilar

- Production'a (`main`) dogrudan push **yapilmaz**, her zaman PR uzerinden merge edilir
- Test olmadan ozellik tamamlanmis **sayilmaz**
- Blazor'da asla DbContext inject **edilmez**, her zaman business layer interface'leri kullanilir
- Utility/calculator siniflarinda exception **firlatilmaz**, dogrulama business katmaninda FluentValidation ile yapilir
- Layout component'lari DB erisiminde `IServiceScopeFactory` kullanmalidir
- Ayni scoped DbContext'i paylasan servis cagrilari `Task.WhenAll` ile **paralel calistirilmaz**

---

## Lisans

Ozel proje. Tum haklari saklidir.
