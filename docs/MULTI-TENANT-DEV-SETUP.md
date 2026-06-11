# Multi-Tenant Local Development Setup

> ⚠️ **Bazı bölümler bayat** (Blazor/SQLite dönemi). Güncel mimari: ASP.NET Core MVC + PostgreSQL `AdminPanelDb`. Tenant çözümleme mantığı geçerli ama uygulama/DB adları değişti.

## ⚠️ Yaygın Tuzak — AdminPanelDb_Dev & Tenant Connection String (2026-06-11)

**Belirti:** Dev (8085) her **authenticated** istekte 500 / `TimeoutException: Failed to connect to 192.168.1.78:5432` (`SecurityStampValidator` → cookie doğrulama). Anonim sayfalar (login) çalışır.

**Kök neden:** Dev MVC container'ı tenant'ı `AdminPanelDb_Dev.Tenants` tablosundan çözer. `IDbContextFactory<IntegrationDbContext>` = `TenantDbContextFactory` → `tenantContext.ConnectionString`. Bu satırdaki connection string `Host=192.168.1.78` ise **container içinden host loopback'ine (127.0.0.1-bound postgres) ulaşılamaz** → timeout. Doğrusu `Host=postgres_db` (docker servis adı) olmalı.

**Nasıl bulaşır:** `AdminPanelDb` → `AdminPanelDb_Dev` veri kopyalarken (master katalog doldurma) operasyonel `Tenants` tablosu da kopyalanır; `SeedData.cs` onu `Host=192.168.1.78;Database=IntegrationDb` ile seed'lemiştir. `Tenants` boşken middleware `ConnectionStrings:Main` (postgres_db) fallback'ine gider — o yüzden kopyadan önce çalışır.

**Önleme — master katalog doldururken SADECE master tablolarını kopyala:**
```bash
# pg_dump'a sadece master + paket tablolarını ver; operasyonel Tenants/AdminUsers/TenantSubscriptions'ı DIŞARIDA bırak
docker exec postgres_db sh -c 'pg_dump -U baturhan -d AdminPanelDb --data-only --disable-triggers \
  -t "\"Master*\"" -t "\"SectorPackage*\"" -t "\"MarketplaceReferences\"" \
  | psql -U baturhan -d AdminPanelDb_Dev -v ON_ERROR_STOP=1'
```

**Bulaştıysa düzeltme (dev tenant'ı container-içi host'a çevir + app restart):**
```sql
UPDATE "Tenants" SET "ConnectionString" =
  replace(replace("ConnectionString",'Host=192.168.1.78','Host=postgres_db'),
          'Database=IntegrationDb;','Database=IntegrationDb_Dev;')
WHERE "Id" = 1;   -- stage tenant (Id=2): yalnız Host=postgres_db swap
```
```bash
docker restart entegrasyon-mvc-dev
```

**Sistemik açık iş (DevOps):** dev/stage/prod fresh kurulumda `AdminPanelDb_*` seed'i pipeline'da YOK (yalnız `IntegrationDb_*` seed'leniyor). Kalıcı çözüm: ya AdminPanel'i stack olarak ekleyip startup seed'ini çalıştır, ya da yukarıdaki master-only dump'ı bir deploy adımı yap.

## Prerequisites

- Docker running (PostgreSQL container)
- AdminPanel DB (`adminpanel.db`) must exist with at least one tenant
- Development tenant is auto-seeded on first AdminPanel startup

## Quick Start

### 1. Start AdminPanel (seeds dev tenant)

```bash
cd Application/Entegrasyon.AdminPanel && dotnet run
```

This automatically creates a "dev" tenant with:
- Subdomain: `dev`
- Pro package subscription (never expires)
- ConnectionString from `TemplateDb` appsettings

### 2. Blazor App

The Blazor app auto-falls back to the "dev" tenant on localhost in Development mode.
Just run:

```bash
cd Application/Entegrasyon.Blazor && dotnet run
```

No subdomain configuration needed -- `localhost` is automatically mapped to `dev`.

### 3. Subdomain Testing (optional)

To test with real subdomains, add entries to `/etc/hosts`:

```
127.0.0.1 dev.app.entegrasyon.local
127.0.0.1 tenant2.app.entegrasyon.local
```

Then configure Kestrel or a reverse proxy to handle these hostnames.

### 4. Storefront

The Storefront resolves tenants by full hostname (custom domain mapping).
For local development, add a `StorefrontDomainMapping` for localhost in the dev tenant DB:

```sql
INSERT INTO "StorefrontDomainMappings" ("TenantId", "DomainName", "IsPrimary", "IsActive")
VALUES (1, 'localhost', true, true);
```

## Configuration

### Blazor `appsettings.Development.json`

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:Main` | Fallback DB connection (also used by dev tenant) |
| `ConnectionStrings:Redis` | Redis cache |
| `ConnectionStrings:AdminPanel` | Path to AdminPanel SQLite DB (read-only) |

### AdminPanel `appsettings.json`

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:AdminPanel` | SQLite DB path |
| `ConnectionStrings:TemplateDb` | Template PostgreSQL DB (used as dev tenant connection string) |

## How It Works

1. **AdminPanel seed:** On startup, `SeedData.Initialize` creates a `dev` tenant if it does not exist, using the `TemplateDb` connection string.
2. **Blazor middleware:** `BlazorTenantResolutionMiddleware` extracts subdomains from the host. In Development mode, if no subdomain is found (e.g., `localhost`), it falls back to `"dev"`.
3. **Production:** No fallback. Requests without a valid subdomain receive a 404.
