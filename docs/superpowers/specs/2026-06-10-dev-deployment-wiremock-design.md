# Dev Deployment + WireMock Mock Ortamı — Tasarım Dokümanı

**Tarih:** 2026-06-10
**Rol:** DevOps / Integrations Engineer
**Durum:** PLAN — uygulama TL + kullanıcı onayından sonra. Bu doküman sadece araştırma + tasarım.

---

## 1. Amaç

Kullanıcının, uygulamayı **gerçek senaryo gibi** test edebileceği izole bir **dev ortamı** kurmak:

1. **Ayrı portta** çalışan bir dev app container'ı (8085).
2. **`develop` branch'e push** → Gitea runner ile **otomatik build + deploy** (push-to-deploy).
3. **Localhost-geliştirme DB'sinden VE stage'den ayrı** bir dev veritabanı (`IntegrationDb_Dev` + `AdminPanelDb_Dev`).
4. **WireMock ile pazaryeri API'leri mock'lanmış** — dev'de **gerçek API key YOK**; app "gerçekten gönderiyormuş gibi" davranır, canned response alır.

Böylece kullanıcı dev'de gerçek akışları (sync, ürün gönder, sipariş çek) güvenle deneyip PA ile yeni task üretebilir.

### 1.1 İKİ ZORUNLU KURAL (kullanıcı — CLAUDE.md'de de var)

**KURAL 1 — Yerel makinede HİÇ container çalışmaz.** postgres, WireMock, app, redis, mock'lar **hepsi server'da** (`192.168.1.78`). Yerelde yalnızca **local debug** (`dotnet run`) yapılır. Bu dev ortamı **server-merkezli** kurulur: dev DB + dev app + WireMock **server'da**, `develop` push → Gitea runner otomatik deploy. "Kullanıcı kendi makinesinde docker-compose ile kaldırsın" deseni **YASAK**.
- **Sonuç:** Repo'daki mevcut `docker-compose.dev.yml` (lokal WireMock override) bu dev ortamı için **KULLANILMAZ** — o yalnız bir lokal-debug yardımcısıydı. Server dev stack'i ayrı, `/opt/stacks/entegrasyon-dev/` altında kurulur.
- **Local debug (`dotnet run`) bağlanırken** server'daki servislere bakar: WireMock → `http://192.168.1.78:8091` (LAN portu), dev DB → §11'deki yöntemle (postgres LAN'a kapalı, SSH tünel veya geliştiricinin mevcut local DB'si). Bu, dev *ortamından* (deploy edilen, tam server-side olan) bağımsızdır.

**KURAL 2 — WireMock doc-fidelity.** Dev mock'ları **resmi pazaryeri dokümanını birebir** yansıtır: (a) **doc-doğru başarılı response** (gerçek API'nin alan adları/tipleri/zarf yapısı), (b) **geçersiz request'te gerçek API gibi validation hatası** (eksik zorunlu alan → 400/422 + marketplace-şekilli hata gövdesi; yanlış/eksik auth → 401). Düz "her zaman 200 dön" yeterli DEĞİL. Detay §7'de; mapping ayrıntısını **PA çıkarıyor — onunla koordine.**

---

## 2. Mevcut Durum (araştırma bulguları — kanıtlı)

### 2.1 Repo
- **Tek Dockerfile:** `Application/Entegrasyon.MVC/Dockerfile` (multi-stage, .NET 10 SDK build → aspnet:10.0-alpine runtime, `EXPOSE 8080`, healthcheck `/health/live`). AdminPanel/Storefront henüz dockerize değil → **dev ortamı = MVC** (storefront/admin ayrı iş).
- **Compose dosyaları:** `docker-compose.yml` (lokal db/redis/pgadmin), `.stage.yml` (mvc-stage 8081), `.prod.yml` (8080), `.e2e.yml`, `.dev.yml` (**WireMock override — zaten var!**), `.override.yml` (boş).
- **`docker-compose.dev.yml` zaten WireMock servisi tanımlıyor:** `wiremock/wiremock:3.9.2`, `docs/wiremock/{mappings,__files}` volume mount, app'e `DevMode__WireMockUrl=http://wiremock:8080` enjekte ediyor. **Lokal geliştirme için tasarlanmış** (server deploy için değil) — server dev stack'i buna paralel kurulacak.

