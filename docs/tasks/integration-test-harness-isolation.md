# Integration Test Harness İzolasyon Fix (ERTELENDİ — ayrı oturum)

**Durum:** Ertelendi (2026-06-11). Test-infra, kullanıcı-facing DEĞİL. İki ajan bu görevde
erken öldü (uzun Testcontainers koşumları + derin diagnoz → süreç ölümü). Hedefli, tek başına
bir oturumda ele alınmalı. Aşağıdaki lead'ler iki ölü ajanın + müşteri ajanının diagnozundan.

## Belirti
Tam `Test/Entegrasyon.IntegrationTest` suite'inde ~82 test düşüyor; **tek tek / izole
koşulduğunda GEÇİYORLAR**. Yani gerçek kod bug'ı değil, harness izolasyon sorunu — ama bu
gerçek regresyonları MASKELİYOR (kör nokta).

## Kök neden lead'leri (somuttan genele)

1. **`UserName` varchar(30) taşması (SOMUT):** `ApplicationUserEntityConfiguration` →
   `UserName`/`NormalizedUserName` `HasMaxLength(30)`. Bazı seed helper'ları (ör.
   `SeedCustomerAndUserAsync`, `UserActivitySummaryIntegrationTests` `act-{key}`) 30'u aşan
   username set ediyor → INSERT patlıyor → bağımlı testler düşüyor. **Fix:** seed username'leri
   ≤30 yap. Etkilenen: 4 CustomerManager + bazı order-import testleri.

2. **Seed Admin satırı kayıp — SecurityStamp NOT NULL (SOMUT lead):** `MigrationTests.cs:53`
   `Users.FirstOrDefault(u => u.UserName == "Admin")` null dönüyor olabilir. Şüphe: seed
   migration `SecurityStamp` kolonunu NOT NULL default'suz ekliyor → seed Admin satırı
   yazılamıyor/eksik. (`AddSecurityStampToApplicationUser` migration'ı kontrol et.) **Fix:**
   seed'de SecurityStamp doldur veya migration default'u.

3. **"Tenant context is not initialized" (FIXTURE):** Integration testlerinde tenant context
   (TenantId=1) `WebApplicationFactory` DI'ında initialize edilmiyor → tenant-bağımlı sorgular
   patlıyor. **Fix:** test fixture'da `DefaultTenantContext`/`ITenantContext` seed/init et.

4. **Respawn cross-class izolasyon (FIXTURE):** Paylaşılan Testcontainers PostgreSQL + Respawn
   checkpoint cross-class state bırakıyor; paralel collection'lar birbirini görüyor. **Fix:**
   collection fixture seri/izole; Respawn seed-tabloları koruyarak deterministik reset.

## Kısıt
- Testcontainers YEREL Docker yok — server socket-tünel (CLAUDE.md):
  `ssh -nNT -L /tmp/docker-server.sock:/var/run/docker.sock server &` +
  `DOCKER_HOST=unix:///tmp/docker-server.sock TESTCONTAINERS_HOST_OVERRIDE=192.168.1.78 TESTCONTAINERS_RYUK_DISABLED=true`
- SADECE test projeleri + test-infra. Source/feature/migration'a dokunma.
- **Ölüm-eğilimli:** uzun full-suite koşumlarından kaçın; tek-tek test-class ile doğrula,
  full suite'i en sonda bir kez koş. Lead 1-2 (somut seed fix'leri) ÖNCE — bunlar büyük chunk'ı
  çözebilir, full Respawn redesign'a gerek kalmayabilir.
