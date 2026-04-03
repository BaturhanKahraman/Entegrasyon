# Blazor Projesi Kaldırma Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Blazor Server projesini ve tüm bağımlılıklarını tamamen kaldırarak MVC'yi tek UI olarak bırakmak.

**Architecture:** Blazor'un 57 sayfasının tamamı MVC'de mevcut. Bu plan: test referanslarını MVC'ye çevir → Docker/CI altyapısını MVC'ye yönlendir → Blazor dizinini sil → dokümantasyonu güncelle.

**Tech Stack:** .NET 10, ASP.NET Core MVC, xUnit, Docker, GitHub Actions

---

## Etki Analizi

| Kategori | Dosya/Dizin | İşlem |
|----------|------------|-------|
| Proje | `Application/Entegrasyon.Blazor/` (~454 dosya) | SİL |
| Proje | `Test/Entegrasyon.BunitTest/` (bUnit testleri) | SİL |
| Solution | `Entegrasyon.sln` | Blazor + bUnit satırlarını çıkar |
| Test | `Entegrasyon.UnitTest.csproj` | Blazor referansını çıkar |
| Test | `PagePermissionAttributeTests.cs` | SİL (Blazor sayfalarına bağlı) |
| Test | `BlazorTenantResolutionMiddlewareTests.cs` | SİL (Blazor middleware'ine bağlı) |
| Test | `Entegrasyon.IntegrationTest.csproj` | Blazor → MVC referansı |
| Docker | `docker-compose.prod.yml` | blazor-prod → mvc-prod |
| Docker | `docker-compose.stage.yml` | blazor-stage servisini sil |
| Docker | `docker-compose.e2e.yml` | blazor-e2e → mvc-e2e |
| CI/CD | `.github/workflows/deploy-stage.yml` | MVC image'ına geçir |
| CI/CD | `.github/workflows/e2e.yml` | Port/URL güncelle |
| Docs | `CLAUDE.md` | Blazor referanslarını kaldır |

---

### Task 1: Solution Dosyasını Temizle

**Files:**
- Modify: `Entegrasyon.sln` (lines 20-21, 24-25, 114-125, 138-149, 279, 281)

- [ ] **Step 1: Blazor ve bUnit projelerini solution'dan çıkar**

```bash
dotnet sln Entegrasyon.sln remove Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj
dotnet sln Entegrasyon.sln remove Test/Entegrasyon.BunitTest/Entegrasyon.BunitTest.csproj
```

- [ ] **Step 2: Solution build'ini doğrula**

```bash
dotnet build Entegrasyon.sln 2>&1 | head -20
```

Expected: Build başarısız olacak çünkü test projeleri hâlâ Blazor'a referans veriyor. Bu beklenen bir sonuç — Task 2'de düzelecek.

- [ ] **Step 3: Commit**

```bash
git add Entegrasyon.sln
git commit -m "chore: remove Blazor and bUnit projects from solution"
```

---

### Task 2: Unit Test Blazor Bağımlılıklarını Kaldır

**Files:**
- Modify: `Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj:35` — Blazor ProjectReference satırını sil
- Delete: `Test/Entegrasyon.Test/Security/PagePermissionAttributeTests.cs` — tümü Blazor page type'larına bağlı
- Delete: `Test/Entegrasyon.Test/Tenants/BlazorTenantResolutionMiddlewareTests.cs` — Blazor middleware'i test ediyor

- [ ] **Step 1: Unit test csproj'dan Blazor referansını sil**

`Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj` dosyasında şu satırı sil:

```xml
    <ProjectReference Include="..\..\Application\Entegrasyon.Blazor\Entegrasyon.Blazor.csproj" />
```

- [ ] **Step 2: Blazor-specific test dosyalarını sil**

```bash
rm Test/Entegrasyon.Test/Security/PagePermissionAttributeTests.cs
rm Test/Entegrasyon.Test/Tenants/BlazorTenantResolutionMiddlewareTests.cs
```

**Not:** `PagePermissionAttributeTests` Blazor assembly'sindeki tüm page type'ları tarar ve `[Authorize(Policy)]` attribute'larını doğrular. MVC'de bu kontrol `[RequireRole]` tag helper ve `[Authorize]` attribute'lar üzerinden yapılıyor — MVC test projesi (`Entegrasyon.MVC.Test`) zaten bu kontrolleri kapsıyor. `BlazorTenantResolutionMiddlewareTests` ise MVC'deki `TenantResolutionMiddleware` ile karşılığı olan testlere sahip.

- [ ] **Step 3: Unit testlerin build olduğunu doğrula**

```bash
dotnet build Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj
```

Expected: SUCCESS

- [ ] **Step 4: Unit testleri çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --verbosity minimal
```

Expected: Tüm testler geçmeli (3 test düşecek — silinen dosyalar). Kalan testlerin hepsi PASS.

- [ ] **Step 5: Commit**

```bash
git add Test/Entegrasyon.Test/
git commit -m "chore: remove Blazor references from unit tests"
```

---

### Task 3: Integration Test Blazor → MVC Referansını Değiştir

**Files:**
- Modify: `Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj:31`
- Modify: `Test/Entegrasyon.IntegrationTest/Fixtures/IntegrationTestWebAppFactory.cs:17`

Integration testler `WebApplicationFactory<Program>` kullanıyor. Şu an Blazor'un `Program` class'ını referans alıyor — MVC'nin `Program` class'ına çevrilmeli.

- [ ] **Step 1: csproj'da Blazor referansını MVC ile değiştir**

`Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj` dosyasında:

```xml
<!-- ESKİ -->
<ProjectReference Include="..\..\Application\Entegrasyon.Blazor\Entegrasyon.Blazor.csproj" />

<!-- YENİ -->
<ProjectReference Include="..\..\Application\Entegrasyon.MVC\Entegrasyon.MVC.csproj" />
```

- [ ] **Step 2: Build doğrula**

```bash
dotnet build Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj
```

Expected: SUCCESS — `WebApplicationFactory<Program>` artık MVC'nin `Program` class'ını kullanacak.

- [ ] **Step 3: Integration testleri çalıştır (Docker gerekli)**

```bash
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --verbosity minimal
```

Expected: Tüm testler PASS. MVC'nin DI container'ı Blazor ile aynı `ApplicationDependencyExtension` kullanıyor.

- [ ] **Step 4: Commit**

```bash
git add Test/Entegrasyon.IntegrationTest/
git commit -m "chore: switch integration tests from Blazor to MVC startup"
```

---

### Task 4: bUnit Test Projesini Sil

**Files:**
- Delete: `Test/Entegrasyon.BunitTest/` (tüm dizin)

bUnit yalnızca Blazor component testing için kullanılır. MVC'de karşılığı yoktur.

- [ ] **Step 1: bUnit test dizinini sil**

```bash
rm -rf Test/Entegrasyon.BunitTest/
```

- [ ] **Step 2: Solution build doğrula**

```bash
dotnet build Entegrasyon.sln
```

Expected: SUCCESS — solution'dan zaten Task 1'de çıkarılmıştı.

- [ ] **Step 3: Commit**

```bash
git add -A Test/Entegrasyon.BunitTest/
git commit -m "chore: delete bUnit test project (Blazor-only)"
```

---

### Task 5: Docker Compose Dosyalarını MVC'ye Geçir

**Files:**
- Modify: `docker-compose.prod.yml`
- Modify: `docker-compose.stage.yml`
- Modify: `docker-compose.e2e.yml`

- [ ] **Step 1: docker-compose.prod.yml — blazor-prod → mvc-prod**

`docker-compose.prod.yml` dosyasının tamamını şununla değiştir:

```yaml
services:
  mvc-prod:
    build:
      context: .
      dockerfile: Application/Entegrasyon.MVC/Dockerfile
    container_name: entegrasyon-prod
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__Main=${PROD_DB_CONNECTION}
      - ConnectionStrings__Redis=${REDIS_CONNECTION:-redis:6379}
      - ConnectionStrings__AdminPanel=${ADMIN_DB_CONNECTION}
      - Minio__Endpoint=${MINIO_ENDPOINT}
      - Minio__AccessKey=${MINIO_ACCESS_KEY}
      - Minio__SecretKey=${MINIO_SECRET_KEY}
      - Minio__UseSSL=false
      - Minio__BucketName=products
      - Minio__PublicBaseUrl=${MINIO_PUBLIC_URL}
      - Tenant__DefaultSubdomain=${TENANT_DEFAULT_SUBDOMAIN:-dev}
    networks:
      - entegrasyon-net

networks:
  entegrasyon-net:
    external: true
    name: entegrasyon_entegrasyon-net
```

- [ ] **Step 2: docker-compose.stage.yml — blazor-stage servisini sil**

`docker-compose.stage.yml` dosyasından `blazor-stage` servisini tamamen kaldır. `mvc-stage` portunu `8081:8080`'e çek (Blazor'un eski portu):

```yaml
services:
  mvc-stage:
    image: 192.168.1.78:5252/entegrasyon-mvc:stage
    container_name: entegrasyon-mvc-stage
    restart: unless-stopped
    ports:
      - "8081:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__Main=${STAGE_DB_CONNECTION}
      - ConnectionStrings__Redis=${REDIS_CONNECTION:-redis:6379}
      - ConnectionStrings__AdminPanel=${ADMIN_DB_CONNECTION}
      - Minio__Endpoint=${MINIO_ENDPOINT}
      - Minio__AccessKey=${MINIO_ACCESS_KEY}
      - Minio__SecretKey=${MINIO_SECRET_KEY}
      - Minio__UseSSL=false
      - Minio__BucketName=products-stage
      - Minio__PublicBaseUrl=${MINIO_PUBLIC_URL}
      - Tenant__DefaultSubdomain=dev
    networks:
      - entegrasyon-net

networks:
  entegrasyon-net:
    external: true
    name: entegrasyon_entegrasyon-net
```

- [ ] **Step 3: docker-compose.e2e.yml — blazor-e2e → mvc-e2e**

`docker-compose.e2e.yml` dosyasında `blazor-e2e` servisini `mvc-e2e` olarak değiştir:

```yaml
services:
  db-e2e:
    image: postgres:16-alpine
    container_name: entegrasyon-e2e-db
    environment:
      POSTGRES_USER: e2e_user
      POSTGRES_PASSWORD: e2e_password
      POSTGRES_DB: IntegrationDb_E2E
    ports:
      - "5433:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U e2e_user"]
      interval: 5s
      timeout: 3s
      retries: 5
    networks:
      - e2e-net

  minio-e2e:
    image: minio/minio:latest
    container_name: entegrasyon-e2e-minio
    command: server /data
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    ports:
      - "9099:9000"
    networks:
      - e2e-net

  mvc-e2e:
    build:
      context: .
      dockerfile: Application/Entegrasyon.MVC/Dockerfile
    container_name: entegrasyon-e2e-app
    ports:
      - "5099:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Testing
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__Main=Host=db-e2e;Port=5432;Database=IntegrationDb_E2E;Username=e2e_user;Password=e2e_password;Include Error Detail=true
      - ConnectionStrings__Redis=
      - Minio__Endpoint=minio-e2e:9000
      - Minio__AccessKey=minioadmin
      - Minio__SecretKey=minioadmin
      - Minio__UseSSL=false
      - Minio__BucketName=products-e2e
      - Minio__PublicBaseUrl=http://localhost:9099
      - Tenant__DefaultSubdomain=dev
      - Trendyol__UseMock=true
      - N11__UseMock=true
    depends_on:
      db-e2e:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:8080/health/live"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    networks:
      - e2e-net

