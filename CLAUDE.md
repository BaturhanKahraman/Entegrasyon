# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build Entegrasyon.sln

# Run MVC app (port 5100)
cd Application/Entegrasyon.MVC && dotnet run

# Run tests
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj

# Run a single test
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~TestClassName"

# Run integration tests (Testcontainers ile PostgreSQL otomatik ayağa kalkar — Docker çalışıyor olmalı)
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj

# Run a single integration test
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~TestClassName"

# Run E2E tests (uygulama debug modda ayakta olmalı — ayrı docker-compose gerekmez)
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj

# Run a single E2E test
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --filter "FullyQualifiedName~TestClassName"

# Apply EF Core migrations
dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC

# Add a new migration
dotnet ef migrations add <MigrationName> -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC
```

**Test kullanıcısı (dev):** admin / 123456789

**Infrastructure (docker-compose):** PostgreSQL (5432)

**Integration Testleri:** Testcontainers ile geçici PostgreSQL container'ı otomatik oluşturulur — Docker daemon çalışıyor olmalı. Respawn ile her test sonrası DB temizlenir (seed tablolar korunur). `WebApplicationFactory` üzerinden gerçek DI + EF Core + migration testi yapılır.

**E2E Testleri:** Uygulama genelde debug modda ayaktadır. E2E testleri doğrudan `dotnet test` ile çalıştırılabilir — ayrı bir docker-compose ortamı başlatmaya gerek yoktur. Testler varsayılan olarak `http://localhost:5099` adresine bağlanır (`E2E_BASE_URL` env var ile değiştirilebilir).

## Architecture

Klasik katmanlı mimari — Entity → DataAccess → Business → MVC:

| Proje | Görev |
|---|---|
| `Entegrasyon.Entity` | Domain modelleri ve DTOlar |
| `Entegrasyon.DataAccess` | EF Core context, migrations, entity configurations |
| `Entegrasyon.Business` | Business managers (Abstract + Concrete) |
| `Entegrasyon.ApplicationBootstrap` | DI container kurulumu |
| `Entegrasyon.MVC` | ASP.NET Core MVC + HTMX + Tabler UI (port 5100) |

### MVC Projesi Yapısı (Entegrasyon.MVC)

**Tech Stack:** ASP.NET Core 8 MVC + HTMX + Tabler UI + Cookie Auth
**Klasör yapısı:** Feature Folders (`Features/{FeatureName}/Controller + ViewModels + Views`)
**Frontend:** Vanilla JS, npm/bundler/TypeScript yok

**Frontend Kütüphaneleri (wwwroot/lib/):** Tom Select (aranabilir dropdown), Flatpickr (tarih secici, TR locale), IMask (input maskeleme), Notyf (toast), SortableJS (surukle-birak), GLightbox (gorsel lightbox)

**Tabler UI Referans:** https://tabler.io/docs/getting-started — Tabler'ın tüm bileşenleri (card, alert, badge, avatar, progress, ribbon, steps, timeline, datagrid, placeholder, empty, status-dot, dropdown, modal, offcanvas, accordion, tabs) kullanılabilir. Yeni UI geliştirirken önce Tabler'ın hazır bileşenlerini kontrol et.

