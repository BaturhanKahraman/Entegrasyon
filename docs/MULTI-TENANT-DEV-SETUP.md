# Multi-Tenant Local Development Setup

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