networks:
  e2e-net:
    driver: bridge
```

**Not:** E2E healthcheck'i MVC'nin `/health/live` endpoint'ini kullanacak şekilde güncellendi.

- [ ] **Step 4: Commit**

```bash
git add docker-compose.prod.yml docker-compose.stage.yml docker-compose.e2e.yml
git commit -m "chore: migrate Docker Compose from Blazor to MVC"
```

---

### Task 6: CI/CD Workflow'larını Güncelle

**Files:**
- Modify: `.github/workflows/deploy-stage.yml`
- Modify: `.github/workflows/e2e.yml`

`deploy-prod.yml` değişiklik gerektirmez — zaten `docker compose -f docker-compose.prod.yml up --build -d` çağırıyor, compose dosyasındaki değişiklik yeterli.

- [ ] **Step 1: deploy-stage.yml — MVC image'ına geçir**

`.github/workflows/deploy-stage.yml` dosyasının `build-and-push` job'ını güncelle:

```yaml
  build-and-push:
    needs: unit-tests
    runs-on: [self-hosted, entegrasyon]
    steps:
      - uses: actions/checkout@v4

      - name: Build and push to local registry
        run: |
          SHORT_SHA=$(git rev-parse --short HEAD)
          docker build \
            -t 192.168.1.78:5252/entegrasyon-mvc:stage-${SHORT_SHA} \
            -t 192.168.1.78:5252/entegrasyon-mvc:stage \
            -f Application/Entegrasyon.MVC/Dockerfile .
          docker push 192.168.1.78:5252/entegrasyon-mvc:stage-${SHORT_SHA}
          docker push 192.168.1.78:5252/entegrasyon-mvc:stage
          echo "Pushed image with tags: stage-${SHORT_SHA}, stage"