### 2.2 WireMock mekanizması (ZATEN İMPLEMENTE)
- `Application/Entegrasyon.MVC/Infrastructure/DevMode/DevWireMockSeeder.cs`:
  - Startup'ta (`ApplicationStarted`) **yalnızca `app.Environment.IsDevelopment()` ise** çalışır (Program.cs:382).
  - `DevMode:WireMockUrl` config doluysa, DB'deki tüm `MarketPlace.BaseUrl` değerlerini bu URL'e **UPDATE eder** (DB'ye yazar).
  - `DevMode:RealApiMarketplaces` listesindeki marketplace'lere dokunmaz (gerçek sandbox'a gitsin istenirse).
  - DB bağlantısı yoksa fail olmaz, log'layıp geçer.
- **Marketplace client'lar BaseUrl'ü DB'den okuyor** (Trendyol/Hepsiburada/N11/Amazon/Ciceksepeti/Pazarama `*ApiClient.cs`) → seeder URL'i değiştirince tüm HTTP çağrıları WireMock'a düşer. **Mekanizma doğru, sadece dev container'ı `ASPNETCORE_ENVIRONMENT=Development` + `DevMode:WireMockUrl` ile koşturmak yeterli.**
- **Stub varlığı:** `docs/wiremock/mappings/` + `__files/` altında Trendyol, Hepsiburada, N11, Amazon, Ciceksepeti, Pazarama, Pttavm, Temu için hazır mapping'ler mevcut (orders, product-create, stock-price-update, auth vb.).

### 2.3 Server (ssh server — kanıtlı)
| Bileşen | Durum |
|---|---|
| `postgres_db` | Up (healthy), **`127.0.0.1:5432` (LAN'a KAPALI)**, network `integration_app_default` (172.24.0.9), alias `postgres_db`/`postgres` |
| `integration_app_default` network | Var (172.24.0.0/16) — app↔db DNS buradan |
| `entegrasyon-stage` | Up, port **8082→8080**, image `entegrasyon-blazor:stage` (**ESKİ blazor**, MVC değil — stage MVC kırık) |
| `registry` | Up, `192.168.1.78:5252` |
| `act_runner` (Gitea) | Up, `server-runner` |
| `entegrasyon-github-runner` | Up (GitHub Actions self-hosted, ayrı) |
| Postgres DB'ler | AdminPanelDb, IntegrationDb, IntegrationDb_Stage, default_db — **dev DB YOK** |
| Stack dizini | `/opt/stacks/<isim>/` (Dockge). `git` stack'i Gitea+runner barındırıyor |

### 2.4 Gitea runner — KRİTİK FARK (GitHub'dan)
`/opt/stacks/git/runner-config.yaml`:
```yaml
runner:
  capacity: 2
  labels:
    - "ubuntu-latest:docker://node:20-bookworm"
    - "ubuntu-22.04:docker://node:20-bookworm"
container:
  options: "-v /opt/stacks:/opt/stacks"   # her job'a /opt/stacks otomatik mount
  valid_volumes:
    - /var/run/docker.sock                  # whitelist — job mount edebilir
    - /opt/stacks
    - /mnt/storage/data/app-dist/files
```
- **Gitea job'ları `node:20-bookworm` CONTAINER'ı içinde koşar** — GitHub'daki `runs-on: [self-hosted, entegrasyon]` (host-level, docker hazır) **Gitea'da YOK.**
- Job içinde `docker` komutu çalıştırmak için: (a) docker CLI'lı bir image + (b) `/var/run/docker.sock` mount (whitelist'te → izinli). `/opt/stacks` zaten otomatik mount'lu.
- **Tasarım sonucu:** dev workflow job'ları `container.image: docker:27-cli` + `container.volumes: [/var/run/docker.sock, /opt/stacks]` deseni kullanmalı. .NET runner'a gerek YOK — `docker build` Dockerfile'ın SDK stage'ini kullanır.

