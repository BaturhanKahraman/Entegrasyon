# Sipariş Fulfillment Akışı (Fatura + E-Fatura + Kargo) — Araştırma & Tasarım Spec

**Tarih:** 2026-06-10
**Hazırlayan:** PA (Product Analyst) Agent
**Durum:** Taslak — TL Onayı Bekleniyor
**İlgili:** SWE (otomatik tetikleme + UI wiring), Designer (fulfillment/SLA UI), DB Master (Invoice ilişkileri), QA, DevOps (mock)
**Kaynak:** Kullanıcı stratejik isteği (TL, Agenda Konu 3) + 2 paralel kod-önce keşif + 1 web sektör-standardı araştırması

---

## 1. Problem

> *"Standart fulfillment akışını çıkar: satıldı → sipariş → fatura/e-fatura → kargo etiketi → kargoya verildi → takip. Zincirin neresi var, neresi kopuk?"*

Bir pazaryeri satıcısının siparişi tek panelden uçtan uca yönetebilmesi gerekiyor. Yasal zorunluluk (e-fatura/e-arşiv) + termin baskısı (geç kargo = ceza) bu akışı kritik yapıyor.

---

## 2. Sektör Standardı (web araştırması — referans)

**Standart akış:** Created → Picking (hazırla) → Invoiced (fatura) → Shipped (kargoya ver, kargo okutunca OTOMATİK) → Delivered (otomatik). Dallar: iptal / iade / **paket bölme** (kısmi tedarik-edilemez → yeni paket+barkod, orderNumber korunur).

**E-fatura/e-arşiv (GİB):** e-Fatura = alıcı da mükellefse (kurumsal); e-Arşiv = nihai tüketici (bireysel, çoğunluk). **2026'dan itibaren bilanço esasına tabi mükelleflerde tutar sınırı kalktı** — her fatura elektronik. **Faturayı SATICI keser**, pazaryerine yükler + müşteriye iletir. Alıcı tipine göre (VKN/TCKN + GİB mükellef sorgusu) otomatik tür seçimi beklenir.

**Kargo:** Anlaşmalı kargo (pazaryeri barkodu, yaygın) vs seller-pays (kendi kargo). Etiket satıcı tarafında basılır (PDF/ZPL/PNP, tekli+toplu). Takip no anlaşmalıda pazaryerinden gelir, seller-pays'te `updateTrackingNumber` ile bildirilir. **SLA: Hepsiburada 2 iş günü** (geç = ceza + skor düşüşü), N11 ~2 iş günü.

**Rakip standardı (Entegra/Sentos/Ticimax):** toplu faturalandırma · toplu kargo etiketi (çok format) · otomatik e-fatura/e-arşiv · kural-tabanlı kargo seçimi/karşılaştırma · tek panel sipariş · SLA/termin uyarısı.

**İdeal (Nebim/OMS-WMS):** sipariş havuzu + akıllı depo yönlendirme + paket bölme + SLA panosu + toplu op + otomatik takip senkronu.

---

## 3. Mevcut Durum — Ne Var, Ne Eksik (kod-önce teyit)

### 3.1 Fatura / E-Fatura zinciri

| Adım | Durum | Kanıt |
|---|---|---|
| EInvoice altyapısı (CRUD + MVC UI) | ✅ | `EInvoiceManager.cs:1-303`, `InvoiceController.cs:1-187`, e-arşiv/e-fatura enum |
| Müşteri tipi (Retail/Corporate) | ✅ | `RetailCustomer.NationalIdentity` / `CorporateCustomer.TaxNumber` |
| Trendyol e-fatura API + polling (5dk) | ✅ | `TrendyolEFaturaService.CreateInvoiceForOrderAsync`, `TrendyolEFaturaStatusPollingService.cs` |
| **Order → Invoice OTOMATİK** | ❌ YOK | `OrderManager.cs` import loop'unda fatura tetiği yok |
| **Sale (POS) → Invoice OTOMATİK** | ❌ YOK | `SaleManager.cs` fatura çağrısı yok |
| **Müşteri tipine göre otomatik e-arşiv/e-fatura seçimi** | ❌ YOK | manuel (enum var, binding yok) |
| Genel e-fatura GİB gönderimi | ⚠️ PLACEHOLDER | `ParasutInvoiceClient.SendInvoice` "henüz implemente edilmedi" |
| Pazarama/Pttavm/Çiçeksepeti invoice upload | ⚠️ MANUEL | endpoint var, otomatik loop yok |
| **İade faturası** | ❌ YOK | `SaleReturn.cs` Invoice FK yok |
| POS fiş → e-belge | ❌ YOK | thermal print var, e-fatura değil |

### 3.2 Kargo zinciri

| Adım | Durum | Kanıt |
|---|---|---|
| Kargo client'ları (Aras/Sürat/Yurtiçi) | ✅ TAM | `Concrete/Kargo/*`, etiket+takip+iptal metodları |
| Etiket/barkod oluşturma (servis) | ✅ | `ArasKargoService.CreateShipmentAsync`, Sürat `GetShipmentLabelAsync`, Trendyol `GetShippingLabelAsync:92` |
| Takip adapter + polling (30dk) + /shipping UI | ✅ | `Shipping/*TrackingAdapter`, `ShipmentStatusUpdateService`, `ShippingController` |
| Trendyol pazaryeri kargo bilgisi import | ✅ | `OrderManager.cs:474-494` CargoProviderName/TrackingNumber/Link |
| **Kargo etiketi indirme UI** | ❌ YOK | `GetShippingLabel`/`GetBarcode` servis var, UI'da expose YOK |
| **Esnaf tracking number girme UI** | ❌ YOK | controller'da input yok |
| **Trendyol'a "kargolandı" bildirimi** | ❌ TETİK YOK | `UpdateTrackingNumberAsync:71` var ama UI'dan hiç çağrılmıyor |
| **N11 kargo bilgisi çekimi** | ❌ YOK | N11 import'unda cargo alanları boş |
| **Pazarama kargo bilgisi** | ⚠️ DTO var, kaydedilmiyor | `PazaramaCargoDto` var ama `OrderManager.cs:155-295` import'da `item.Cargo` okunmuyor |
| Toplu kargo etiketi | ❌ YOK | tekil bile UI'da yok |
| SLA/termin panosu | ❌ YOK | kalan-süre/geciken-sipariş uyarısı yok |

