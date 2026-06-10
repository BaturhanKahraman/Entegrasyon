# Dev Ortamı — Server-Merkezli (KURAL 1)

İki ayrı, birbirinden bağımsız senaryo var:

| | **Dev-Deploy (8083)** | **Local Debug (`dotnet run`)** |
|---|---|---|
| Amaç | Kullanıcının gerçek senaryo testi | Geliştirici breakpoint/debug |
| Nerede | %100 server'da (container) | Yerel makinede `dotnet run` |
| DB | `IntegrationDb_Dev` (server, `Host=postgres_db`) | `IntegrationDb` (server, SSH tünel → `Host=localhost`) |
| WireMock | dev stack içi `http://wiremock:8080` | server LAN portu `http://192.168.1.78:8091` |
| Tetikleme | `develop` push → Gitea otomatik deploy | manuel `dotnet run` |

**KURAL 1:** Yerel makinede HİÇ container çalıştırma (postgres/wiremock/app/redis hepsi server'da). Yerelde yalnız `dotnet run` debug.

---

## 1) Dev-Deploy (port 8083) — otomatik

`develop` branch'e push → Gitea act_runner (`.gitea/workflows/deploy-dev.yml`):
1. `entegrasyon-mvc:dev` image build + registry push (`192.168.1.78:5252`)
2. WireMock stub'larını `docs/wiremock/` → stack'e senkronlar
3. `IntegrationDb_Dev` migration (`--migrate`, `integration_app_default` network)
4. `/opt/stacks/entegrasyon-dev/` compose pull + up + healthcheck

**Erişim:** http://192.168.1.78:8083  (login: admin / 123456789)
**WireMock admin/inceleme:** http://192.168.1.78:8091/__admin/requests

- `ASPNETCORE_ENVIRONMENT=Development` → `DevWireMockSeeder` tüm `MarketPlace.BaseUrl`'lerini `http://wiremock:8080`'e çevirir. **Gerçek pazaryeri API key'i YOK** — her çağrı WireMock'a gider, canned response döner.
- Secret YOK: bağlantı string'leri server `.env`'de (`/opt/stacks/entegrasyon-dev/.env`, gitignore).
- Gerçek sandbox'a gitmesi istenen bir marketplace olursa: dev container env `DevMode__RealApiMarketplaces=["Trendyol"]` (seeder ona dokunmaz).

---

## 2) Local Debug — SSH tünel

postgres_db ve minio_storage server'da yalnız `127.0.0.1` dinler (LAN'a KAPALI, bilinçli). Yerel `dotnet run`'ın bunlara erişmesi için **SSH tünel** (postgres'i LAN'a açMADAN):

```bash
./scripts/dev-tunnel.sh --bg     # postgres→localhost:5432, minio→localhost:9000
# ... dotnet run ...
./scripts/dev-tunnel.sh --stop   # bitince kapat
```

Tünel açıkken `appsettings.Development.json`:
```jsonc
"ConnectionStrings": {
  "Main":       "Host=localhost;Port=5432;Database=IntegrationDb;Username=baturhan;Password=...",
  "AdminPanel": "Host=localhost;Port=5432;Database=AdminPanelDb;Username=baturhan;Password=..."
},
"DevMode": { "WireMockUrl": "http://192.168.1.78:8091", "RealApiMarketplaces": [] }
```
> `Host=192.168.1.78` ÇALIŞMAZ (postgres LAN'a kapalı). Tünel + `Host=localhost` kullan.
> `DevMode:WireMockUrl` doluyken local debug de pazaryeri çağrılarını server WireMock'a yollar (gerçek key gerekmez). Gerçek API istiyorsan boş bırak.

Çalıştır:
```bash
cd Application/Entegrasyon.MVC && dotnet run   # http://localhost:5100
```

---

## Notlar
- Dev DB'ler `IntegrationDb_Dev` + `AdminPanelDb_Dev` — local `IntegrationDb` ve stage'den **bağımsız**. Birinde yapılan değişiklik diğerini etkilemez.
- WireMock stub'ları: `docs/wiremock/{mappings,__files}` (tek kaynak; dev-deploy otomatik sync'ler). Eksik/yanlış stub → `:8091/__admin/requests`'te eşleşmeyen isteği gör, mapping ekle. Doc-fidelity (geçerli→doc-doğru 200, geçersiz→gerçek API gibi 4xx) iteratif yükseltilir.