### 2.5 Multi-tenant DB modeli (dev'i etkiler)
- `2026-03-27-multi-tenant-design.md`: **Database-per-tenant.** `ConnectionStrings__Main` (IntegrationDb) + `ConnectionStrings__AdminPanel` (AdminPanelDb). Tenant CS'leri AdminPanelDb'de saklı.
- **Stage bug'ının kökü buraya bağlı** (tasks.json HIGH): AdminPanelDb'deki tenant CS'leri `Host=192.168.1.78` → container'dan çözülmez; `Host=postgres_db` olmalı. **Dev için baştan doğru kuracağız** (aynı hataya düşmeyiz).

---

## 3. Tasarım Kararları

| # | Karar | Seçim | Gerekçe |
|---|---|---|---|
| D1 | Dev DB | **Ayrı `IntegrationDb_Dev` + `AdminPanelDb_Dev`** (aynı `postgres_db` container'ında, ayrı veritabanları) | Stage/lokal'den tam izolasyon; ayrı postgres container'ı gereksiz (kaynak). KVKK riski yok (mock data). |
| D2 | DB Host | **`Host=postgres_db`** (container DNS, `integration_app_default` network) | postgres `127.0.0.1` dinliyor; `192.168.1.78` container'dan çözülmez. **Stage bug'ını tekrarlama.** |
| D3 | Dev app environment | **`ASPNETCORE_ENVIRONMENT=Development`** | DevWireMockSeeder yalnızca `IsDevelopment()`'ta çalışıyor. Dev'de developer exception page + offline mode istiyoruz zaten. |
| D4 | Port | App **8085**, WireMock admin **8091** (host'a yayın, inceleme için) | 8080(prod)/8081(stage-mvc)/8082(stage-blazor) dolu. WireMock admin UI kullanıcının request'leri görüp stub eklemesi için faydalı. |
| D5 | CI trigger | **`develop` push → Gitea Actions** (`.gitea/workflows/deploy-dev.yml`) | Kullanıcı isteği. Gitea = GitHub Actions uyumlu. |
| D6 | Runner deseni | **`docker:27-cli` container + docker.sock mount** | Gitea runner node:20-bookworm container içinde; docker CLI gerekiyor (§2.4). |
| D7 | Marketplace mock | **DevWireMockSeeder (mevcut)** — `DevMode__WireMockUrl=http://wiremock:8080` | Yeni kod YOK; mevcut mekanizma. Gerçek key gerekmez. |
| D8 | WireMock data | `docs/wiremock/{mappings,__files}` repo'dan (build context veya bind) | Stub'lar versiyonlu, hot-reload. |
| D9 | Secret yönetimi | **Gitea Actions repo secrets** + server `.env` (gitignore) | `DEV_DB_CONNECTION`, `DEV_ADMIN_DB_CONNECTION` secret; compose'da `${VAR}`. Commit YOK. |
| D10 | Stack yeri | **`/opt/stacks/entegrasyon-dev/`** (Dockge) | Sunucu konvansiyonu. Stage/prod'a dokunmadan ayrı stack. |

---

## 4. Hedef Mimari

```
                    push to develop (gitea remote :2222)
                              │
                              ▼
              ┌───────────────────────────────────┐
              │  Gitea Actions: deploy-dev.yml     │
              │  (act_runner, docker:27-cli job)   │
              │  1. docker build MVC Dockerfile    │
              │  2. push → 192.168.1.78:5252/      │
              │     entegrasyon-mvc:dev            │
              │  3. migrate (docker run --network  │
              │     integration_app_default)       │
              │  4. compose pull && up -d          │
              └───────────────┬───────────────────┘
                              ▼
   /opt/stacks/entegrasyon-dev/compose.yaml
   ┌──────────────────────────────────────────────────────────┐
   │  entegrasyon-mvc-dev   (8085:8080)                        │
   │    ASPNETCORE_ENVIRONMENT=Development                     │
   │    ConnectionStrings__Main=Host=postgres_db;...Dev        │
   │    ConnectionStrings__AdminPanel=Host=postgres_db;...Dev  │
   │    DevMode__WireMockUrl=http://wiremock:8080              │
   │    networks: integration_app_default (→postgres_db)       │
   │              entegrasyon-dev-net      (→wiremock)         │
   │                                                          │
   │  entegrasyon-wiremock-dev  (8091:8080)                   │
   │    /home/wiremock/{mappings,__files}                     │
   │    networks: entegrasyon-dev-net                         │
   └──────────────────────────────────────────────────────────┘
                              │
                  postgres_db (integration_app_default)
                  ├── IntegrationDb_Dev   (mock veriler)
                  └── AdminPanelDb_Dev     (tenant CS → Host=postgres_db)
```

**Ağ notu:** dev app **iki network'e** bağlanır — `integration_app_default` (postgres_db DNS için, external) + `entegrasyon-dev-net` (wiremock DNS için). WireMock yalnız dev-net'te (postgres'e erişmesi gerekmez). Böylece stage/prod'a sıfır temas.

---

## 5. Adım Adım Uygulama Planı (onaydan sonra)

### Faz 0 — Hazırlık / doğrulama (idempotent, destructive değil)
1. Gitea'da repo secret olarak `DEV_DB_CONNECTION` ve `DEV_ADMIN_DB_CONNECTION` ekle (UI veya API):
   - `DEV_DB_CONNECTION = Host=postgres_db;Port=5432;Database=IntegrationDb_Dev;Username=baturhan;Password=<...>;Pooling=true;Maximum Pool Size=20;Include Error Detail=true`
   - `DEV_ADMIN_DB_CONNECTION = Host=postgres_db;Port=5432;Database=AdminPanelDb_Dev;Username=baturhan;Password=<...>`
2. `develop` branch'in `gitea` remote'unda olduğunu doğrula (var — `remotes/gitea/develop`).

### Faz 1 — Dev DB provisioning (idempotent)
3. `postgres_db` içinde DB oluştur (varsa atla):
   ```sql
   SELECT 'CREATE DATABASE "IntegrationDb_Dev"'  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname='IntegrationDb_Dev');
   SELECT 'CREATE DATABASE "AdminPanelDb_Dev"'    WHERE NOT EXISTS (...);
   ```
   (psql `\gexec` veya `createdb` ile.) **Boş DB** — migration + start-action seed dolduracak.
4. **Migration:** workflow'un migrate adımı ilk deploy'da şemayı kurar (aşağıda). Manuel ilk kez:
   `docker run --rm --network integration_app_default -e "ConnectionStrings__Main=$DEV_DB_CONNECTION" 192.168.1.78:5252/entegrasyon-mvc:dev dotnet Entegrasyon.MVC.dll --migrate`
5. **AdminPanelDb_Dev tenant kaydı:** app start-action / provisioning tenant CS'sini `Host=postgres_db;Database=IntegrationDb_Dev` yazmalı. Seed sonrası **doğrula** (stage bug'ı buydu): AdminPanelDb_Dev'deki tenant CS'lerinde `192.168.1.78` OLMAMALI. Gerekirse tek seferlik UPDATE.

### Faz 2 — Stack dosyaları (`/opt/stacks/entegrasyon-dev/`)
6. `compose.yaml` (taslak §6.1), `.env` (secret — gitignore), `.env.example` (placeholder) yaz. `scp`/`printf|tee` (zsh heredoc bozar).
7. `docker compose config -q` ile doğrula.

### Faz 3 — Gitea workflow (`.gitea/workflows/deploy-dev.yml`)
8. Workflow yaz (taslak §6.2), repo'ya commit (develop). **`.github/workflows` kopyası DEĞİL** — Gitea runner farkı (§2.4) için uyarlanmış.

### Faz 4 — İlk deploy + doğrulama
9. `develop`'a push → run yeşil mi izle.
10. **Sağlık testleri (kanıt):**
    - `curl http://192.168.1.78:8085/health/live` → 200
    - `192.168.1.78:8085` login (admin / 123456789) çalışıyor
    - WireMock yönlendirme: bir Trendyol sync tetikle → `curl http://192.168.1.78:8091/__admin/requests` ile WireMock'a istek düştüğünü gör (gerçek API'ye GİTMEDİ)
    - DB izolasyon: dev'de yapılan değişiklik IntegrationDb/IntegrationDb_Stage'i etkilemiyor

### Faz 5 — (Opsiyonel) NPM ile dışa açma
11. İstenirse `dev.entegrasyon.baturhan.xyz` → `192.168.1.78:8085` (NPM, Force SSL, Access List). **Önce kullanıcıya sor.**

---

## 6. Taslak Artefaktlar (onayda netleşecek — şimdilik referans)

### 6.1 `/opt/stacks/entegrasyon-dev/compose.yaml` (taslak)
```yaml
services:
  mvc-dev:
    image: 192.168.1.78:5252/entegrasyon-mvc:dev
    container_name: entegrasyon-mvc-dev
    restart: unless-stopped
    ports:
      - "8085:8080"                       # tüm arayüze (NPM/LAN erişsin)
    environment:
      - ASPNETCORE_ENVIRONMENT=Development # DevWireMockSeeder bunu ister
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__Main=${DEV_DB_CONNECTION}          # Host=postgres_db
      - ConnectionStrings__AdminPanel=${DEV_ADMIN_DB_CONNECTION}
      - ConnectionStrings__Redis=
      - DevMode__WireMockUrl=http://wiremock:8080             # → mock'a yönlendir
      # DevMode__RealApiMarketplaces boş = TÜM marketplace mock
      - Minio__Endpoint=${MINIO_ENDPOINT}
      - Minio__AccessKey=${MINIO_ACCESS_KEY}
      - Minio__SecretKey=${MINIO_SECRET_KEY}
      - Minio__UseSSL=false
      - Minio__BucketName=products-dev
      - Minio__PublicBaseUrl=${MINIO_PUBLIC_URL}
      - Tenant__DefaultSubdomain=dev
      - TZ=Europe/Istanbul
    depends_on:
      wiremock:
        condition: service_healthy
    networks:
      - integration_app_default   # postgres_db DNS
      - dev-net                    # wiremock DNS

  wiremock:
    image: wiremock/wiremock:3.9.2
    container_name: entegrasyon-wiremock-dev
    restart: unless-stopped
    ports:
      - "8091:8080"               # admin UI / request inceleme (kullanıcı için)
    command: ["--global-response-templating", "--verbose", "--root-dir", "/home/wiremock"]
    volumes:
      - /opt/stacks/entegrasyon-dev/wiremock/mappings:/home/wiremock/mappings:ro
      - /opt/stacks/entegrasyon-dev/wiremock/__files:/home/wiremock/__files:ro
    healthcheck:
      test: ["CMD", "wget", "--spider", "-q", "http://localhost:8080/__admin/health"]
      interval: 10s
      timeout: 5s
      retries: 3
    networks:
      - dev-net

networks:
  integration_app_default:
    external: true
    name: integration_app_default
  dev-net:
    driver: bridge
```
> **Açık soru (Q3):** WireMock stub'ları repo'da `docs/wiremock/` altında. Stack'e nasıl gelsin? (a) workflow deploy adımında `cp -r repo/docs/wiremock/{mappings,__files} /opt/stacks/entegrasyon-dev/wiremock/` ile senkronla (repo = tek kaynak), ya da (b) WireMock'a repo dizinini doğrudan mount et. (a) önerilir — stack self-contained.

### 6.2 `.gitea/workflows/deploy-dev.yml` (taslak — Gitea runner'a uyarlı)
```yaml
name: Deploy to Dev
on:
  push:
    branches: [ develop ]

jobs:
  build-push-deploy:
    runs-on: ubuntu-latest
    container:
      image: docker:27-cli            # docker CLI'lı job container
      volumes:
        - /var/run/docker.sock:/var/run/docker.sock   # whitelist'te
        - /opt/stacks:/opt/stacks                      # whitelist'te (zaten auto)
    steps:
      - uses: actions/checkout@v4

      - name: Build & push dev image
        run: |
          SHORT_SHA=$(git rev-parse --short HEAD)
          docker build \
            -t 192.168.1.78:5252/entegrasyon-mvc:dev-${SHORT_SHA} \
            -t 192.168.1.78:5252/entegrasyon-mvc:dev \
            -f Application/Entegrasyon.MVC/Dockerfile .
          docker push 192.168.1.78:5252/entegrasyon-mvc:dev-${SHORT_SHA}
          docker push 192.168.1.78:5252/entegrasyon-mvc:dev

      - name: Sync WireMock stubs to stack
        run: |
          mkdir -p /opt/stacks/entegrasyon-dev/wiremock
          cp -r docs/wiremock/mappings /opt/stacks/entegrasyon-dev/wiremock/
          cp -r docs/wiremock/__files  /opt/stacks/entegrasyon-dev/wiremock/

      - name: Run EF Core migrations (dev DB)
        run: |
          docker run --rm --network integration_app_default \
            -e "ConnectionStrings__Main=${{ secrets.DEV_DB_CONNECTION }}" \
            192.168.1.78:5252/entegrasyon-mvc:dev \
            dotnet Entegrasyon.MVC.dll --migrate

      - name: Deploy dev stack
        run: |
          cd /opt/stacks/entegrasyon-dev
          docker compose pull
          docker compose up -d --wait
          echo "Dev deploy completed at $(date)"
```
> **Not (§2.4):** `docker:27-cli` image'ında `git`/`bash` minimal; `git rev-parse` için `apk add --no-cache git` gerekebilir, ya da SHA'yı `${{ github.sha }}` (gitea: `gitea.sha`) context'inden al. `docker compose` v2 plugin'i `docker:27-cli`'da var. Doğrulanacak (Q4).
> Unit-test job'u opsiyonel: dev hız için atlanabilir; istenirse ayrı `mcr.microsoft.com/dotnet/sdk:10.0` container job'u eklenir (node runner'da dotnet yok).

### 6.3 `.env.example` (placeholder — gerçek `.env` gitignore)
```env
DEV_DB_CONNECTION=Host=postgres_db;Port=5432;Database=IntegrationDb_Dev;Username=baturhan;Password=__SECRET__;Pooling=true;Maximum Pool Size=20;Include Error Detail=true
DEV_ADMIN_DB_CONNECTION=Host=postgres_db;Port=5432;Database=AdminPanelDb_Dev;Username=baturhan;Password=__SECRET__
MINIO_ENDPOINT=192.168.1.78:9000
MINIO_ACCESS_KEY=__SECRET__
MINIO_SECRET_KEY=__SECRET__
MINIO_PUBLIC_URL=http://192.168.1.78:9000
```

---

## 7. WireMock Doc-Fidelity Gereksinimi (KURAL 2)

Dev mock'ları gerçek pazaryeri API'sini **inandırıcı** taklit etmeli — yoksa "gerçek senaryo gibi test" amacı boşa gider. Mevcut `docs/wiremock/mappings/` çoğunlukla **tek, koşulsuz 200 response** içeriyor (yeterli DEĞİL). Hedef fidelity seviyesi:

### 7.1 Başarılı response — doc-doğru
- Gövde, resmi pazaryeri dokümanının **alan adları / tipleri / zarf yapısı** ile birebir (ör. Trendyol `content[].orderNumber`, sayfalama `page/size/totalElements`; HB/N11 kendi şemaları).
- `Content-Type`, doc'taki status (200/201/202), gerektiğinde rate-limit/etag header'ları.
- `--global-response-templating` açık (mevcut) → echo alanları (gönderilen barkod/sipariş no response'a yansır) ile daha gerçekçi.

### 7.2 Geçersiz request → gerçek API gibi hata (validation)
WireMock **request matching + priority** ile katmanlanır:
- **En spesifik mapping** (geçerli istek) yüksek öncelik → doc-doğru 200.
- **Daha düşük öncelikli "negatif" mapping'ler:**
  - Eksik/yanlış zorunlu alan (`matchesJsonPath`/`absent`) → **400/422** + marketplace-şekilli hata gövdesi (ör. Trendyol `{ "errors": [{ "key": "...", "message": "..." }] }`).
  - Auth header yok/yanlış → **401/403** (gerçek API gibi).
  - Bilinmeyen endpoint → **404** (catch-all, doc-şekilli).
- Böylece app'in validation/hata-yönetim yolları da dev'de gerçekten tetiklenir.

### 7.3 Sahiplik & koordinasyon
- **PA** her endpoint için resmi doc referansı + örnek geçerli/geçersiz request-response ve beklenen hata gövdelerini çıkarıyor → bu mapping spesifikasyonunun **kaynağı**.
- **DevOps (ben):** PA'nın çıkardığı spec'i WireMock mapping JSON'larına (`mappings/<mp>/*.json` + `__files/`) çeviririm, priority/matcher kurarım, server WireMock'ta doğrularım.
- **Doğrulama:** geçerli istek → 200 doc-şekil; bozuk istek → doğru 4xx. `:8091/__admin/requests` ile gözlemlenir.
- Fixture güncelliği: `docs/wiremock/README.md`'deki `record.sh`/`sanitize.sh` (gerçek sandbox'tan kayıt + credential temizliği) ileride opsiyonel — dev'de gerçek key olmadığı için öncelik **doc'tan elle yazım**.

> **Açık iş:** Mevcut stub'lar tek-response; doc-fidelity'ye yükseltmek endpoint-başı iş. PA backlog'una bağlanmalı (her marketplace × kritik endpoint). Dev ortamı önce mevcut stub'larla ayağa kalkar, fidelity iteratif yükseltilir.

---

## 8. Riskler & Önlemler

| # | Risk | Önlem |
|---|---|---|
| R1 | **DB host bug tekrarı** (stage 2 ay kırık) | Tüm CS'ler `Host=postgres_db`; migrate adımı `--network integration_app_default`. AdminPanelDb_Dev tenant CS'lerini deploy sonrası doğrula. |
| R2 | **DevWireMockSeeder DB'ye yazıyor** (MarketPlace.BaseUrl UPDATE) | Yalnız `IntegrationDb_Dev`'de çalışır (ayrı DB). Stage/prod IntegrationDb'ye ASLA dokunmaz çünkü onlar `Staging`/`Production` env (seeder `IsDevelopment` gate'li). İzolasyon kanıtı: dev DB ayrı + env=Development. |
| R3 | **`ASPNETCORE_ENVIRONMENT=Development` server'da** → developer exception page, detaylı hata | Dev ortamı için kabul/istenen. NPM açılırsa Access List ile koru. Internete açık DEĞİL (UFW + sadece NPM). |
| R4 | **Gitea runner docker.sock erişimi** job'da çalışmazsa | valid_volumes whitelist'te → izinli. İlk run'da `docker info` ile doğrula. Plan B: `entegrasyon-github-runner`'a benzer host-exec label runner eklemek (config değişikliği — kullanıcı onayı). |
| R5 | **Cargo (Aras/Surat/Yurtici) + e-fatura WireMock kapsamı dışı** | DevWireMockSeeder **yalnız MarketPlaces tablosunu** çevirir. Kargo client'ların BaseUrl'ü ayrı config/tablo → dev'de gerçek sandbox'a gidebilir. **Açık iş:** kargo/e-fatura için ayrı mock yönlendirmesi gerekirse follow-up task (PA ile). Şimdilik kapsam = pazaryeri. |
| R6 | **Stub eksikse 404 / fidelity düşük** (app marketplace çağrısında) | WireMock admin UI (`:8091/__admin/requests`) ile eşleşmeyen istek görülür, mapping eklenir. Doc-fidelity §7 — PA spec'i + iteratif yükseltme. `docs/wiremock/` zaten zengin ama çoğu tek-response. |
| R7 | **Migration `develop`'ta breaking** → dev DB bozulur | Dev DB mock; gerekirse `dropdb && createdb` ile sıfırla (idempotent). Backup şart değil (gerçek veri yok) ama ilk kurulumda `pg_dump` opsiyonel. |
| R8 | **MinIO bucket çakışması** | Ayrı bucket `products-dev` (compose'da). |
| R9 | **postgres_db'yi LAN'a açma cazibesi** | YAPMA. Container-network (`postgres_db` DNS) ile çözüldü. |
| R10 | **`docker:27-cli` job'da git/SHA/compose eksik** | İlk run'da doğrula; gerekirse `apk add git` veya `gitea.sha` context. |

## 9. Kırmızı Çizgiler (uygulama sırasında)
- **Yerel makinede container kaldırma** (KURAL 1) — her şey server'da. Yerel = sadece `dotnet run` debug.
- Stage/prod container, network, DB'lerine **dokunma** — dev ayrı stack + ayrı DB + ayrı network alias.
- postgres'i LAN'a **açma**.
- Secret'ı commit/log'a **yazma** (Gitea secret + `.env` gitignore).
- Server'da destructive aksiyon (DB drop, mevcut stack stop) öncesi **kullanıcıya sor**.
- Gerçek pazaryeri API'sine **canlı istek atma** (dev WireMock'a gider; `RealApiMarketplaces` boş kalsın).

## 10. Açık Sorular (TL/kullanıcı kararı)
- **Q1:** Dev DB başlangıç verisi — **boş + seed** (temiz, tekrarlanabilir) mi, yoksa **IntegrationDb'den kopya** (gerçekçi veri ama PII riski) mı? → Öneri: boş + start-action seed + birkaç mock ürün.
- **Q2:** Unit/integration test'leri dev pipeline'ında koşalım mı, yoksa hız için sadece build+deploy mı? → Öneri: dev = hızlı (sadece build+deploy); test'ler CI (`ci.yml`) ve stage/prod'da.
- **Q3:** WireMock stub kaynağı — workflow `cp` ile senkron (önerilen) mi, repo bind mount mı?
- **Q4:** `docker:27-cli` job deseni mi, yoksa Gitea runner'a **host-exec label** mı ekleyelim (daha basit ama runner-config değişikliği → kullanıcı onayı)?
- **Q5:** Dışa açma (`dev.entegrasyon.baturhan.xyz` NPM) bu fazda mı, sonra mı?
- **Q6 (KURAL 1):** Local debug (`dotnet run`) dev DB'ye nasıl bağlansın? postgres_db LAN'a kapalı (§11). Öneri: SSH tünel. Yoksa local debug mevcut local DB'siyle kalsın, dev *ortamı* tamamen server-side.

## 11. Local Debug & KURAL 1 (yerelde container yok)

**KURAL 1 gereği yerel makinede HİÇ container çalışmaz** — postgres/WireMock/app/redis hepsi server'da. Yerel yalnız `dotnet run` ile debug.

- **Dev ORTAMI** (deploy edilen, kullanıcının test ettiği): %100 server-side. Yerel docker'a hiç dokunmaz. Bu dokümanın ana konusu budur.
- **Local debug (`dotnet run`)** geliştirici için ayrı bir senaryo:
  - **WireMock'a erişim:** server WireMock LAN portu `http://192.168.1.78:8091` (compose'da host'a yayınlandı) → local app `DevMode__WireMockUrl` bunu kullanabilir. Yerelde WireMock container BAŞLATMA.
  - **DB erişimi:** `postgres_db` yalnız `127.0.0.1:5432` (LAN'a kapalı) → local `dotnet run` doğrudan ulaşamaz. Seçenekler: (a) **SSH tünel** `ssh -nNT -L 5432:127.0.0.1:5432 server &` → `Host=localhost` (önerilen, ekstra açılım yok), (b) geliştiricinin kendi lokal DB'sini kullanması (dev DB'den bağımsız). **postgres'i LAN'a AÇMA.**
  - Mevcut `appsettings.Development.json`'daki `Host=192.168.1.78;...IntegrationDb` zaten LAN'dan erişilemez (postgres kapalı) → SSH tünel + `Host=localhost` ile düzeltilebilir; bu ayrı bir küçük iyileştirme, dev deploy'dan bağımsız.
- **Repo'daki `docker-compose.dev.yml`/`.yml`/`.e2e.yml`** lokal container başlatır → KURAL 1 ile **yerelde kullanılmaz**. (CI/e2e ve server bağlamı ayrı; bu compose'ları silmiyoruz, sadece yerel iş akışında çağırmıyoruz.)

---

**Sonraki adım:** TL + kullanıcı onayı → Faz 0'dan başla. Her faz sonunda `docker compose config -q` + sağlık testi ile kanıtlı doğrula. Her container server'da (KURAL 1), her pazaryeri mock'u doc-fidelity hedefli (KURAL 2).
