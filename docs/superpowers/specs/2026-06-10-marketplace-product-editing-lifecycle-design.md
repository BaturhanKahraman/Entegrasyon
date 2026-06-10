# Pazaryeri Ürün Düzenleme & Yaşam Döngüsü — Araştırma & Tasarım Spec

**Tarih:** 2026-06-10
**Hazırlayan:** PA (Product Analyst) Agent
**Durum:** Taslak — TL Onayı Bekleniyor
**İlgili:** SWE (update servisleri + endpoint), Designer (yönetim UI), QA, DevOps (mock)
**Kaynak:** Kullanıcı stratejik isteği (TL, Agenda Konu 2) + 2 paralel kod-önce keşif

---

## 1. Problem

> *"Sadece EKLEME değil — gönderilmiş ürünün her platformda DÜZENLENMESİ: fiyat/stok/açıklama/durum güncelle, yayından kaldır."*

Esnaf ürünü pazaryerine gönderir, sonra fiyatını/açıklamasını değiştirir veya tamamen geri çekmek ister. Bugün bunu tek panelden yapamıyor → eski derdine (her panele ayrı girme) geri dönüyor. **Yaşam döngüsü yönetimi** çekirdek değer.

---

## 2. Mevcut Durum — Ne Var, Ne Eksik (kod-önce teyit)

> **DEĞERLENDİRME:** İçerik güncelleme **yarım** (bazı pazaryeri tam, bazısı hiç yok). Yaşam döngüsü (yayından kaldır/arşivle) **çoğunlukla eksik** — okuma var, yazma yok. Durum izleme (polling/red bildirimi/retry) **sağlam**.

### Stok/Fiyat güncelleme
Konu 1'de ele alındı — sync servisleri çalışıyor (Amazon TODO, Temu yok, fiyat-event eksik → S3/S4/S5).

### İçerik güncelleme (başlık/açıklama/görsel/attribute) — pazaryeri başına

| Pazaryeri | Add | Update | İçerik-update tetikleme | Kanıt |
|---|:--:|:--:|---|---|
| Trendyol | ✅ | ✅ `UpdateUnapprovedProductAsync` + `UpdateApprovedContentAsync` (ContentId) | ❌ manuel | `TrendyolProductService.cs:182,271` |
| Çiçeksepeti | ✅ | ✅ `UpdateProductAsync` (PUT) | ❌ manuel | `CiceksepetiProductService.cs:110` |
| Amazon | ✅ | ✅ (`UpdateProductAsync`=Publish) | ❌ manuel | `AmazonProductService.cs:110` |
| N11 | ✅ | ⚠️ `UpdateProductBasicAsync` (fiyat/stok/açıklama; **title belirsiz**) | ❌ manuel | `N11ProductService.cs:198` |
| Pttavm | ✅ | ⚠️ `UpsertProductsAsync` (gerçek-update tracing yok) | ❌ manuel | `PttavmProductService.cs:22` |
| **Hepsiburada** | ✅ | ❌ **YOK** | ❌ | `HepsiburadaProductService.cs:31` (sadece Publish/CheckStatus) |
| **Pazarama** | ✅ | ❌ **YOK** | ❌ | `PazaramaProductService.cs:24` (sadece Publish/CheckBatchStatus) |

- ✅ **Trendyol approved-update (ContentId) TAM** — `ProductMarketplace.ContentId`/`IsApproved` izleniyor, `TrendyolProductStatusSyncService` (5dk) çekiyor.
- ❌ **Otomatik tetikleme YOK:** `ProductManager.cs:306` `ProductUpdatedEvent` yayıyor ama `ProductUpdatedNotificationHandler` sadece UI bildirimi yapıyor; **pazaryeri update'i tetikleyen handler yok**. İçerik değişince elle "senkronize et" gerekiyor (`ProductSyncManager.SyncProductAsync` manuel var).

### Yaşam döngüsü (yayından kaldır / arşivle / durum)

| Yetenek | Durum | Kanıt |
|---|---|---|
| **Yayından kaldırma (Trendyol)** | ⚠️ Servis + UI button/modal VAR ama **controller endpoint YOK** → çalışmıyor | `ITrendyolProductService.DeleteProductAsync():27`; `TrendyolDetail.cshtml:195-201` + `_TrendyolRemoveConfirm.cshtml:40` POST `/trendyol/remove` → action yok |
| **Yayından kaldırma (Hepsiburada)** | ❌ Servis (`DeactivateListingAsync`) VAR ama **UI/controller hiç yok** | `HepsiburadaListingService.cs:169-189` |
| **Arşivleme** | ⚠️ `IsArchived` sadece **OKUMA** (polling çekiyor); kullanıcı arşivleyemez | `ProductMarketplace.cs:32`; `TrendyolProductStatusSyncService.cs:87` |
| **Durum polling + red bildirimi** | ✅ Trendyol/Hepsiburada polling; Rejected → `StatusMessage` + notification + retry | `MarketplaceProductRejectedNotificationHandler`; `ProductSyncManager.cs:234` |
| **Retry (toplu)** | ✅ `SyncAllPendingAsync`, `RetryAllFailedAsync` | `ProductSyncManager.cs:264-374` |
| **Toplu yayından kaldır/delist** | ❌ YOK | `BulkOperationType.cs` sadece Import/Export |

### Esnafın UI'da YAPABİLDİĞİ vs YAPAMADIĞI