**Tabler Component Kullanımı (Strict Rule):** Herhangi bir Tabler bileşeni (badge, card, alert, ribbon, status, button vb.) kullanılmadan ÖNCE o bileşenin resmi dokümantasyonuna (https://tabler.io/docs/ui/<component>) bakılmalıdır. Class isimleri tahmin edilmemeli; doğru kombinasyon doğrulanmalı. Örn. `badge bg-green` tek başına kullanılmaz — Tabler solid renkli badge için `badge bg-green text-green-fg` ya da light varyant için `badge bg-green-lt` ister. CSS'te global override eklemek yerine Tabler'ın önerdiği class kombinasyonunu kullan.

MVC-specific pattern'ler için bkz: `~/.claude/skills/aspnet-mvc-htmx/` skill dosyaları.

**Temel MVC pattern'leri:**
- **PRG:** POST → TempData.SetSuccess → RedirectToAction → GET (geri tuşu güvenli)
- **HTMX:** `Request.IsHtmx()` → PartialView, else → View (aynı endpoint, iki davranış)
- **Filters:** TenantActionFilter (claims→tenant), AutoValidationFilter (ModelState otomatik)
- **ViewData extensions:** `SetPageTitle()`, `SetActiveNav()`, `SetBreadcrumb()` (tip güvenli)
- **TempData extensions:** `SetSuccess()`, `SetError()`, `GetToast()` (JSON serialize)
- **Error handling:** IExceptionHandler zinciri (BusinessRule → HTMX → ProblemDetails)
- **Tag Helpers:** `<form-group asp-for>`, `require-role="Admin"`, `nav-active="products"`

## Key Patterns

**Primary Constructor DI** (C# 12):
```csharp
public class ProductManager(IntegrationDbContext dbContext) : IProductService
```
**Business Layer Method Structure (Strict Rule):**
Tüm Business Manager (`XxxManager.cs`) metodları KESİNLİKLE aşağıdaki 3 adımlı "Pipeline" akışını takip etmelidir. Asla bu sırayı bozma:

1. **Validation:** İşleme başlamadan önce `FluentValidation` ile DTO/objeyi doğrula. Başarısızsa, validasyon hatalarını içeren bir sonuç (Result) dön.
2. **Business Rules:** Validasyon geçerse, iş kurallarını (örn: stok kontrolü, benzersiz isim kontrolü vb.) `LogicRunner` ile kontrol et. Başarısızsa iş kuralı hatalarını dön.
3. **Execution:** Sadece üstteki iki adım başarılı olursa asıl işleme geç.


**Manager/Interface pattern:** Her business servisi için `Abstract/IXxxManager.cs` + `Concrete/XxxManager.cs`. Tümü `ApplicationDependencyExtension.cs` üzerinden DI'a kaydedilir.

**EF Core:** Default olarak no-tracking. `SaveChangesAsync()` otomatik UTC dönüşümü yapar. Tüm entity'ler `BaseEntity`'den türer (`IsDeleted`, `DeletedAt`, `CreatedAt`, `UpdatedAt`).

**EF Core Migration (Strict Rule):** Entity veya DbContext'te değişiklik yapıldığında KESİNLİKLE migration oluşturulmalı ve dev DB'ye uygulanmalıdır. Adımlar:
1. `dotnet ef migrations add <MigrationName> -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`
2. Oluşan migration dosyasını gözden geçir (gereksiz/duplicate değişiklik var mı?)
3. `dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.MVC --context IntegrationDbContext`
4. `dotnet ef migrations has-pending-model-changes ...` ile model-snapshot senkronizasyonunu doğrula
Migration olmadan entity değişikliği TAMAMLANMIŞ SAYILMAZ.

**Mapperly:** DTO mapping için kullanılır (Mapster kaldırıldı). Source-generator tabanlı, compile-time. Mapper'lar `Business/Mappers/` altında `[Mapper]` partial class'lar.

**Logging (İki Katmanlı — Strict Rule):**
İki farklı log sistemi var, ASLA karıştırılmamalı:
1. **`IApplicationLogManager.AddLog()`** → Kullanıcı-facing loglar. Admin dashboard'dan görünür. Türkçe, temiz mesajlar. Her business işleminde (CRUD, sync, matching vb.) başında ve sonunda çağrılmalı. `LogType` enum ile kategorize. `ProductActivityLog` ürün bazlı timeline. **ÖNEMLİ:** Bu loglar şu an senkron DB yazımı yapıyor — iş transaction'ı içinde. İleride async queue'ya geçirilecek (Channel<T> pattern).
2. **`ILogger<T>`** → Developer-facing loglar. Serilog/console/OpenTelemetry. Exception detayları, stack trace, debug bilgileri. Kullanıcı GÖRMEZ.

Business manager'larda HER İKİSİ DE kullanılmalı:
```csharp
await applicationLogManager.AddLog("Ürün ekleniyor.", LogType.Product, LogAction.Add);
logger.LogInformation("Adding product {ProductId}", productId);
```

**Event Channel pattern:** Background servisler arası iletişim için:
```csharp
EventChannel<CategoryUpdatedEvent> // publisher → subscriber
```

## Domain Özeti

**Kategori & Özellik modeli:**
- `CategoryAttribute` — özellik tanımı (Key, Humanized, AllowCustom, Values)
- `CategoryAttributeCategory` — junction tablosu (IsRequired, IsVarianter, IsSlicer)
- `CategoryAttributeValue` — önceden tanımlı değerler
- `AttributeKeyValue` — ürün ↔ özellik değeri bağlantısı
- MarketPlace eşleştirmeleri: `CategoryAttributeMarketPlaceMatch`, `CategoryAttributeValueMarketPlaceMatch` (MarketPlaceId=1 = Trendyol)

**Müşteri kalıtımı:** `Customer` → `RetailCustomer` / `CorporateCustomer`

**Ürün barkod:** `BarcodeSequence` tablosu, arka planda temizlenir.

## DI Kayıt Yapısı

`ApplicationDependencyExtension.cs` içindeki extension methodlar:
- `AddApplicationDependencies()` — tüm manager'lar, event channel'lar, validator'lar, Mapster
- `AddCustomDbContext()` — PostgreSQL, no-tracking
- `AddBackgroundServices()` — Trendyol import, product publish
- `AddStorageServices()` — MinIO + ImageSharp
- `AddSignalRSettings()` + `AddNotification()` — real-time bildirimler

## Development Workflow (Strict Rule)

  **TDD-First:** Her yeni özellik ve bug fix için KESİNLİKLE şu sıra izlenir:
  1. Önce testi yaz (RED)
  2. Testi çalıştır, başarısız olduğunu doğrula
  3. Minimum kodu implement et (GREEN)
  4. Testi çalıştır, geçtiğini doğrula
  5. Refactor et (gerekiyorsa)
  6. Tüm testleri çalıştır: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
  7. Entegrasyon testini çalıştır: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
  8. E2E testini çalıştır: `dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj`

  Test olmadan özellik tamamlanmış SAYILMAZ. "Testleri sonra yazarız" KABUL EDİLMEZ.

## Multi-Tenant Design (Strict Rule)

  Bu sistem ileride **multi-tenant** yapılacak. Tüm yeni geliştirmelerde tenant izolasyonunu göz önünde bulundur:

  - **Singleton servislerde in-memory state:** Tek bir field yerine `ConcurrentDictionary<int, T>` kullan (key = tenantId veya MarketPlace.Id). Özellikle OAuth token cache'leri bu kurala TABİ.
  - **SemaphoreSlim:** Tenant başına izole lock mekanizması kullan, global tek lock değil.
  - **DB query'leri:** Tüm sorgularda tenant filtresi uygulanabilir olmalı.
  - **Configuration:** Tenant-specific config'ler DB'den okunmalı, appsettings.json'a hardcode edilmemeli.
  - **Tek tenant için çalışıyor ≠ multi-tenant'ta çalışacak.** Tasarımda her zaman "bu N tenant ile çalışır mı?" sorusunu sor.

## Local Ollama API (Sub-Agent)

Makinede Ollama çalışıyor (`http://localhost:11434`). GPU: RTX 4080 Laptop 12 GB VRAM.

**Kullanılabilir model:** `entegrasyon-coder` — proje-özel system prompt gömülü, `qwen2.5-coder:14b` tabanlı. MCP tool olarak `mcp__ollama__ollama_chat` ile erişilebilir.

**Uygun görevler:** Bulk pattern-based fix, boilerplate üretimi, basit kod analizi, çok sayıda dosyada aynı değişiklik.

**Uygun OLMAYAN görevler:** Mimari kararlar, karmaşık reasoning, büyük context gerektiren işler, production-critical kod.

**Strateji — Üret + Kontrol Et:**
1. Ollama'ya mekanik görevi ver (örn: "bu dosyadaki null'ları null! yap")
2. Çıktıyı kabataslak kontrol et (build, grep, basit doğrulama)
3. Sorun varsa düzelt, yoksa uygula

Bu sayede Claude token'ı tekrarlı işlere harcanmaz, sadece karar verme ve doğrulamaya gider.

**Team agent / sub-agent olarak kullanım:** Ollama, iş yükünü hafifletmek için team agent veya sub-agent olarak kullanılabilir. Özellikle paralel görevlerde mekanik kısımları Ollama'ya offload edip Claude sadece doğrulama ve karar verme rolünde kalabilir.

**Token Tasarrufu Politikası (Strict Rule):** Eğer bir görev tekrarlı/mekanik ise ve Ollama ile çözülebilecekse, Claude token'ı harcamak yerine Ollama'ya offload et. Örnekler:
- 3+ dosyada aynı pattern'i uygulama (null!, default!, ?? "" gibi)
- Boilerplate kod üretimi (yeni manager, yeni test sınıfı iskeleti)
- Basit kod dönüşümleri (rename, type change, import ekleme)
- Dosya içeriğini analiz edip fix önerisi çıkarma

Claude sadece karar verme, doğrulama ve karmaşık reasoning için kullanılmalı.

```bash
# Kullanım
curl -s http://localhost:11434/api/generate -d '{"model":"entegrasyon-coder","prompt":"...","stream":false}'
```

## ECC Skill Kullanımı — Verimli Delegasyon (Strict Rule)

ECC skill/agent'ları (`ecc:*`) projenin kendi kurallarını EZMEZ, zenginleştirir. Tüm proje agent'ları (swe/db/qa/designer/devops/pa) bu ilkeyi izler:

- **Açık + dar delegasyon:** Skill'ler otomatik aktive OLMAZ. İhtiyaç anında, kapsamlı işe **ilgili skill'i açıkça çağır** — hepsini birden yükleme. "Doğru skill, doğru anda" (gereksiz skill yükü = bağlam erozyonu).
- **Önce araştır (research-first):** Bilinmeyen API/desende koda başlamadan `search-first` / `documentation-lookup` / `microsoft-docs` (sürüme-özel) ile doğrula — halüsinasyonu kaynağında engelle.
- **Sıra (plan→test→kod→gate):** `ecc:plan` (gerekirse) → `tdd-workflow` (RED→GREEN) → implementasyon → bağımsız review gate (`ecc:csharp-reviewer` / `ecc:database-reviewer` / `ecc:security-reviewer`). Review'u atlamak yok.
- **Token yönlendirme:** Opus-düzeyi derin düşünmeyi yalnız **mimari/şema kararı** için harca; mekanik/tekrarlı işi yerel Ollama'ya (`entegrasyon-coder`) offload et; uzun oturumda **mantıksal kırılımda** (araştırma sonrası, implementasyondan önce) compact.
- **Bağlamı koru:** Aynı anda <10 MCP / <80 tool aktif; ham `grep`/büyük dosya okuma yerine `graphify query`. Doğru ECC kurulumu plugin **veya** manuel — ikisini üst üste bindirme.

## Data-Access İş Bölümü (Strict Rule)

Veritabanı bu işin kalbi; **"yavaşlığa tahammül yok"**. Sorgu yazımı ile index/şema kararı AYRI disiplinlerdir (denetimde görüldü: Orders matcher'ları yazıldı ama index'leri unutuldu). **(C) Hibrit model** geçerlidir:

- **DB Master sahibi:** migration, `IEntityTypeConfiguration`, **index'ler**, hot-path/perf-kritik query'ler (pazaryeri polling matcher, export, dashboard, search, sync aggregate), N+1 audit, multi-tenant sorgu deseni, compiled query, perf gate.
- **SWE sahibi:** günlük CRUD / basit tek-tablo filtre LINQ'i — gate'siz, feature içinde TDD ile.
- **DB Master review ŞART (objektif tetikleyici):**
  1. `Include` zinciri (≥2) / `GroupBy` / `join` / raw SQL
  2. Döngü içinde çalışan query (N+1 riski)
  3. Hot/büyük tabloda (Products, ProductVariants, BranchOfficeStocks, Orders) **indexsiz kolonda** `Where`/`OrderBy`
  4. `Skip/Take` pagination'da indexli `OrderBy` yoksa
  → Geri kalan (by-PK lookup, basit tek-tablo filtre) **gate'siz**, SWE serbest.
- **SWE self-check (review öncesi):** `AsNoTracking` mı? `Select` projeksiyon mu (full-entity değil)? multi-`Include`'da `AsSplitQuery` mi? Hedef kolon index'li mi?
- **SQL loglaması:** sadece Dev/CI (`LogTo` + `EnableSensitiveDataLogging`) — **prod'da ASLA** (PII/secret/I-O). CI'da opt-in N+1/seq-scan flag.
- **Compiled query (multi-tenant):** context tipi tüm tenant'larda aynı (sadece connection değişir) → cache güvenli; DB Master bilerek tasarlar.

## Geliştirme Ortamı — Container Politikası (Strict Rule)

- **Kullanıcının YEREL makinesinde container KURMA/ÇALIŞTIRMA.** Tüm container'lar (postgres, wiremock, app, redis, mock servisleri, vb.) **server'da (192.168.1.78)** çalışır. Yerel makinede yalnızca **local debug** olur (`dotnet run` + debugger).
- **Testcontainers** gerekiyorsa yerel Docker daemon başlatma — **server Docker'ını socket-tünelle** kullan: `ssh -nNT -L /tmp/docker-server.sock:/var/run/docker.sock server &` + `DOCKER_HOST=unix:///tmp/docker-server.sock TESTCONTAINERS_HOST_OVERRIDE=192.168.1.78 TESTCONTAINERS_RYUK_DISABLED=true`. (`ssh://` Docker.DotNet'te ÇALIŞMAZ.)
- **Dev/stage ortamı, DB'ler, WireMock, mock servisleri → hepsi server'da.** `develop` push → Gitea runner ile otomatik dev deploy.
- **İSTİSNA:** Yerelde bir container gerçekten gerekiyorsa ÖNCE kullanıcıya sor — varsayılan asla yerel container değil.

## WireMock Doğruluğu (Strict Rule — dev ortamı)

Dev'de pazaryeri API'leri WireMock ile mock'lanır (gerçek key yok). Mock'lar **resmi pazaryeri dokümantasyonunu birebir yansıtmalı:** response alanları/formatı dokümandaki gibi olmalı; **geçersiz/yanlış request gönderilirse gerçek API gibi uygun HATA mesajı/kodu dönmeli** (request validation). Yani sadece happy-path canned değil — doc-doğru + validation-error'lı. Uygulama dev'de "gerçekten gönderiyormuş gibi" davranmalı, yanlışta hatayı görmeli.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
