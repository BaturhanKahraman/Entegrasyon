# Implementasyon Promptlari

**Tarih:** 2026-03-24
**Amac:** Her ozelligin Claude Opus 4.6 modeline verilecek bagımsiz implementasyon promptlarini icerir.
**Kaynak:** `docs/rakip-analizi.md` (onceliklendirme) + `docs/offline-satis-plani.md` (offline satis detaylari)

---

## 1. E-Fatura Entegrasyonu

**Oncelik:** P0
**Tahmini Karmasiklik:** Yuksek
**Bagimliliklar:** Yok (bagimsiz baslanabilir)

### Prompt

> **Gorev:** Entegrasyon projesine E-Fatura / E-Arsiv entegrasyonu ekle.
>
> **Proje Mimarisi:** Klasik katmanli .NET 8 Blazor Server uygulamasi. Entity -> DataAccess (EF Core, PostgreSQL) -> Business -> Blazor (MudBlazor). Primary constructor DI kullanilir. Tum business metodlari 3 adimli pipeline izler: (1) FluentValidation, (2) LogicRunner is kurallari, (3) Execution. Her `.razor` dosyasinin `.razor.cs` code-behind'i olmali. Feature-based klasor yapisi (`Features/Invoicing/`). Multi-tenant uyumlu tasarim (tenant bazli config DB'den okunmali, singleton cache'lerde `ConcurrentDictionary<int, T>`). TDD-first: once test yaz, sonra implement et.
>
> **Ne Yapilacak:**
>
> **Entity Katmani** (`Application/Entegrasyon.Entity/`):
> - `Invoicing/EInvoice.cs` entity'si: InvoiceNumber, InvoiceType (enum: EFatura, EArsiv), Status (enum: Draft, Sent, Accepted, Rejected, Cancelled), CustomerTaxId, CustomerTitle, IssueDate, TotalAmount, TaxAmount, GrandTotal, GibUuid, XmlContent, PdfUrl, SaleId (FK), MarketplaceOrderId (nullable FK), IntegratorProvider (enum: Parasut, ForIba, Logo, Custom)
> - `Invoicing/EInvoiceLine.cs`: ProductName, Quantity, UnitPrice, TaxRate, TaxAmount, LineTotal
> - `Invoicing/EInvoiceIntegratorConfig.cs`: IntegratorProvider, ApiUrl, ApiKey, ApiSecret, Username, Password, IsActive — tenant-specific ayar icin (DB'den okunacak, appsettings'e hardcode edilmeyecek)
> - DTO'lar: `CreateEInvoiceDto`, `EInvoiceListDto`, `EInvoiceDetailDto`, `BulkInvoiceDto`
>
> **DataAccess Katmani** (`Application/Entegrasyon.DataAccess/`):
> - `IntegrationDbContext`'e DbSet ekle
> - `EntityConfigurations/EInvoiceConfiguration.cs` — index'ler: GibUuid (unique), InvoiceNumber, SaleId, Status
> - Migration olustur
>
> **Business Katmani** (`Application/Entegrasyon.Business/`):
> - `Abstract/IEInvoiceManager.cs` interface: CreateInvoice, CreateBulkInvoices, GetInvoices, GetInvoiceDetail, CancelInvoice, SendToGib, DownloadPdf, GetInvoiceFromSale
> - `Concrete/EInvoiceManager.cs` — `SaleManager.cs` pattern'ini takip et (bkz: `Business/Concrete/SaleManager.cs`). Primary constructor DI, IDbContextFactory kullan
> - `Abstract/IEInvoiceIntegratorClient.cs` interface: SendInvoice, CancelInvoice, GetStatus, DownloadPdf
> - `Concrete/Invoicing/ParasutInvoiceClient.cs` — Parasut API v4 entegrasyonu (REST, OAuth2)
> - `Concrete/Invoicing/MockInvoiceClient.cs` — test icin mock implementasyon (Kargo pattern'ini takip et, bkz: `Business/Concrete/Kargo/MockArasKargoService.cs`)
> - `Validation/FluentValidation/CreateEInvoiceValidator.cs`
> - GIB E-Fatura UBL-TR XML olusturma utility'si: `Utility/EInvoice/UblTrXmlBuilder.cs`
> - Satis'ten otomatik fatura olusturma: `SaleManager` icerisine veya ayri bir servis olarak `GetInvoiceFromSale` methodu
>
> **Blazor Katmani** (`Application/Entegrasyon.Blazor/`):
> - `Features/Invoicing/InvoicesPage.razor` + `.razor.cs` — fatura listesi (MudDataGrid, filtreleme: tarih araligi, durum, tip)
> - `Features/Invoicing/InvoiceDetailDialog.razor` + `.razor.cs` — fatura detay/onizleme
> - `Features/Invoicing/CreateInvoiceDialog.razor` + `.razor.cs` — tek fatura olusturma
> - `Features/Invoicing/BulkInvoiceDialog.razor` + `.razor.cs` — toplu fatura kesimi (secilen Siparişlerden)
> - `Features/Invoicing/InvoiceSettingsPanel.razor` + `.razor.cs` — entegrator ayarlari (IntegrationSettings sayfasina tab olarak eklenebilir)
> - NavMenu'ye "E-Fatura" linki ekle (`Components/Shared/NavMenu.razor`)
>
> **DI Kayit:** `ApplicationDependencyExtension.cs`'ye IEInvoiceManager ve IEInvoiceIntegratorClient kayitlarini ekle.
>
> **Testler** (`Test/Entegrasyon.Test/`):
> - `Invoicing/EInvoiceManagerTests.cs` — en az 8 test: fatura olusturma (basarili), validation hatasi, bos kalem hatasi, toplu fatura, iptal (basarili/basarisiz), durum sorgulama, satis'ten fatura olusturma
> - `Invoicing/UblTrXmlBuilderTests.cs` — en az 4 test: gecerli XML uretimi, zorunlu alan eksikligi, KDV hesaplama, yuvarlama
>
> **Kodlama Dili:** Turkce aciklama, Ingilizce kod. Degisken/method/class isimleri Ingilizce, yorum ve log mesajlari Turkce.

### Beklenen Cikti
- 3 yeni entity + DTO'lar, 1 EF configuration + migration
- 1 interface + 1 manager + 1 integrator interface + 2 integrator implementasyonu (Parasut + Mock)
- 1 validator + 1 XML builder utility
- 5 Blazor component (sayfa + dialog'lar)
- NavMenu guncelleme, DI kayit guncelleme
- En az 12 unit test

---

## 2. Raporlama Dashboard

**Oncelik:** P0
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Yok (mevcut Sale, Order, BranchOfficeStock entity'leri yeterli)

### Prompt

> **Gorev:** Mevcut raporlama altyapisini genisleterek kapsamli bir raporlama dashboard'u olustur.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, MudBlazor UI. Mevcut `ReportManager.cs` (bkz: `Business/Concrete/ReportManager.cs`) ve `IReportManager.cs` zaten var — bu dosyalari genislet. Mevcut `Features/Reports/` klasorunde `SalesReport.razor`, `MarketplaceReport.razor`, `InventoryReport.razor` zaten var. `Features/Dashboard/` klasorunde `Index.razor` ve yardimci component'lar var. Primary constructor DI, 3 adimli pipeline (validation -> business rules -> execution), code-behind zorunlu, multi-tenant uyumlu (sorgularda tenant filtresi uygulanabilir olmali).
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Dtos/Reports/` klasorune yeni DTO'lar ekle:
>   - `ProfitLossReportDto`: Revenue, CostOfGoods, MarketplaceCommission, CargoExpense, TaxAmount, NetProfit, ProfitMargin, ByMarketplace (liste)
>   - `ProductPerformanceDto`: ProductId, ProductName, TotalSold, TotalRevenue, ReturnRate, AverageRating, StockTurnoverRate
>   - `StockAlertDto`: ProductVariantId, Barcode, ProductName, CurrentStock, MinimumStock, DaysUntilStockout, SuggestedOrderQuantity
>   - `MarketplaceSummaryDto`: MarketplaceId, MarketplaceName, TotalOrders, TotalRevenue, CommissionPaid, AverageOrderValue, TopSellingProducts
>
> **Business Katmani:**
> - `IReportManager`'a yeni metodlar ekle: GetProfitLossReport, GetProductPerformance, GetStockAlerts, GetMarketplaceSummary, GetTopSellingProducts, GetSlowMovingProducts
> - `ReportManager.cs`'yi genislet — mevcut raw SQL pattern'ini takip et (ReportManager zaten raw SQL kullaniyor). Performans icin EF yerine raw SQL tercih et
> - Komisyon hesaplama icin `Utility/CommissionCalculator.cs` — pazaryeri bazli komisyon orani hesaplama (simdilik sabit oranlar, ileride dinamik)
>
> **Blazor Katmani:**
> - `Features/Reports/ProfitLossReport.razor` + `.razor.cs` — kar/zarar raporu, MudChart ile gorsellestirilmis, tarih araligi secimi, pazaryeri filtresi
> - `Features/Reports/ProductPerformanceReport.razor` + `.razor.cs` — urun performansi, MudDataGrid, siralama/filtreleme
> - `Features/Reports/StockAlertsReport.razor` + `.razor.cs` — stok uyarilari (kritik, dusuk, yeterli renk kodlari)
> - `Features/Dashboard/Index.razor` guncelle — mevcut dashboard'a yeni kartlar ekle: bugunun kari, komisyon toplami, kritik stok sayisi
> - `Features/Dashboard/DashboardProfitChart.razor` + `.razor.cs` — son 30 gunluk kar grafigi (MudChart)
>
> **Testler:**
> - `Reports/ReportManagerTests.cs` — en az 6 test: kar/zarar hesaplama, urun performansi siralama, stok uyarisi esik degerleri, bos veri durumu, tarih araligi filtresi, pazaryeri filtresi
> - `Reports/CommissionCalculatorTests.cs` — en az 4 test: Trendyol komisyon, HB komisyon, N11 komisyon, bilinmeyen marketplace

### Beklenen Cikti
- 4 yeni DTO
- IReportManager'a 6 yeni method + ReportManager implementasyonu
- CommissionCalculator utility
- 3 yeni rapor sayfasi + 1 dashboard component
- Dashboard guncelleme
- En az 10 unit test

---

## 3. Komisyon Hesaplama Katmani

**Oncelik:** P0
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Pazaryeri bazli komisyon hesaplama sistemi olustur. Her pazaryerinin kategori bazinda farkli komisyon oranlari var. Bu oranlar DB'de saklanacak ve satis/rapor hesaplamalarinda kullanilacak.
>
> **Proje Mimarisi:** .NET 8, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, multi-tenant uyumlu tasarim. Mevcut `MarketPlace` entity'si var (Id=1 Trendyol, 2 N11, 3 HB, 4 Amazon, 5 Pazarama, 7 PttAVM, 8 Ciceksepeti). Mevcut `MarketplaceOverrideManager.cs` pattern'ini takip et (bkz: `Business/Concrete/MarketplaceOverrideManager.cs`).
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Marketplace/MarketplaceCommissionRate.cs`: MarketPlaceId (FK), CategoryId (nullable — null ise genel oran), CommissionPercent (decimal), MinCommission (decimal, nullable), MaxCommission (decimal, nullable), EffectiveFrom (DateTime), EffectiveTo (DateTime, nullable), IsActive
> - `Dtos/Commission/CommissionCalculationResultDto.cs`: GrossAmount, CommissionPercent, CommissionAmount, NetAmount, TaxOnCommission
> - `Dtos/Commission/SetCommissionRateDto.cs`: MarketPlaceId, CategoryId, CommissionPercent, MinCommission, MaxCommission, EffectiveFrom
>
> **DataAccess:**
> - `EntityConfigurations/MarketplaceCommissionRateConfiguration.cs` — composite index: (MarketPlaceId, CategoryId, EffectiveFrom)
> - Migration
>
> **Business Katmani:**
> - `Abstract/ICommissionManager.cs`: CalculateCommission(marketplaceId, categoryId, grossAmount), GetCommissionRates(marketplaceId), SetCommissionRate(dto), GetEffectiveRate(marketplaceId, categoryId, date)
> - `Concrete/CommissionManager.cs` — pipeline pattern. Komisyon orani arama sirasi: (1) marketplace + category icin spesifik oran, (2) marketplace icin genel oran (categoryId null), (3) varsayilan oran (const). Rate cache'i `ConcurrentDictionary<(int marketplaceId, int? categoryId), decimal>` ile multi-tenant uyumlu
> - `Validation/FluentValidation/SetCommissionRateValidator.cs`
>
> **Blazor:**
> - `Features/Settings/CommissionSettings.razor` + `.razor.cs` — IntegrationSettings sayfasina tab olarak ekle. Pazaryeri secimi, kategori bazli oran girisi, MudDataGrid ile mevcut oranlari listeleme
>
> **DI:** `ApplicationDependencyExtension.cs`'ye kayit ekle.
>
> **Testler:**
> - `Commission/CommissionManagerTests.cs` — en az 8 test: spesifik kategori orani, genel marketplace orani, varsayilan oran fallback, gecerlilik tarihi kontrolu, oran guncelleme, bos oran durumu, hesaplama dogrulugu (yuvarlama), min/max komisyon siniri

### Beklenen Cikti
- 1 entity + 2 DTO
- 1 EF config + migration
- 1 interface + 1 manager + 1 validator
- 1 Blazor component (settings tab'i)
- DI kayit guncelleme
- En az 8 unit test

---

## 4. Offline Satis (Agent Genisletme)

**Oncelik:** P0
**Tahmini Karmasiklik:** Yuksek
**Bagimliliklar:** Yok (mevcut Agent altyapisi uzerine insa)

### Prompt

> **Gorev:** Mevcut `Agent/Entegrasyon.PrintAgent/` projesini genisleterek offline satis yeteneği ekle (Faz 1 + Faz 2). Detayli plan icin `docs/offline-satis-plani.md` dosyasini referans al.
>
> **Mevcut Agent:** `Agent/Entegrasyon.PrintAgent/` — .NET 8 minimal Web API, Kestrel, `https://localhost:19100`, API key middleware, ZPL + ESC/POS yazici destegi. `PrintAgent.Contracts` projesi DTO'lar icin mevcut. Agent'in veritabani yok — tamamen stateless.
>
> **Mevcut Satis Domain'i:** `Sale` -> `SaleItem[]`, `BranchOfficeStock`, `StockMovement`, `ProductVariant` (Barcode, ListPrice, SalePrice, VatRate). Satis DTO: `MakeSaleDto(SalePersonId, CustomerId, GeneralDiscount, BranchOfficeId, SaleItems)`. Mevcut `SaleManager.MakeSale()` akisi: validation -> stok kontrolu -> stok dusme -> kayit (bkz: `Business/Concrete/SaleManager.cs`).
>
> **Ne Yapilacak (Faz 1 — Temel Altyapi):**
>
> **Agent Projesine EF Core SQLite Ekle:**
> - `Agent/Entegrasyon.PrintAgent/` projesine `Microsoft.EntityFrameworkCore.Sqlite` NuGet paketi
> - `LocalData/LocalDbContext.cs` — SQLite context
> - Local entity'ler (sunucu entity'lerinin hafif kopyalari):
>   - `LocalProduct`: Id, Name, Brand, CategoryId, CategoryName
>   - `LocalProductVariant`: Id, ProductId, Barcode, ListPrice, SalePrice, VatRate
>   - `LocalBranchOfficeStock`: ProductVariantId, BranchOfficeId, CurrentStock
>   - `LocalSale`: Id (Guid), SalePersonId, CustomerId, BranchOfficeId, GeneralDiscount, TotalAmount, CreatedAt, SyncStatus (enum: Pending, Synced, Failed, Conflict), SyncedAt, RetryCount, ErrorMessage
>   - `LocalSaleItem`: SaleId, ProductVariantId, Quantity, UnitPrice, DiscountPercent, TaxPercentage
>   - `LocalCustomer`: Id, Name, TaxId
>   - `SyncMetadata`: LastSyncTimestamp, EntityType, RecordCount
> - SQLite migration'lari
>
> **Sunucu Tarafinda Sync API (Blazor Server):**
> - `Application/Entegrasyon.Blazor/Endpoints/SyncEndpoints.cs` — minimal API:
>   - `GET /api/sync/catalog?since={timestamp}&branchOfficeId={id}` — urun + variant delta
>   - `GET /api/sync/stock/{branchOfficeId}` — guncel stok
>   - `GET /api/sync/customers?since={timestamp}` — musteri delta
>   - `POST /api/sync/sales` — offline satislari toplu gonderme
>   - `GET /api/sync/status` — sync durumu
> - API key ile guvenlik (Agent'in mevcut API key mekanizmasini kullan)
>
> **Faz 2 — Offline Satis API (Agent'ta):**
> - `POST /api/sales` — offline satis olusturma (SQLite'a yazar, local stok duser)
> - `GET /api/products/search?barcode={barcode}` — barkod ile arama
> - `GET /api/products/search?query={text}` — isim ile arama
> - `GET /api/stock/{branchOfficeId}` — sube stok durumu
> - `POST /api/sync/pull` — sunucudan veri cekme (catalog + stock + customers)
> - Stok dusme mantigi: local SQLite'da atomic (mevcut `OfficeStockManager.DecreaseStockAtomicAsync` benzer mantik)
> - Fis yazdirma: mevcut `PrintService` kullan
>
> **Testler:**
> - Agent unit testleri: LocalSaleService satis olusturma (basarili, stok yetersiz, barkod bulunamadi)
> - SyncEndpoints integration testleri: catalog delta sync, stock sync, sales batch upload
> - En az 10 test

### Beklenen Cikti
- Agent'a SQLite + EF Core eklenmesi, LocalDbContext + 7 local entity
- Sunucu tarafinda 5 sync endpoint (SyncEndpoints.cs)
- Agent tarafinda 5 offline API endpoint
- Local stok yonetimi mantigi
- En az 10 test

---

## 5. Rekabet Analizi / Rakip Fiyat Takibi

**Oncelik:** P1
**Tahmini Karmasiklik:** Yuksek
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Pazaryerlerinde rakip fiyatlarini otomatik takip eden ve raporlayan bir Rekabet Analizi modulu olustur.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant (tenant bazli config DB'den, `ConcurrentDictionary` cache). Feature-based klasor yapisi. Background servisler icin `EventChannel` pattern'i mevcut (bkz: `Business/BackgroundServices/`).
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Competition/CompetitorPrice.cs`: MarketPlaceId, ProductVariantId (FK), CompetitorName, CompetitorPrice, CompetitorUrl, OurPrice, PriceDifference, PriceDifferencePercent, CapturedAt, Source (enum: ApiScrape, ManualEntry)
> - `Competition/PriceTrackingRule.cs`: ProductVariantId (FK), MarketPlaceId, MinPrice, MaxPrice, AutoAdjust (bool), AdjustStrategy (enum: MatchLowest, UnderCutByPercent, UnderCutByAmount), AdjustValue (decimal), IsActive
> - `Competition/PriceChangeLog.cs`: ProductVariantId, MarketPlaceId, OldPrice, NewPrice, Reason, ChangedAt, TriggeredByRuleId
> - DTO'lar: `CompetitorPriceDto`, `PriceTrackingRuleDto`, `CompetitionSummaryDto`
>
> **DataAccess:**
> - EF Configuration'lar + migration
> - Index: (MarketPlaceId, ProductVariantId, CapturedAt)
>
> **Business Katmani:**
> - `Abstract/ICompetitionManager.cs`: GetCompetitorPrices, AddPriceTrackingRule, GetPriceTrackingRules, RunPriceCheck, GetCompetitionSummary, GetPriceHistory
> - `Concrete/Competition/CompetitionManager.cs` — pipeline pattern
> - `Concrete/Competition/TrendyolPriceScraper.cs` — Trendyol'da ayni urunun farkli satici fiyatlarini cekmek icin (Trendyol API'nin satici listeleme endpoint'i)
> - `Abstract/IPriceScraper.cs` — marketplace bazli fiyat toplama interface'i
> - `BackgroundServices/PriceCheckBackgroundService.cs` — periyodik fiyat kontrolu (mevcut background service pattern'ini takip et, bkz: `Business/BackgroundServices/CategoryImportBackgroundService.cs`). SemaphoreSlim tenant bazli izole
> - Validator'lar
>
> **Blazor Katmani:**
> - `Features/Competition/CompetitionDashboard.razor` + `.razor.cs` — ozet kartlar (en dusuk fiyatli oldugumuz urunler, en yuksek fiyat farki, toplam takip edilen urun)
> - `Features/Competition/PriceComparisonGrid.razor` + `.razor.cs` — MudDataGrid, urun bazli rakip fiyat karsilastirmasi, renk kodlari (yesil: biz en ucuz, kirmizi: biz pahali)
> - `Features/Competition/PriceTrackingRules.razor` + `.razor.cs` — kural yonetimi (otomatik fiyat guncelleme kurallari)
> - `Features/Competition/PriceHistoryChart.razor` + `.razor.cs` — urun bazli fiyat gecmisi grafigi (MudChart, bizim fiyat vs rakip fiyat)
> - NavMenu'ye "Rekabet Analizi" linki ekle
>
> **DI + Background Service Kayit:** `ApplicationDependencyExtension.cs` guncelle.
>
> **Testler:**
> - `Competition/CompetitionManagerTests.cs` — en az 8 test: fiyat karsilastirma, kural uygulama (match lowest, undercut), otomatik fiyat guncelleme, gecersiz kural, bos rakip verisi, tarihsel veri filtreleme, ozet hesaplama, fiyat degisim logu
> - `Competition/TrendyolPriceScraperTests.cs` — en az 3 test (mock HTTP)

### Beklenen Cikti
- 3 entity + 3 DTO
- EF config + migration
- 2 interface + 2 manager + 1 background service + 1 scraper
- 4 Blazor component + NavMenu guncelleme
- DI kayit guncelleme
- En az 11 unit test

---

## 6. Akilli Fiyatlandirma (Kural Tabanli Otomatik Fiyat Guncelleme)

**Oncelik:** P1
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Komisyon Hesaplama Katmani (3)

### Prompt

> **Gorev:** Maliyet degisikliklerinde ve kurallara gore otomatik fiyat guncelleme sistemi olustur. Kar marji koruma, platform bazli fiyatlandirma stratejileri ve toplu fiyat guncelleme yetenekleri ekle.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut `MarketplaceOverrideManager` (bkz: `Business/Concrete/MarketplaceOverrideManager.cs`) zaten pazaryeri bazli fiyat override mekanizmasini kullaniyor. `ProductVariantMarketplaceOverride` entity'si mevcut (bkz: `Entity/Products/ProductVariantMarketplaceOverride.cs`).
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Pricing/PricingRule.cs`: Name, Description, RuleType (enum: FixedMargin, PercentMargin, RoundTo99, CompetitorBased, CostPlusPercent), MarketPlaceId (nullable — null ise tum pazaryerleri), CategoryId (nullable), MinMarginPercent, TargetMarginPercent, RoundingStrategy (enum: None, RoundTo99, RoundTo95, RoundDown), Priority (int), IsActive
> - `Pricing/PricingRuleExecution.cs`: PricingRuleId (FK), ProductVariantId, OldPrice, NewPrice, Reason, ExecutedAt, Status (enum: Applied, Skipped, Error), ErrorMessage
> - DTO'lar: `CreatePricingRuleDto`, `PricingRuleListDto`, `PriceSimulationResultDto`, `BulkPriceUpdateDto`
>
> **Business Katmani:**
> - `Abstract/IPricingRuleManager.cs`: CreateRule, GetRules, UpdateRule, DeleteRule, SimulateRule(ruleId) -> hangi urunlerin etkilenecegini goster, ExecuteRule(ruleId), ExecuteAllActiveRules, GetExecutionHistory
> - `Concrete/Pricing/PricingRuleManager.cs` — pipeline pattern. Kural calistirma mantigi: kurallari Priority sirasina gore uygula, MinMarginPercent'in altina dusme, cakisma durumunda oncelikli kural kazanir
> - `Concrete/Pricing/PriceCalculationEngine.cs` — saf hesaplama mantigi (side-effect yok): maliyet + komisyon + kargo + hedef kar marji -> satis fiyati. `ICommissionManager` kullanarak komisyon hesabi yapar
> - Validator
>
> **Blazor:**
> - `Features/Pricing/PricingRulesPage.razor` + `.razor.cs` — kural listesi, CRUD, oncelik siralama (drag-drop)
> - `Features/Pricing/PricingRuleDialog.razor` + `.razor.cs` — kural olusturma/duzenleme formu
> - `Features/Pricing/PriceSimulation.razor` + `.razor.cs` — kural uygulanmadan once etki simulasyonu goster (kac urun, ortalama fiyat degisimi, tahmini gelir degisimi)
> - NavMenu guncelleme
>
> **Testler:**
> - `Pricing/PricingRuleManagerTests.cs` — en az 8 test: sabit marj hesaplama, yuzde marj, 99'a yuvarlama, minimum marj korumasi, oncelik siralama, simulasyon, toplu guncelleme, bos kural seti
> - `Pricing/PriceCalculationEngineTests.cs` — en az 6 test: maliyet + komisyon + marj hesaplama, yuvarlama stratejileri, sinir degerleri

### Beklenen Cikti
- 2 entity + 4 DTO
- EF config + migration
- 1 interface + 2 concrete class + 1 validator
- 3 Blazor component
- En az 14 unit test

---

## 7. Depo Yonetimi (WMS — Raf/Lokasyon Bazli Stok)

**Oncelik:** P1
**Tahmini Karmasiklik:** Yuksek
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Raf ve lokasyon bazli depo yonetim sistemi (WMS) olustur. Mevcut `BranchOfficeStock` yapisinin uzerine lokasyon detayi ekle.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut stok yapisi: `BranchOfficeStock` sube bazinda stok tutar, `StockMovement` her hareketi loglar, `OfficeStockManager` (bkz: `Business/Concrete/OfficeStockManager.cs`) stok islemlerini yonetir.
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Warehouse/WarehouseLocation.cs`: BranchOfficeId (FK), Code (ornek: "A-01-03" = Koridor A, Raf 1, Bolme 3), Aisle (string), Rack (string), Shelf (string), Bin (string), MaxCapacity (int, nullable), IsActive, LocationType (enum: Storage, Picking, Receiving, Shipping, Returns)
> - `Warehouse/LocationStock.cs`: WarehouseLocationId (FK), ProductVariantId (FK), Quantity, MinQuantity (reorder point), MaxQuantity, LastCountedAt
> - `Warehouse/StockTransfer.cs`: FromLocationId, ToLocationId, ProductVariantId, Quantity, TransferredBy (UserId), TransferredAt, Status (enum: Pending, InTransit, Completed, Cancelled), Notes
> - `Warehouse/StockCount.cs`: WarehouseLocationId, ProductVariantId, SystemQuantity, CountedQuantity, Difference, CountedBy (UserId), CountedAt, Status (enum: Pending, Counted, Adjusted), AdjustedAt
> - DTO'lar: `LocationDto`, `LocationStockDto`, `StockTransferDto`, `StockCountDto`, `CreateLocationDto`, `WarehouseSummaryDto`
>
> **DataAccess:**
> - EF config'ler, unique constraint: (WarehouseLocationId, ProductVariantId) on LocationStock
> - Migration
>
> **Business Katmani:**
> - `Abstract/IWarehouseManager.cs`: AddLocation, GetLocations, GetLocationStock, TransferStock, StartStockCount, CompleteStockCount, AdjustStock, GetWarehouseSummary, SuggestPickingLocation(productVariantId) -> FIFO veya en yakin lokasyon
> - `Concrete/Warehouse/WarehouseManager.cs` — pipeline pattern, `IOfficeStockManager` ile entegre (toplam stok BranchOfficeStock'ta, detay LocationStock'ta)
> - Validator'lar
>
> **Blazor:**
> - `Features/Warehouse/WarehouseOverview.razor` + `.razor.cs` — depo gorsel haritasi veya tablo gorunumu, doluluk oranlari
> - `Features/Warehouse/LocationManager.razor` + `.razor.cs` — lokasyon CRUD (MudDataGrid)
> - `Features/Warehouse/StockTransferDialog.razor` + `.razor.cs` — transfer islemi (from -> to lokasyon secimi, barkod okutma)
> - `Features/Warehouse/StockCountPage.razor` + `.razor.cs` — stok sayim islemleri, farklari gosterme, duzeltme onay
> - NavMenu guncelleme
>
> **Testler:**
> - `Warehouse/WarehouseManagerTests.cs` — en az 10 test: lokasyon ekleme, stok transfer (basarili, yetersiz stok, ayni lokasyon hatasi), stok sayim (fark yok, fazla, eksik), picking onerisi, toplam stok tutarliligi, kapasite kontrolu

### Beklenen Cikti
- 4 entity + 6 DTO
- EF config'ler + migration
- 1 interface + 1 manager + validator'lar
- 4 Blazor component
- En az 10 unit test

---

## 8. Barkod Okuyucu (Global Scanner)

**Oncelik:** P1
**Tahmini Karmasiklik:** Dusuk
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Tum sayfalarda calisacak global bir barkod okuyucu alt sistemi ekle. Barkod okutuldugunda bulunulan sayfaya gore farkli aksiyonlar alsın (satis sayfasinda sepete ekle, depo sayfasinda stok goster, urun sayfasinda urun detay ac).
>
> **Proje Mimarisi:** .NET 8 Blazor Server, MudBlazor, code-behind zorunlu. Mevcut `Features/Sales/Sales.razor.cs` dosyasinda barkod input alani zaten var. Mevcut `ProductVariant` entity'sinde `Barcode` alani mevcut.
>
> **Ne Yapilacak:**
>
> **Business Katmani:**
> - `Abstract/IBarcodeResolver.cs`: ResolveBarcode(string barcode) -> `BarcodeResolveResult` (ProductVariantId, ProductId, ProductName, VariantName, Barcode, CurrentPrice, StockInfo)
> - `Concrete/BarcodeResolver.cs` — DB'den barcode ile ProductVariant'i bul, ilgili bilgileri don. Primary constructor DI, IDbContextFactory kullan
>
> **Blazor Katmani:**
> - `Components/Shared/GlobalBarcodeScanner.razor` + `.razor.cs` — MainLayout'a eklenen, her zaman aktif bir component:
>   - Kullanici herhangi bir sayfadayken klavye ile barkod okutabilsin
>   - Fiziksel barkod okuyucu USB HID olarak calisir — hizli tuslama + Enter gonderir
>   - JS Interop ile `keydown` event'lerini dinle, hizli tuslama pattern'ini tespit et (ornegin 200ms icerisinde 8+ karakter + Enter = barkod okuyucu)
>   - Barkod tespit edilince `OnBarcodeScanned` EventCallback tetikle
>   - Sayfaya gore routing: mevcut URL'e bakilarak ilgili aksiyonu tetikle
> - `wwwroot/js/barcode-scanner.js` — klavye event listener, barkod pattern tespiti
> - `Features/Sales/Sales.razor.cs` — GlobalBarcodeScanner'dan gelen event'i dinle, sepete ekle
>
> **DI Kayit:** IBarcodeResolver kaydi.
>
> **Testler:**
> - `Barcode/BarcodeResolverTests.cs` — en az 4 test: gecerli barkod, bulunamayan barkod, silinmis urun barcode'u, coklu sonuc durumu (olmamali, unique constraint)

### Beklenen Cikti
- 1 interface + 1 manager + 1 DTO
- 1 Blazor shared component + JS dosyasi
- Sales sayfasi guncelleme
- DI kayit guncelleme
- En az 4 unit test

---

## 9. Kargo Takip Entegrasyonu (Tek Panel)

**Oncelik:** P1
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Yok (mevcut kargo entegrasyonlari uzerine insa)

### Prompt

> **Gorev:** Mevcut kargo entegrasyonlarini (Aras, Surat, Yurtici — bkz: `Business/Concrete/Kargo/`) birlestiren tek bir kargo takip paneli olustur. Tum kargo firmalarinin gonderi durumlarini tek ekranda goster.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, MudBlazor, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu. Mevcut kargo yapisi: `Business/Concrete/Kargo/` altinda her firma icin ayri Client + Service + Models dosyalari var. Her biri icin Mock implementasyonu da mevcut (test icin).
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Shipping/ShipmentTracking.cs`: OrderId (FK), CargoCompanyId, TrackingNumber, CurrentStatus (enum: Created, PickedUp, InTransit, OutForDelivery, Delivered, Returned, Lost), LastStatusUpdate, EstimatedDeliveryDate, ActualDeliveryDate, RecipientName, RecipientAddress
> - `Shipping/ShipmentStatusHistory.cs`: ShipmentTrackingId (FK), Status, StatusDescription, Location, Timestamp
> - DTO'lar: `ShipmentTrackingDto`, `ShipmentStatusHistoryDto`, `CargoSummaryDto`
>
> **Business Katmani:**
> - `Abstract/IShipmentTrackingManager.cs`: TrackShipment(trackingNumber), GetAllShipments(filters), GetShipmentHistory(trackingNumber), RefreshTrackingStatus(shipmentId), GetCargoSummary
> - `Concrete/Shipping/ShipmentTrackingManager.cs` — mevcut kargo client'larini (ArasKargoClient, SuratKargoClient, YurticiKargoClient) orchestrate eder. Kargo firmasina gore dogru client'i secer
> - `Abstract/ICargoTrackingAdapter.cs` — her kargo firmasinin takip API'sini soyutlayan interface: GetTrackingInfo(trackingNumber), GetStatusHistory(trackingNumber)
> - Her kargo firmasi icin adapter implementasyonu: `ArasTrackingAdapter`, `SuratTrackingAdapter`, `YurticiTrackingAdapter`
> - `BackgroundServices/ShipmentStatusUpdateService.cs` — periyodik gonderi durumu guncelleme (her 30 dk). Mevcut background service pattern'ini takip et
>
> **Blazor:**
> - `Features/Shipping/ShipmentTrackingPage.razor` + `.razor.cs` — tum gonderilerin durumu (MudDataGrid), filtre: kargo firmasi, durum, tarih araligi. Renk kodlari ile durum gostergesi
> - `Features/Shipping/ShipmentDetailDialog.razor` + `.razor.cs` — tek gonderi detayi, durum gecmisi timeline (MudTimeline)
> - `Features/Shipping/CargoSummaryCards.razor` + `.razor.cs` — ozet kartlar: teslim edilen, yolda, sorunlu gonderi sayilari
> - NavMenu'ye "Kargo Takip" linki ekle
>
> **Testler:**
> - `Shipping/ShipmentTrackingManagerTests.cs` — en az 6 test: takip sorgulama (basarili, bulunamayan), durum guncelleme, gecmis sorgulama, ozet hesaplama, kargo firmasi secimi

### Beklenen Cikti
- 2 entity + 3 DTO
- EF config + migration
- 2 interface + 1 manager + 3 adapter + 1 background service
- 3 Blazor component + NavMenu guncelleme
- En az 6 unit test

---

## 10. Toplu Urun Guncelleme (Excel Import/Export)

**Oncelik:** P1
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Excel ve csv dosyaları ile toplu urun, fiyat ve stok guncelleme sistemi olustur. ClosedXML kutuphanesini kullan.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, MudBlazor, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut `ProductManager` (bkz: `Business/Concrete/ProductManager.cs`) urun CRUD islemlerini yonetir.
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `BulkOperations/BulkOperationLog.cs`: OperationType (enum: Import, Export), FileName, TotalRows, SuccessCount, ErrorCount, Status (enum: Processing, Completed, CompletedWithErrors, Failed), StartedAt, CompletedAt, StartedByUserId, ErrorDetails (JSON string)
> - DTO'lar: `BulkImportResultDto`, `BulkImportRowErrorDto`, `ExportFilterDto`
>
> **Business Katmani:**
> - `Abstract/IBulkOperationManager.cs`: ImportProducts(Stream excelStream), ImportPrices(Stream), ImportStock(Stream), ExportProducts(ExportFilterDto) -> byte[], ExportPrices(ExportFilterDto) -> byte[], ExportStock(ExportFilterDto) -> byte[], GetImportTemplate(string templateType) -> byte[], GetOperationLog(int logId), GetRecentOperations
> - `Concrete/BulkOperations/BulkOperationManager.cs` — pipeline pattern. Import akisi: (1) Excel oku, (2) her satiri validate et, (3) hatalari topla, (4) hatasiz satirlari uygula, (5) sonuc raporu olustur. Export akisi: DB'den cek, ClosedXML ile Excel olustur
> - `Concrete/BulkOperations/ExcelParser.cs` — Excel okuma/yazma utility'si (ClosedXML wrapper)
> - `Concrete/BulkOperations/ProductImportValidator.cs` — satir bazli validasyon (barkod format, fiyat >= 0, stok >= 0, zorunlu alan kontrolu)
> - NuGet: `ClosedXML` paketini `Entegrasyon.Business.csproj`'ye ekle
>
> **Blazor:**
> - `Features/BulkOperations/BulkOperationsPage.razor` + `.razor.cs` — ana sayfa: import/export sekmeleri, gecmis islemler listesi
> - `Features/BulkOperations/ImportDialog.razor` + `.razor.cs` — dosya yukleme (MudFileUpload), onizleme, hata gosterimi, onay ve uygula
> - `Features/BulkOperations/ImportResultDialog.razor` + `.razor.cs` — islem sonucu: basarili/basarisiz satir sayisi, hata detaylari (MudDataGrid)
> - `Features/BulkOperations/ExportDialog.razor` + `.razor.cs` — filtre secimi (kategori, marka, pazaryeri), dosya indirme
> - NavMenu guncelleme
>
> **Sablon Excel Dosyalari:**
> - `wwwroot/templates/product-import-template.xlsx`
> - `wwwroot/templates/price-import-template.xlsx`
> - `wwwroot/templates/stock-import-template.xlsx`
>
> **Testler:**
> - `BulkOperations/BulkOperationManagerTests.cs` — en az 8 test: basarili import, validation hatali satirlar, bos dosya, gecersiz format, fiyat import, stok import, export urun, export bos sonuc
> - `BulkOperations/ExcelParserTests.cs` — en az 4 test: okuma, yazma, bos satirlari atlama, buyuk dosya performansi

### Beklenen Cikti
- 1 entity + 3 DTO
- EF config + migration
- 1 interface + 1 manager + 1 parser + 1 validator
- 4 Blazor component + NavMenu guncelleme
- 3 Excel sablon dosyasi
- En az 12 unit test

---

## 11. ML Tabanli Talep Tahmini

**Oncelik:** P2
**Tahmini Karmasiklik:** Yuksek
**Bagimliliklar:** Raporlama Dashboard (2)

### Prompt

> **Gorev:** Gecmis satis verisine dayanarak ML tabanli talep tahmini modulu olustur. ML.NET kullan.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut `Sale` + `SaleItem` entity'leri gecmis satis verisini tutuyor. Mevcut `ReportManager` satis raporlama altyapisini sagliyor.
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Forecasting/DemandForecast.cs`: ProductVariantId (FK), ForecastDate, PredictedQuantity, ConfidenceLevel (decimal, 0-1), ActualQuantity (nullable — gerceklestikten sonra dolacak), ModelVersion, GeneratedAt
> - `Forecasting/ForecastModel.cs`: ModelName, ModelVersion, TrainedAt, TrainingDataStartDate, TrainingDataEndDate, Accuracy (RMSE), ModelFilePath, IsActive
> - DTO'lar: `DemandForecastDto`, `ForecastSummaryDto`, `StockoutRiskDto`, `PurchaseRecommendationDto`
>
> **Business Katmani:**
> - `Abstract/IDemandForecastManager.cs`: TrainModel, GenerateForecasts(productVariantIds, days), GetForecasts(filters), GetStockoutRisks(branchOfficeId), GetPurchaseRecommendations(branchOfficeId), EvaluateModelAccuracy
> - `Concrete/Forecasting/DemandForecastManager.cs` — pipeline pattern
> - `Concrete/Forecasting/SalesForecastingEngine.cs` — ML.NET SSA (Singular Spectrum Analysis) time series prediction. Egitim verisi: son 6-12 aylik gunluk satis adetleri. Cikti: gelecek 30-90 gun icin gunluk tahmin
> - `Concrete/Forecasting/ForecastDataPreparator.cs` — satis verisinden ML.NET input formatina donusum, eksik gunleri 0 ile doldurma, mevsimsellik (haftalik, aylik pattern)
> - NuGet: `Microsoft.ML`, `Microsoft.ML.TimeSeries` paketlerini ekle
>
> **Blazor:**
> - `Features/Forecasting/ForecastDashboard.razor` + `.razor.cs` — tahmin ozet kartlari: stok tukenmesi riski olan urunler, satin alma onerileri, model dogrulugu
> - `Features/Forecasting/ProductForecastChart.razor` + `.razor.cs` — urun bazli tahmin grafigi (MudChart: gecmis satis + tahmin cizgisi + guven araligi)
> - `Features/Forecasting/PurchaseRecommendations.razor` + `.razor.cs` — satin alma onerileri listesi (MudDataGrid: urun, mevcut stok, tahmini tuketim, onerilen Sipariş miktari)
> - NavMenu guncelleme
>
> **Testler:**
> - `Forecasting/DemandForecastManagerTests.cs` — en az 6 test: tahmin olusturma, bos veri durumu, model egitimi, stok tukenmesi risk hesabi, satin alma onerisi, model dogruluk degerlendirme
> - `Forecasting/ForecastDataPreparatorTests.cs` — en az 4 test: eksik gun doldurma, mevsimsellik tespiti, veri normalizasyonu, sinir degerleri

### Beklenen Cikti
- 2 entity + 4 DTO
- EF config + migration
- 1 interface + 1 manager + 1 ML engine + 1 data preparator
- 3 Blazor component
- En az 10 unit test

---

## 12. Workflow/Otomasyon Builder

**Oncelik:** P2
**Tahmini Karmasiklik:** Yuksek
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Kullanicilarin gorsel olarak is kuralları olusturabilecegi bir workflow/otomasyon builder sistemi olustur. "Tetikleyici -> Kosul -> Aksiyon" yapisi.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, MudBlazor, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut `EventChannel<T>` pattern'i event-driven iletisim icin kullaniliyor.
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Automation/WorkflowDefinition.cs`: Name, Description, IsActive, TriggerType (enum: StockBelowThreshold, NewOrder, PriceChange, ScheduledTime, ManualTrigger), TriggerConfig (JSON), CreatedByUserId, LastModifiedAt
> - `Automation/WorkflowStep.cs`: WorkflowDefinitionId (FK), StepOrder (int), StepType (enum: Condition, Action, Delay, Branch), Configuration (JSON — step tipine gore farkli schema), NextStepOnTrue (nullable FK self-reference), NextStepOnFalse (nullable FK self-reference)
> - `Automation/WorkflowExecution.cs`: WorkflowDefinitionId (FK), StartedAt, CompletedAt, Status (enum: Running, Completed, Failed, Cancelled), CurrentStepId, TriggerData (JSON), ErrorMessage
> - `Automation/WorkflowExecutionLog.cs`: WorkflowExecutionId (FK), StepId, ExecutedAt, Result (JSON), Status
> - DTO'lar: `WorkflowDefinitionDto`, `WorkflowStepDto`, `WorkflowExecutionDto`
>
> **Business Katmani:**
> - `Abstract/IWorkflowManager.cs`: CreateWorkflow, UpdateWorkflow, GetWorkflows, GetWorkflowDetail, ActivateWorkflow, DeactivateWorkflow, TriggerWorkflow, GetExecutionHistory
> - `Concrete/Automation/WorkflowManager.cs` — pipeline pattern
> - `Concrete/Automation/WorkflowEngine.cs` — step-by-step calistirma motoru. Her step tipini handle et:
>   - Condition: JSON config'den kosul evaluate et (stok < X, fiyat > Y)
>   - Action: bildirim gonder (mevcut INotificationManager), fiyat guncelle, stok uyarisi, log yaz
>   - Delay: bekleme (TimeSpan)
>   - Branch: kosul sonucuna gore farkli path
> - `Concrete/Automation/WorkflowTriggerListener.cs` — EventChannel'lari dinleyerek otomatik tetikleme
> - Validator'lar
>
> **Blazor:**
> - `Features/Automation/WorkflowListPage.razor` + `.razor.cs` — workflow listesi, aktif/pasif durumu, son calisma zamani
> - `Features/Automation/WorkflowDesigner.razor` + `.razor.cs` — gorsel workflow tasarimcisi (basit surukle-birak veya form-based step ekleme). Step'leri MudPaper kartlar olarak goster, oklar ile baglanti
> - `Features/Automation/WorkflowStepEditor.razor` + `.razor.cs` — step duzenleme dialog'u (tip secimi, config formu)
> - `Features/Automation/WorkflowExecutionHistory.razor` + `.razor.cs` — calisma gecmisi ve loglar (MudTimeline)
> - NavMenu guncelleme
>
> **Testler:**
> - `Automation/WorkflowManagerTests.cs` — en az 6 test: workflow olusturma, step ekleme, tetikleme, kosul degerlendirme, aksiyon calistirma, hata durumu
> - `Automation/WorkflowEngineTests.cs` — en az 6 test: condition step (true/false), action step (bildirim), delay step, branch step, zincirleme calisma, dongusel referans engelleme

### Beklenen Cikti
- 4 entity + 3 DTO
- EF config'ler + migration
- 1 interface + 1 manager + 1 engine + 1 trigger listener + validator'lar
- 4 Blazor component
- En az 12 unit test

---

## 13. Omnichannel POS (Fiziksel Magaza + Online Birlestirme)

**Oncelik:** P2
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Offline Satis (4), Depo Yonetimi (7)

### Prompt

> **Gorev:** Fiziksel magaza satislarini online satislarla ayni stok havuzunda yoneten bir omnichannel POS yapilandirmasi olustur. Mevcut `Sales.razor` sayfasini POS modu ile genislet.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, MudBlazor, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut satis altyapisi: `SaleManager.MakeSale()`, `OfficeStockManager.DecreaseStockAtomicAsync()`. Mevcut `BranchOffice` entity'si sube tanimlarini tutuyor.
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `POS/POSSession.cs`: BranchOfficeId, CashierId (UserId), OpenedAt, ClosedAt, OpeningCash (decimal), ClosingCash (decimal), Status (enum: Open, Closed, Suspended), TerminalId (string)
> - `POS/POSTransaction.cs`: POSSessionId (FK), SaleId (FK), PaymentMethod (enum: Cash, CreditCard, DebitCard, Mixed, MealCard), CashReceived, ChangeGiven, CardAuthCode, TransactionAt
> - `POS/CashMovement.cs`: POSSessionId (FK), MovementType (enum: CashIn, CashOut, FloatAdjustment), Amount, Reason, CreatedByUserId, CreatedAt
> - DTO'lar: `OpenSessionDto`, `CloseSessionDto`, `POSTransactionDto`, `POSSummaryDto`
>
> **Business Katmani:**
> - `Abstract/IPOSSessionManager.cs`: OpenSession, CloseSession, GetActiveSession(branchOfficeId), RecordTransaction, AddCashMovement, GetSessionSummary, GetDailySummary
> - `Concrete/POS/POSSessionManager.cs` — pipeline pattern. Oturum acma: kasa baslangic tutari, kasiyer bilgisi. Oturum kapama: sayim, fark hesaplama, rapor. Mevcut `ISaleManager.MakeSale()` ile entegre — POS satisi = normal satis + odeme kaydı
>
> **Blazor:**
> - `Features/POS/POSPage.razor` + `.razor.cs` — tam ekran POS arayuzu: barkod okutma alani, sepet, tutar, odeme secenekleri, hizli urun butonlari. Touch-friendly buyuk butonlar
> - `Features/POS/POSSessionDialog.razor` + `.razor.cs` — oturum acma/kapama dialog'u
> - `Features/POS/POSSummaryDialog.razor` + `.razor.cs` — gun sonu raporu (nakit, kart, toplam, fark)
> - `Features/POS/POSPaymentDialog.razor` + `.razor.cs` — odeme alim ekrani (nakit tutari, para ustu, kart secimi)
> - NavMenu guncelleme
>
> **Testler:**
> - `POS/POSSessionManagerTests.cs` — en az 8 test: oturum acma, oturum kapama (kasada fazla, kasada eksik, tam), islem kaydi, nakit hareketi, ozet hesaplama, cift oturum engelleme, odeme secenekleri, bos oturum kapama

### Beklenen Cikti
- 3 entity + 4 DTO
- EF config'ler + migration
- 1 interface + 1 manager + validator'lar
- 4 Blazor component
- En az 8 unit test

---

## 14. Dropship Otomasyonu

**Oncelik:** P2
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Tedarikci urunlerini kendi stogunuz gibi pazaryerlerinde satmanizi saglayan bir dropship otomasyon modulu olustur.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut `OrderManager` (bkz: `Business/Concrete/OrderManager.cs`) Sipariş islemlerini yonetir.
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Dropship/Supplier.cs`: Name, ContactName, Email, Phone, ApiUrl (nullable), ApiKey (nullable), LeadTimeDays (int — teslim suresi gun), IsActive, PaymentTerms (string)
> - `Dropship/SupplierProduct.cs`: SupplierId (FK), ProductVariantId (FK), SupplierSku, SupplierPrice (decimal — alis fiyati), SupplierStock (int), LastStockUpdate, IsActive
> - `Dropship/DropshipOrder.cs`: OrderId (FK — pazaryeri Siparişi), SupplierId (FK), SupplierOrderId (string — tedarikciye iletilen Sipariş numarasi), Status (enum: Pending, SentToSupplier, Confirmed, Shipped, Delivered, Cancelled), SupplierTrackingNumber, CreatedAt, SentAt, ConfirmedAt
> - DTO'lar: `SupplierDto`, `SupplierProductDto`, `DropshipOrderDto`, `DropshipSummaryDto`
>
> **Business Katmani:**
> - `Abstract/IDropshipManager.cs`: AddSupplier, GetSuppliers, AddSupplierProduct, GetSupplierProducts, CreateDropshipOrder(orderId), GetDropshipOrders, UpdateDropshipOrderStatus, SyncSupplierStock, GetDropshipSummary
> - `Concrete/Dropship/DropshipManager.cs` — pipeline pattern. Sipariş gelince: (1) urun dropship mi kontrol et, (2) tedarikciye Sipariş olustur, (3) durumu takip et
> - `BackgroundServices/DropshipStockSyncService.cs` — periyodik tedarikci stok guncelleme (API olan tedarikciler icin)
> - Validator'lar
>
> **Blazor:**
> - `Features/Dropship/SuppliersPage.razor` + `.razor.cs` — tedarikci listesi ve CRUD
> - `Features/Dropship/SupplierProductsPage.razor` + `.razor.cs` — tedarikci urun eslesmeleri (MudDataGrid)
> - `Features/Dropship/DropshipOrdersPage.razor` + `.razor.cs` — dropship Siparişleri ve durumlari
> - NavMenu guncelleme
>
> **Testler:**
> - `Dropship/DropshipManagerTests.cs` — en az 6 test: tedarikci ekleme, urun esleme, dropship Sipariş olusturma, durum guncelleme, stok senkronizasyonu, tedarikci urun fiyat degisikligi

### Beklenen Cikti
- 3 entity + 4 DTO
- EF config'ler + migration
- 1 interface + 1 manager + 1 background service + validator'lar
- 3 Blazor component + NavMenu guncelleme
- En az 6 unit test

---

## 15. Coklu Dil Destegi (i18n)

**Oncelik:** P2
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Uygulamaya coklu dil destegi (i18n) ekle. Baslangicta Turkce (varsayilan) ve Ingilizce destegi olacak. Ileride kolayca yeni dil eklenebilmeli.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, MudBlazor, code-behind zorunlu. Mevcut tum UI metinleri Turkce olarak hardcode edilmis durumda.
>
> **Ne Yapilacak:**
>
> **Altyapi:**
> - .NET 8'in yerlesik `IStringLocalizer<T>` mekanizmasini kullan (3rd party kutuphane gerek yok)
> - `Resources/` klasoru olustur: `Resources/Pages/`, `Resources/Shared/`, `Resources/Components/`
> - Her Blazor component icin `.resx` dosyasi ciftleri: `ComponentName.tr.resx` (Turkce), `ComponentName.en.resx` (Ingilizce)
> - `Program.cs`'de localization middleware ve supported cultures ayarla
> - Kullanici dil tercihi: `ApplicationUser` entity'sine `PreferredLanguage` (string, default "tr") alani ekle
> - Dil degistirme: cookie-based culture switching (Blazor Server icin standart pattern)
>
> **Oncelikli Cevirilecek Sayfalar (ilk fazda):**
> - NavMenu (tum menu isimleri)
> - Dashboard (kart basliklari, etiketler)
> - Login sayfasi
> - Ortak bilesenler (buton metinleri: "Kaydet", "Iptal", "Sil", "Duzenle" vb.)
> - Validation hata mesajlari
>
> **Blazor:**
> - `Components/Shared/LanguageSwitcher.razor` + `.razor.cs` — navbar'a dil secim dropdown'u ekle (bayrak ikonu + dil adi)
> - NavMenu.razor guncelle — tum hardcode Turkce metinleri `@Localizer["MenuItemName"]` ile degistir
>
> **Business Katmani:**
> - FluentValidation hata mesajlari icin `WithMessage()` cagrilarinda localization kullan
> - `IApplicationLogManager` log mesajlari Turkce kalacak (bu admin icin, cevirilmeyecek)
>
> **Testler:**
> - `Localization/LocalizationTests.cs` — en az 4 test: Turkce varsayilan, Ingilizce gecis, eksik key fallback, kullanici tercihi kaydetme

### Beklenen Cikti
- ApplicationUser entity'sine alan ekleme + migration
- Program.cs localization konfigurasyonu
- Resources/ altinda .resx dosyalari (en az 5 component icin tr + en)
- 1 Blazor component (LanguageSwitcher)
- NavMenu + Dashboard + Login guncellemeleri
- En az 4 unit test

---

## 16. Musteri Sadakat Programi

**Oncelik:** P3
**Tahmini Karmasiklik:** Orta
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Musteri sadakat programi modulu olustur. Alisverislerde puan kazanma, puan harcama ve musteri segmentasyonu ozellikleri ekle.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu. Mevcut `Customer` entity'si (RetailCustomer/CorporateCustomer kalitimi) var. Mevcut `SaleManager.MakeSale()` satis islemini yonetir.
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `Loyalty/LoyaltyAccount.cs`: CustomerId (FK, unique), TotalPoints, LifetimePoints, Tier (enum: Bronze, Silver, Gold, Platinum), TierUpdatedAt, IsActive
> - `Loyalty/PointTransaction.cs`: LoyaltyAccountId (FK), Points (int — pozitif: kazanim, negatif: harcama), TransactionType (enum: Purchase, Redemption, BonusPoints, Expiration, ManualAdjustment), SaleId (nullable FK), Description, CreatedAt, ExpiresAt (nullable)
> - `Loyalty/LoyaltyTierRule.cs`: Tier, MinLifetimePoints, PointMultiplier (decimal — Gold = 1.5x puan), DiscountPercent, FreeShipping (bool)
> - DTO'lar: `LoyaltyAccountDto`, `PointTransactionDto`, `LoyaltyTierRuleDto`, `RedeemPointsDto`
>
> **Business Katmani:**
> - `Abstract/ILoyaltyManager.cs`: GetAccount(customerId), EarnPoints(customerId, saleId, amount), RedeemPoints(customerId, points, saleId), GetPointHistory(customerId), UpdateTier(customerId), GetTierRules, SetTierRule, CalculatePointsForPurchase(amount, tier)
> - `Concrete/Loyalty/LoyaltyManager.cs` — pipeline pattern. Puan kazanim: satis tutari * puan orani * tier multiplier. Puan harcama: yeterlilik kontrolu, mevcut satistan dusme. Tier guncelleme: lifetime points'e gore otomatik
> - `SaleManager.MakeSale()` icerisine puan kazanim entegrasyonu ekle (satis tamamlandiktan sonra ILoyaltyManager.EarnPoints cagir)
> - Validator'lar
>
> **Blazor:**
> - `Features/Loyalty/LoyaltyDashboard.razor` + `.razor.cs` — sadakat programi ozeti: toplam uye, tier dagilimi, toplam kazanilan/harcanan puan
> - `Features/Loyalty/CustomerLoyaltyDetail.razor` + `.razor.cs` — musteri bazli sadakat detayi (puan gecmisi, tier bilgisi). Mevcut CustomerDialog icerisine tab olarak eklenebilir
> - `Features/Loyalty/LoyaltySettings.razor` + `.razor.cs` — tier kurallari yonetimi (puan esikleri, carpanlar, avantajlar)
> - NavMenu guncelleme
>
> **Testler:**
> - `Loyalty/LoyaltyManagerTests.cs` — en az 8 test: puan kazanma, puan harcama (yeterli/yetersiz), tier yukseltme, tier multiplier, puan suresi dolma, hesap olusturma, satis entegrasyonu, manuel puan duzeltme

### Beklenen Cikti
- 3 entity + 4 DTO
- EF config'ler + migration
- 1 interface + 1 manager + validator'lar
- 3 Blazor component + NavMenu guncelleme
- SaleManager guncelleme (puan entegrasyonu)
- En az 8 unit test

---

## 17. Sosyal Medya Entegrasyonu

**Oncelik:** P3
**Tahmini Karmasiklik:** Dusuk
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Urun bilgilerini sosyal medya platformlarina (Instagram, Facebook, X/Twitter) otomatik paylasim yapabilen bir entegrasyon modulu olustur.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu, multi-tenant uyumlu (sosyal medya token'lari tenant bazli `ConcurrentDictionary<int, T>` ile cache'lenmeli).
>
> **Ne Yapilacak:**
>
> **Entity Katmani:**
> - `SocialMedia/SocialMediaAccount.cs`: Platform (enum: Instagram, Facebook, X, TikTok), AccountName, AccessToken (encrypted), RefreshToken, TokenExpiresAt, IsActive, LastPostAt
> - `SocialMedia/SocialMediaPost.cs`: SocialMediaAccountId (FK), ProductId (nullable FK), PostType (enum: NewProduct, PriceDropm, Campaign, Custom), Content, ImageUrls (JSON), Status (enum: Draft, Scheduled, Posted, Failed), ScheduledAt, PostedAt, ExternalPostId, ErrorMessage
> - DTO'lar: `SocialMediaAccountDto`, `CreatePostDto`, `PostScheduleDto`
>
> **Business Katmani:**
> - `Abstract/ISocialMediaManager.cs`: AddAccount, GetAccounts, CreatePost, SchedulePost, GetPosts, GetPostStatus, GenerateProductPost(productId) -> otomatik icerik olusturma (urun adi, fiyat, gorsel, link)
> - `Concrete/SocialMedia/SocialMediaManager.cs` — pipeline pattern
> - `Abstract/ISocialMediaPlatformClient.cs`: PublishPost, DeletePost, GetPostStatus
> - `Concrete/SocialMedia/MockSocialMediaClient.cs` — test icin mock
> - `BackgroundServices/SocialMediaSchedulerService.cs` — zamanlanmis paylasimlar (ScheduledAt'e gore)
>
> **Blazor:**
> - `Features/SocialMedia/SocialMediaPage.razor` + `.razor.cs` — hesap yonetimi, paylasim gecmisi, yeni paylasim
> - `Features/SocialMedia/CreatePostDialog.razor` + `.razor.cs` — paylasim olusturma (urun secimi, icerik duzenleme, zamanlama)
> - NavMenu guncelleme
>
> **Testler:**
> - `SocialMedia/SocialMediaManagerTests.cs` — en az 5 test: hesap ekleme, paylasim olusturma, zamanlama, urun icerigi otomatik olusturma, token suresi dolma

### Beklenen Cikti
- 2 entity + 3 DTO
- EF config'ler + migration
- 2 interface + 1 manager + 1 mock client + 1 background service
- 2 Blazor component + NavMenu guncelleme
- En az 5 unit test

---

## 18. Gelismis SEO Araclari

**Oncelik:** P3
**Tahmini Karmasiklik:** Dusuk
**Bagimliliklar:** Yok

### Prompt

> **Gorev:** Pazaryeri listelemeleri icin SEO optimizasyonu araclari ekle. Urun basligi, aciklamasi ve anahtar kelimeleri icin kalite skoru ve iyilestirme onerileri sun.
>
> **Proje Mimarisi:** .NET 8 Blazor Server, EF Core + PostgreSQL, Primary constructor DI, 3 adimli pipeline, code-behind zorunlu. Mevcut `Product` entity'sinde Name ve Description alanlari var. Mevcut `ProductVariant` ve `ProductMarketplace` entity'leri pazaryeri listeleme bilgilerini iceriyor.
>
> **Ne Yapilacak:**
>
> **Business Katmani (hicbir entity gerekmez — analiz realtime yapilacak):**
> - `Abstract/ISeoAnalyzer.cs`: AnalyzeProductTitle(string title, string marketplace) -> `SeoScoreDto`, AnalyzeProductDescription(string description) -> `SeoScoreDto`, GetSuggestions(productId) -> `SeoSuggestionDto[]`, CalculateOverallScore(productId) -> int (0-100)
> - `Concrete/Seo/SeoAnalyzer.cs` — kural tabanli analiz (exception firlatma, bu utility sinifi): baslik uzunlugu (50-120 karakter optimal), anahtar kelime yogunlugu, yasak kelimeler (Trendyol kuralları: "en iyi", "garanti" vb.), aciklama uzunlugu, HTML tag kullanimi, buyuk harf orani
> - DTO'lar: `SeoScoreDto` (Score, Issues[], Suggestions[]), `SeoSuggestionDto` (Field, CurrentValue, SuggestedValue, Reason, ImpactLevel)
>
> **Blazor:**
> - `Features/Products/ProductSeoPanel.razor` + `.razor.cs` — urun duzenleme sayfasina (ProductEdit) tab veya yan panel olarak ekle. Realtime SEO skoru gostergesi (daire seklinde, renk kodlu), sorunlar ve oneriler listesi
> - Mevcut `Features/Products/ProductEdit.razor` guncelle — SeoPanel'i entegre et
>
> **Testler:**
> - `Seo/SeoAnalyzerTests.cs` — en az 6 test: kisa baslik, uzun baslik, optimal baslik, yasak kelime tespiti, aciklama analizi, bos input

### Beklenen Cikti
- 2 DTO (entity yok)
- 1 interface + 1 analyzer
- 1 Blazor component + ProductEdit guncelleme
- En az 6 unit test

---

## Ozet Tablosu

| # | Ozellik | Oncelik | Karmasiklik | Tahmini Test Sayisi |
|---|---------|---------|-------------|---------------------|
| 1 | E-Fatura Entegrasyonu | P0 | Yuksek | 12+ |
| 2 | Raporlama Dashboard | P0 | Orta | 10+ |
| 3 | Komisyon Hesaplama | P0 | Orta | 8+ |
| 4 | Offline Satis (Agent) | P0 | Yuksek | 10+ |
| 5 | Rekabet Analizi | P1 | Yuksek | 11+ |
| 6 | Akilli Fiyatlandirma | P1 | Orta | 14+ |
| 7 | Depo Yonetimi (WMS) | P1 | Yuksek | 10+ |
| 8 | Barkod Okuyucu | P1 | Dusuk | 4+ |
| 9 | Kargo Takip | P1 | Orta | 6+ |
| 10 | Toplu Urun Guncelleme | P1 | Orta | 12+ |
| 11 | ML Talep Tahmini | P2 | Yuksek | 10+ |
| 12 | Workflow Builder | P2 | Yuksek | 12+ |
| 13 | Omnichannel POS | P2 | Orta | 8+ |
| 14 | Dropship Otomasyonu | P2 | Orta | 6+ |
| 15 | Coklu Dil (i18n) | P2 | Orta | 4+ |
| 16 | Musteri Sadakat | P3 | Orta | 8+ |
| 17 | Sosyal Medya | P3 | Dusuk | 5+ |
| 18 | SEO Araclari | P3 | Dusuk | 6+ |
| | **TOPLAM** | | | **154+** |

---

## Genel Kurallar (Tum Promptlar Icin Gecerli)

Her prompt Claude Opus 4.6'ya verilirken asagidaki hatirlaticlari icerir:

1. **TDD-First:** Once test yaz, sonra implement et. Test olmadan ozellik tamamlanmis SAYILMAZ.
2. **3 Adimli Pipeline:** Tum business metodlarinda: (1) FluentValidation, (2) LogicRunner is kurallari, (3) Execution.
3. **Code-Behind:** Her `.razor` dosyasinin `.razor.cs` kardes dosyasi olmali.
4. **Multi-Tenant:** Singleton cache'lerde `ConcurrentDictionary<int, T>`, tenant bazli SemaphoreSlim, DB sorgularinda tenant filtresi.
5. **Primary Constructor DI:** `public class XxxManager(IDbContextFactory<IntegrationDbContext> contextFactory) : IXxxManager`
6. **Feature-Based Klasor:** `Features/Xxx/` altina — tek kullanimlik bile olsa.
7. **Turkce Aciklama, Ingilizce Kod:** Degisken/method/class isimleri Ingilizce, yorum/log mesajlari Turkce.
8. **DI Kayit:** Yeni servisler `ApplicationDependencyExtension.cs`'ye eklenmeli.
9. **NavMenu:** Yeni sayfalar `Components/Shared/NavMenu.razor`'a eklenmeli.
10. **No Task.WhenAll with DbContext:** Ayni scoped DbContext'i paylasan servis cagrilarini asla paralel calistirma.
