# Spec: Marketplace Sync — Eksik Credential Disabled Durumu

**Tarih:** 2026-06-11  
**Durum:** TL onayı bekleniyor  
**Bağlı task:** "Marketplace Sync — Eksik API Credential Durumunda Disabled Kart + Popover"

---

## Problem

`/marketplace/sync` (Overview) sayfasında tüm pazaryeri kartları eşit şekilde görünüyor. API anahtarları yapılandırılmamış bir pazaryerinde "Ürünleri Gör", "Kategori Eşleştir", "Marka Eşleştir" butonları tıklanabilir durumda — bu boş/hatalı ekranlara veya API error'larına yol açıyor.

Esnafın beklentisi: hangi pazaryerinin hazır, hangisinin kurulum gerektirdiği tek bakışta anlaşılsın.

---

## Credential Tespit Mantığı

`MarketPlace.cs` entity'si incelendi. Mevcut `Settings/Views/Integrations.cshtml` aynı mantığı kullanıyor (`!string.IsNullOrEmpty(mp.ApiKey) && !string.IsNullOrEmpty(mp.ApiSecret)`).

**Trendyol (mp.Id == 1):** ApiKey + ApiSecret + SellerId üçü de dolu olmalı  
**Amazon/Pazarama OAuth2 (mp.Id == 4 veya 5):** TokenUrl + RefreshToken ikisi dolu olmalı  
**Diğer pazaryerleri:** ApiKey + ApiSecret ikisi dolu olmalı

```csharp
// SWE implement edecek — controller veya servis katmanında private helper
private static bool IsCredentialComplete(MarketPlace mp) =>
    mp.Id is 4 or 5
        ? !string.IsNullOrEmpty(mp.TokenUrl) && !string.IsNullOrEmpty(mp.RefreshToken)
        : !string.IsNullOrEmpty(mp.ApiKey) && !string.IsNullOrEmpty(mp.ApiSecret)
          && (mp.Id != 1 || !string.IsNullOrEmpty(mp.SellerId));
```

---

## Backend Değişikliği

### `MarketplaceSyncCardVm.cs` — `HasCredentials` alanı ekle

```csharp
public class MarketplaceSyncCardVm
{
    public int MarketPlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasCredentials { get; set; }   // YENİ
    public ProductSyncSummaryDto SyncSummary { get; set; } = new(0, 0, 0, 0, 0);
    public int MappedCategoryCount { get; set; }
}
```

### `SyncOverviewController.cs` (veya SyncOverviewManager) — mapping güncelle

Kart oluşturulurken `HasCredentials = IsCredentialComplete(mp)` set edilmeli.

---

## Tasarım: Kart Durumları

| Durum | Görünüm |
|-------|---------|
| HasCredentials=true, IsActive=true | Normal kart (mevcut) |
| HasCredentials=false | Disabled kart (rozet + disabled butonlar + tooltip) |
| HasCredentials=true, IsActive=false | Pasif kart (farklı badge, butonlar disabled) |

### Disabled Kart Tasarım Öğeleri (Designer)

**Kart header:**
- `badge bg-danger-lt` ile "Yapılandırılmamış" rozeti

**Kart body:**
- Sync istatistikleri gizlenebilir veya `—` ile placeholder gösterilir (tüketici karar verir)

**Footer butonlar:**
- `disabled` attribute + `tabindex="-1"` + `cursor-not-allowed` style
- Her butona Tabler Tooltip veya Popover:
  ```
  "Bu pazaryeri için API anahtarları yapılandırılmamış. 
   Ayarlar → Entegrasyonlar sayfasından tamamlayın."
  ```
  + `<a href="/settings/integrations">Entegrasyonlar sayfasına git →</a>` linki

**Opsiyonel:** kart container'a `opacity-75` veya `text-muted` hafif solma efekti

### Pasif (IsActive=false) Durumu
- Credential var ama IsActive=false: badge `bg-secondary-lt "Pasif"`, butonlar disabled, tooltip "Bu pazaryeri şu an pasif durumda."

---

## Rol Dağılımı

| Kim | Ne |
|-----|----|
| **SWE** | `MarketplaceSyncCardVm.HasCredentials` alanı ekle |
| **SWE** | `SyncOverviewController`/`Manager`'da `IsCredentialComplete` helper + mapping |
| **Designer** | `Overview/Index.cshtml` — disabled kart tasarımı: rozet, opacity, disabled butonlar, tooltip/popover |
| **QA** | Trendyol ApiKey boşken kart disabled; doldurulunca aktif olduğunu doğrula |

---

## Kabul Kriterleri

1. `Settings → Entegrasyonlar`'da ApiKey/ApiSecret boş olan pazaryeri için Sync Overview kartında butonlar **disabled**.
2. Disabled butona hover'da/focus'ta tooltip/popover: "API anahtarları eksik…" + /settings/integrations linki.
3. Kart header'ında "Yapılandırılmamış" badge var.
4. Credential dolu pazaryeri kartı etkilenmiyor — mevcut davranış korunuyor.
5. Trendyol özel kural: ApiKey+ApiSecret dolu, SellerId boş → disabled.
6. Amazon/Pazarama özel kural: TokenUrl+RefreshToken boş → disabled.

---

## Manuel Test Adımları

1. `Settings → Entegrasyonlar` → Trendyol ApiKey'ini boşalt → Kaydet.
2. `/marketplace/sync` sayfasını aç — Trendyol kartında "Yapılandırılmamış" badge görünmeli.
3. "Ürünleri Gör" butonuna tıklamayı dene — tıklanamaz (disabled) olmalı.
4. Butona hover yap — tooltip/popover "Bu pazaryeri için API anahtarları yapılandırılmamış..." mesajı görünmeli.
5. Mesajdaki link ile `/settings/integrations` açılmalı.
6. Trendyol: ApiKey + ApiSecret doldurup SellerId boş bırak → kaydet → Sync Overview hâlâ disabled.
7. Trendyol: ApiKey + ApiSecret + SellerId'yi doldur → kaydet → butonlar aktif olmalı.
8. Tam credential dolu başka bir pazaryeri kartı etkilenmemeli — normal görünüyor olmalı.
9. Unit test: `IsCredentialComplete` helper'ı 5 senaryo (trendyol-tam, trendyol-sellerId-eksik, oauth-tam, oauth-refreshToken-eksik, diğer-tam) için test edilmeli.