### 3.3 Esnafın yapabildiği vs yapamadığı

**✅ Yapabiliyor:** siparişleri tek listede gör (`/sales`), Trendyol "tedarik edilemez" işaretle, kargo durumunu takip et (`/shipping`), manuel e-fatura oluştur+gönder.
**❌ Yapamıyor:** kargo etiketi bas, tracking no gir, "kargolandı" bildir, otomatik fatura, müşteri-tipi fatura seçimi, iade faturası, toplu fatura/etiket, SLA takibi, N11/Pazarama kargo bilgisi.

---

## 4. Önerilen Yaklaşım (öncelikli boşluklar)

Mevcut backlog'da **#27 (Sipariş→Kargo Etiketi)** ve **#28 (Trendyol E-Fatura Toplu UI)** task'ları zaten var ama "durum teyidi gerekli" notuyla belirsizdi — bu spec o teyidi yaptı, ikisi kod-önce kanıtla güncellendi. Yeni boşluklar F1-F5:

- **F1 (HIGH):** Sipariş→fatura **otomatik tetikleme** + müşteri tipine göre e-arşiv/e-fatura seçimi (Order/Sale → taslak fatura, VKN/TCKN'e göre tür).
- **F2 (HIGH):** N11 kargo bilgisi çekimi + Pazarama cargo import kaydı (veri boşluğu — sipariş import-side).
- **F3 (MEDIUM):** Genel e-fatura GİB gönderimi (Parasut/entegratör tamamla) + Pazarama/Pttavm/Çiçeksepeti otomatik invoice upload.
- **F4 (MEDIUM):** İade faturası (SaleReturn → iade e-fatura/e-arşiv).
- **F5 (MEDIUM):** SLA/termin panosu (kalan kargo süresi, geciken siparişler — Hepsiburada 2 iş günü ceza eşiği).
- **(MEDIUM, #27 kapsamı genişlet):** toplu kargo etiketi (PDF/ZPL) + fatura-barkod birleşik çıktı.
- **(LOW, F8):** POS satış e-arşiv fişi.

---

## 5. Kabul Kriterleri

1. **(F1)** Sipariş/satış oluşunca otomatik fatura taslağı oluşur; bireysel müşteri → e-arşiv, kurumsal (VKN'li) → e-fatura otomatik seçilir.
2. **(#27)** Esnaf sipariş detayından kargo firması seçip gönderi oluşturur, etiket PDF indirir, tracking no kaydolur; Trendyol siparişinde takip no Trendyol'a bildirilir.
3. **(F2)** N11 ve Pazarama siparişlerinde kargo firma + takip no bilgisi import'ta kaydolur (NULL kalmaz).
4. **(F3)** Genel e-fatura GİB'e gönderilir (placeholder kalkar); diğer pazaryerlerine fatura otomatik yüklenir.
5. **(F4)** İade işleminde iade faturası oluşur.
6. **(F5)** Esnaf geciken/risk altındaki siparişleri SLA panosunda görür (kalan süre, ceza uyarısı).

---

## 6. Rol Dağılımı

| Bulgu | Sahip |
|---|---|
| F1 Otomatik fatura + tür seçimi | SWE + DB Master (Order↔Invoice ilişki) + QA |
| #27 Kargo etiketi/tracking UI | SWE + Designer + QA |
| F2 N11/Pazarama kargo import | SWE + DevOps (mock) + QA |
| F3 GİB gönderimi + pazaryeri upload | SWE + DevOps + QA |
| F4 İade faturası | SWE + DB Master + QA |
| F5 SLA panosu | Designer + SWE + DB Master (geciken-sipariş sorgu/index) |
| Toplu fatura/etiket | SWE + Designer |

---

## 7. Açık Sorular (TL/Kullanıcı)

1. **F1 tetikleme:** sipariş gelince fatura **anında otomatik mi** kesilsin yoksa "taslak oluştur, esnaf onaylayıp kessin" mi? (Yanlış fatura riski → onaylı daha güvenli.)
2. **F3 entegratör:** genel e-fatura için hangi sağlayıcı — Paraşüt mü, GİB özel entegratör mü, Trendyol e-Faturam mı? (Maliyet/sözleşme kararı.)
3. **F5 SLA:** termin eşikleri tenant başına config mi (pazaryeri SLA'ları değişir)?
4. **Paket bölme** (kısmi tedarik-edilemez) ilk müşteri için gerekli mi, yoksa V2 mi?

---

## 8. Notlar

- Tüm ✅/❌ 2 paralel kod-önce keşif + web sektör araştırmasına dayanır.
- Mock ihtiyaçları (N11/Pazarama kargo, e-fatura) Konu 5 + WireMock T2 ile kesişir.
- Bu spec sadece araştırma+plan. #27/#28 güncellendi; F1-F5 + F8 `tasks.json`'a eklendi.