**✅ Yapabiliyor:** Trendyol'a gönder (override formuyla), reddedilen/başarısızı retry, sync durumu + timeline görüntüle, durum filtreleme.
**❌ Yapamıyor:** ürünü pazaryerinden **kaldırma** (endpoint yok), **arşivleme** (yazma yok), **unpublish/delist** (Hepsiburada UI yok), **toplu kaldırma**, **Hepsiburada/Pazarama içerik düzenleme** (update servisi yok), **Trendyol dışı pazaryerlerinde herhangi bir yönetim** (UI sadece Trendyol — Konu E5 ile aynı boşluk).

---

## 3. Önerilen Yaklaşım

- **D1 (HIGH):** Yayından kaldırma akışını tamamla — Trendyol controller endpoint'i ekle (servis+UI hazır, sadece action eksik), Hepsiburada delist UI+action. Esnaf ürünü geri çekebilsin.
- **D2 (HIGH):** Hepsiburada + Pazarama içerik UPDATE servisi ekle — gönderilmiş ürün düzenlenebilsin (şu an sadece yeni publish). N11 title + Pttavm gerçek-update netleştir.
- **D3 (MEDIUM):** İçerik değişince otomatik pazaryeri güncelleme — `ProductUpdatedEvent`'i tüketen marketplace-update handler (published ürünlerde içerik değişince pazaryerine push). S5 (fiyat event) ile aynı desen.
- **D4 (MEDIUM):** Arşivleme YAZMA aksiyonu (`IsArchived` set + pazaryeri archive API) + toplu unpublish/delist (`BulkOperationType` genişlet).
- **D5 (LOW):** N11 title güncelleme + Pttavm upsert gerçek-update tracing netleştir.

> **KESİŞİM (önemli):** D1/D2 ve **E5 (override UI'ı 8 pazaryerine yay)** aynı UI'a yakınsar. Önerilen: tek **generic "pazaryeri ürün yönetim" sayfası** — gönder + düzenle + yayından kaldır + durum, pazaryeri-parametrik. E5 ile birlikte tasarlanmalı (Designer).

---

## 4. Kabul Kriterleri

1. **(D1)** Esnaf Trendyol'a gönderdiği ürünü panelden "yayından kaldır" ile geri çekebilir (endpoint çalışır, pazaryerinde silinir/pasifleşir). Hepsiburada için de delist çalışır.
2. **(D2)** Hepsiburada ve Pazarama'da gönderilmiş ürünün başlık/açıklaması güncellenip pazaryerine yansır (yeni-publish değil, update).
3. **(D3)** Published ürünün içeriği panelden değişince otomatik (veya tek tık) pazaryeri güncellemesi tetiklenir.
4. **(D4)** Esnaf ürünü arşivleyebilir; birden çok ürünü topluca yayından kaldırabilir.
5. **(genel)** Mevcut durum-polling/red-bildirimi/retry bozulmaz.

---

## 5. Manuel Test Adımları (özet)

1. Trendyol'a gönderilmiş ürünü panelden "yayından kaldır" → Trendyol'da pasifleştiğini/silindiğini (WireMock request) ve UI'da durumun güncellendiğini doğrula (D1).
2. Hepsiburada'da gönderilmiş ürünün açıklamasını değiştir → Hepsiburada'ya update gittiğini doğrula (D2).
3. Published Trendyol ürününün başlığını panelden değiştir → güncellemenin pazaryerine yansıdığını doğrula (D3).
4. Bir ürünü arşivle + 3 ürünü topluca yayından kaldır → hepsinin pazaryerinde işlendiğini doğrula (D4).

---

## 6. Rol Dağılımı

| Bulgu | Sahip | İş |
|---|---|---|
| D1 Yayından kaldırma endpoint+UI | **SWE** (controller action) + **Designer** (Hepsiburada UI) + **QA** | Trendyol action, Hepsiburada delist UI |
| D2 Hepsiburada/Pazarama update servisi | **SWE** + **DevOps** (mock) + **QA** | Update endpoint + mapper |
| D3 Otomatik içerik-güncelleme tetikleme | **SWE** + **QA** | ProductUpdatedEvent → marketplace-update handler |
| D4 Arşivleme yazma + toplu delist | **SWE** + **Designer** + **DB Master** (BulkOperationType) | |
| D5 N11 title / Pttavm update | **SWE** | Kapsam netleştir |
| Generic yönetim UI (D1/D2 + E5) | **Designer** + **SWE** | Tek pazaryeri-parametrik sayfa |

---

## 7. Açık Sorular (TL/Kullanıcı)

1. **D1 "yayından kaldırma" semantiği:** gerçek delete/delist mi, yoksa stok-0 + pasif mi (pazaryerine göre farklı; Trendyol archive vs delete)?
2. **D3 tetikleme:** otomatik mi (içerik değişince anında push) yoksa "değişiklik var, güncelle?" onaylı mı? (S5/E1 önizleme ile tutarlı olmalı.)
3. **Generic UI:** D1/D2/D4 ile E5'i tek "pazaryeri ürün yönetim" sayfasında birleştirelim mi (önerilen) yoksa ayrı mı?

---

## 8. Notlar

- Tüm ✅/❌ 2 paralel kod-önce keşfe dayanır.
- D2/D1 mock ihtiyacı Konu 5 (API+mock envanteri) + WireMock task'larıyla kesişir.
- Bu spec sadece araştırma+plan. Task'lar `tasks.json`'a D1-D5 olarak eklendi.
