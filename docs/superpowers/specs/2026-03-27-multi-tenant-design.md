# Multi-Tenant Architecture Design Spec

## Problem

Entegrasyon platformu single-tenant calisiyorve SaaS olarak satisa acilamaz durumda. Her musteri (tenant) icin:
- Veri izolasyonu gerekli (KVKK/GDPR)
- Ozelliklerin paket halinde satilmasi isteniyor (Starter/Pro/Enterprise)
- Subdomain uzerinden tenant tanimlanmasi planlaniyorlar
- Background service'ler (28+) tenant-aware calismaliklar
- Opsiyonel storefront (custom domain) tenant bazinda saglanmali

## Kararlar

| Karar | Secim | Neden |
|---|---|---|
| DB izolasyonu | Database-per-tenant | Tam veri izolasyonu, kolay backup/restore, KVKK uyumu |
| Feature toggling | Permission-based paketler | Mevcut 48+ permission altyapisini genisletir, tek sistem |
| BG service | Tenant iterator pattern | Basit, anlasilir, feature kontrolu dogal |
| Blazor tenant | Subdomain-based, tek instance | Kaynak verimli, kolay guncelleme |
| DB provision | AdminPanel otomatik | Tek tikla: CREATE DB + migration + seed |
| Storefront domain | Custom domain | Mevcut StorefrontDomainMapping yapisini kullanir |
| Gecis sirasi | Core-first | Faz 1: Core → Faz 2: Packages → Faz 3: BG Services → Faz 4: Storefront |

## Mimari Ozet

### Faz 1 — Tenant Core Infrastructure
- `ITenantContext` genisletme (ConnectionString, IsInitialized, Initialize)
- `HttpTenantContext` (scoped, middleware ile initialize)
- `ITenantRegistry` + `TenantRegistryService` (singleton, AdminPanel SQLite'dan cache)
- `TenantDbContextFactory` (scoped, `Func<string>` pattern ile dongusel referans onlenir)
- `BlazorTenantResolutionMiddleware` (subdomain → tenant cozumleme)
- `TenantCircuitHandler` (Blazor circuit reconnect korumasi)
- `TenantProvisioningService` (AdminPanel'de DB olusturma + migration)
- `UserSession.TenantId` + claims'e ekleme

### Faz 2 — Permission-Based Feature Packages
- `FeaturePackage`, `FeaturePackagePermission`, `TenantSubscription` (AdminPanel DB)
- `IFeatureService` (tenant paket kontrolu)
- `TenantFeatureAuthorizationHandler` (iki katmanli yetkilendirme)
- `FeatureGate` Blazor component
- Predefined paketler: Starter, Pro, Enterprise

### Faz 3 — Tenant-Aware Background Services
- `BaseEvent.TenantId` ekleme
- `TenantAwarePollingService` abstract base class
- 28 servisin tenant-aware'e migrasyonu
- Event-driven servislerde tenant scope yonetimi

### Faz 4 — Storefront Multi-Tenant
- `TenantStorefrontDomain` (AdminPanel DB)
- `StorefrontTenantResolver` guncelleme
- Storefront DbContext tenant-aware
- Enterprise paket feature gate

## Cross-Cutting

- **Cache izolasyonu:** `t:{tenantId}:` key prefix
- **Logging:** Serilog `TenantId` property
- **SignalR:** `(TenantId, UserId)` subscriber key
- **Migration:** AdminPanel "Migrate All" endpoint

## Riskler

- Blazor circuit reconnect: TenantId claims + CircuitHandler ile korunur
- Connection pool exhaustion: MaxPoolSize=20 per tenant
- Background service thundering herd: Randomized tenant delay
- AdminPanel SQLite concurrency: WAL mode + 5dk memory cache
