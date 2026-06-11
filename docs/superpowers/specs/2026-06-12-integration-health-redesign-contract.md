# Designer Kontratı: Entegrasyon Sağlığı (IntegrationHealth) Redesign

**Tarih:** 2026-06-12
**Backend owner:** SWE-Mehmet (tamamlandı) · **View owner:** Designer
**Bağlam:** #21 audit + #81 (marketplace credential disabled) tutarlılığı

---

## Backend (HAZIR — değiştirme)

`/integrations/health` → `IntegrationHealthController.Index` → `IntegrationHealthVm`:

| Alan | Tip | Anlam |
|---|---|---|
| `RecentSyncCount` | int | Son 24s Marketplace/Sync log sayısı |
| `RecentErrorCount` | int | Son 24s Error log sayısı |
| `LastSyncTime` | DateTimeOffset? | En son sync log zamanı |
| `RecentErrors` | List | Son hata logları (tablo) |
| **`Marketplaces`** | **List\<MarketplaceHealthRow\>** | **YENİ — per-marketplace credential durumu** |

`MarketplaceHealthRow(int MarketPlaceId, string Name, bool HasCredentials)` — `HasCredentials` tek kaynaktan: `MarketPlace.IsCredentialComplete()` (#81 ile aynı kural).

---

## Tasarım İşi (Designer)

Sayfayı **Ürünler dili**ne getir (mevcut KPI kartı + tablo desenleri):

1. **Üst KPI şeridi:** mevcut 3 özet kart (Son 24s Sync / Hata / Son Sync Zamanı) — Ürünler sayfasındaki page-header KPI stiliyle hizala.
2. **Pazaryeri Bağlantı Durumu tablosu** (şu an minimal-fonksiyonel, cila gerekli):
   - Her satır: pazaryeri adı + durum rozeti.
   - `HasCredentials=true` → `badge bg-success-lt "Hazır"` (veya `status status-green` dot).
   - `HasCredentials=false` → `badge bg-danger-lt "Yapılandırılmamış"` + `/settings/integrations`'a "Anahtarları Gir" butonu. **#81'deki disabled-kart badge diliyle BİREBİR** olmalı (tutarlılık).
   - Tabler `status-dot` / `badge` doğrulaması: https://tabler.io/docs/ui/badges, /status.
3. **Son Hatalar tablosu:** mevcut — Ürünler tablo stiliyle hizala.
4. Boş durumlar: pazaryeri yok → mevcut "Tanımlı pazaryeri bulunamadı"; hata yok → mevcut `_EmptyState`.

**Koru:** `@model` tipi + `Model.Marketplaces` alan adları + route. View-only redesign; controller/VM'e dokunma.

## Gelecek (ayrı task — bu kontrata DAHİL DEĞİL)
Per-marketplace **son-sync zamanı / hata sayısı** kırılımı: `ApplicationLog`'da marketplace linkage (`MarketPlaceId`) YOK → log şeması değişikliği gerektirir. Şu an yalnız credential durumu per-mp. DB Master ile ayrı değerlendirilmeli.