```

Ayrıca `deploy` job'ındaki migration komutunu güncelle:

```yaml
      - name: Run EF Core migrations
        run: |
          docker run --rm --network host \
            -e "ConnectionStrings__Main=${STAGE_DB_CONNECTION}" \
            192.168.1.78:5252/entegrasyon-mvc:stage \
            dotnet Entegrasyon.MVC.dll --migrate
```

- [ ] **Step 2: e2e.yml — MVC healthcheck endpoint'ini kullan**

`.github/workflows/e2e.yml` dosyasında `Wait for app healthy` step'ini güncelle:

```yaml
      - name: Wait for app healthy
        run: |
          for i in $(seq 1 30); do
            if curl -sf http://localhost:5099/health/live > /dev/null 2>&1; then
              echo "App is ready"
              break
            fi
            echo "Waiting for app... ($i/30)"
            sleep 5
          done
```

**Not:** MVC'nin `/health/live` endpoint'i kullanılıyor (Dockerfile healthcheck ile tutarlı).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy-stage.yml .github/workflows/e2e.yml
git commit -m "chore: update CI/CD workflows to use MVC instead of Blazor"
```

---

### Task 7: Blazor Dizinini Sil

**Files:**
- Delete: `Application/Entegrasyon.Blazor/` (tüm dizin, ~454 dosya)

- [ ] **Step 1: Blazor dizinini sil**

```bash
rm -rf Application/Entegrasyon.Blazor/
```

- [ ] **Step 2: Solution build doğrula**

```bash
dotnet build Entegrasyon.sln
```

Expected: SUCCESS

- [ ] **Step 3: Tüm test suite'ini çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --verbosity minimal
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --verbosity minimal
```

Expected: Tüm testler PASS.

- [ ] **Step 4: Commit**

```bash
git add -A Application/Entegrasyon.Blazor/
git commit -m "feat: remove Blazor Server project — MVC is now the sole UI"
```

---

### Task 8: CLAUDE.md Güncelle

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Blazor referanslarını kaldır/güncelle**

Aşağıdaki değişiklikleri `CLAUDE.md`'de uygula:

1. **Commands bölümü (satır 12-13):** "Run Blazor app" komutunu sil

2. **Commands bölümü (satır 36, 39):** EF migration komutlarındaki `--startup-project Application/Entegrasyon.Blazor` → `--startup-project Application/Entegrasyon.MVC` olarak değiştir

3. **Architecture tablosu (satır 52):** `Entity → DataAccess → Business → MVC/Blazor` → `Entity → DataAccess → Business → MVC`

4. **Architecture tablosu (satır 61):** `Entegrasyon.Blazor` satırını tamamen sil

5. **Blazor & UI Development Standards bölümü:** Blazor'a özel tüm bölümleri (Code-Behind Pattern, Componentization, MudBlazor, SignalR referansları) sil

6. **Blazor Sayfa Yapısı bölümü:** Tamamen sil

7. **MudBlazor namespace çakışması notu:** Sil

8. **Key Patterns:** `MudDataGrid` notunu sil

9. **Test kullanıcısı:** E2E base URL notunu Blazor port'undan (5099) kaldır (MVC kullanacak)

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md — remove all Blazor references"
```

---

### Task 9: Memory Dosyalarını Güncelle

**Files:**
- Modify: `/home/baturhan/.claude/projects/-home-baturhan-Projeler-Entegrasyon/memory/MEMORY.md`
- Delete/Update: Blazor-specific memory files

- [ ] **Step 1: MEMORY.md'den Blazor referanslarını temizle**

`MEMORY.md`'deki aşağıdaki girdileri güncelle veya sil:
- "NavMenu" key file path'i (Blazor'a özel) → sil
- "Blazor UI Standards" bölümü → sil (artık geçersiz)
- "MudBlazor bUnit Gotchas" bölümü → sil
- `feedback_blazor_best_practices.md` referansını sil
- `feedback_notification_bell_scope.md` referansını sil (Blazor circuit'e özel)
- MVC migration memory'sini "tamamlandı" olarak güncelle

- [ ] **Step 2: Artık geçersiz memory dosyalarını sil**

```bash
rm /home/baturhan/.claude/projects/-home-baturhan-Projeler-Entegrasyon/memory/feedback_blazor_best_practices.md
rm /home/baturhan/.claude/projects/-home-baturhan-Projeler-Entegrasyon/memory/feedback_notification_bell_scope.md
```

- [ ] **Step 3: Commit (sadece MEMORY.md, memory dosyaları git dışı)**

Memory dosyaları git-tracked değil, commit gerekmez. Sadece MEMORY.md güncellenmiş olacak.

---

### Task 10: Son Doğrulama

- [ ] **Step 1: Solution clean build**

```bash
dotnet clean Entegrasyon.sln
dotnet build Entegrasyon.sln
```

Expected: SUCCESS, hiçbir Blazor referansı kalmamış olmalı.

- [ ] **Step 2: Tüm testleri çalıştır**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --verbosity minimal
dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --verbosity minimal
dotnet test Test/Entegrasyon.MVC.Test/Entegrasyon.MVC.Test.csproj --verbosity minimal
dotnet test Test/Entegrasyon.AdminPanel.Test/Entegrasyon.AdminPanel.Test.csproj --verbosity minimal
```

Expected: Tüm test suite'leri PASS.

- [ ] **Step 3: Grep ile son kontrol — kalan Blazor referansı var mı?**

```bash
grep -ri "blazor" --include="*.cs" --include="*.csproj" --include="*.sln" --include="*.yml" --include="*.yaml" --include="*.md" --include="*.json" . | grep -v ".git/" | grep -v "node_modules/" | grep -v "bin/" | grep -v "obj/"
```

Expected: Sadece `docs/superpowers/plans/` altındaki plan dosyasında ve belki eski plan referanslarında görünmeli. Aktif kod/config dosyalarında Blazor referansı OLMAMALI.

- [ ] **Step 4: Docker compose syntax doğrula**

```bash
docker compose -f docker-compose.prod.yml config --quiet
docker compose -f docker-compose.stage.yml config --quiet
docker compose -f docker-compose.e2e.yml config --quiet
```

Expected: Üçü de hatasız.

- [ ] **Step 5: Final commit (gerekirse)**

Eğer Task 10'da ek düzeltme yapıldıysa:

```bash
git add -A
git commit -m "chore: final cleanup after Blazor removal"
```
