# Çok-Platform Senkronizasyon — Araştırma & Tasarım Spec

**Tarih:** 2026-06-10
**Hazırlayan:** PA (Product Analyst) Agent
**Durum:** Taslak — TL Onayı Bekleniyor
**İlgili:** SWE (event/manager düzeltmeleri), DB Master (concurrency/atomiklik), QA (race/idempotency testleri), DevOps (Amazon/Temu mock)
**Kaynak:** Kullanıcı stratejik isteği (TL üzerinden, Agenda Konu 1) + 3 paralel kod-önce keşif + PA çelişki teyidi

---

## 1. Problem (Kullanıcının Çekirdek Değer İsteği)

> *"Bir platformda olan biten diğer tüm platformlara yansımalı. E-ticaret satışı / fiziksel mağaza satışı / elle stok düşme → diğer tüm pazaryeri + storefront stokları otomatik güncellensin."*

Bu platformun **çekirdek değer vaadi**: Esnaf 10 ayrı panelde stok kovalamasın. Bir yerde stok değişince her yere otomatik yansısın; **overselling olmasın** (son ürün iki yere birden satılmasın).

---

## 2. Mevcut Durum — Ne Var, Ne Eksik (kod-önce teyit)

> **GENEL DEĞERLENDİRME:** Senkronizasyon altyapısı **şaşırtıcı derecede olgun** — merkezî event + atomik stok düşme + idempotency + advisory lock VAR. Eksikler **belirli kopuk uçlar** ve **2 pazaryeri** ile sınırlı. Sıfırdan kurulum DEĞİL, **boşluk kapatma** işi.

### ✅ VAR — sağlam altyapı (reuse)

| Bileşen | Yer (dosya:satır) | Notlar |
|---|---|---|
| Merkezî event | `Business/Channels/Events/Products/StockPriceChangedEvent.cs:7-18` | BaseEvent (TenantId, EventId, OccurredAt) + ProductVariantId, ProductId |
| EventChannel altyapısı | `Business/Channels/EventChannel.cs:5-27` | `System.Threading.Channels` bounded (1000), DropOldest, singleton |
| Tek-nokta stok yönetimi | `OfficeStockManager.cs` — `DecreaseStockAtomicAsync:102`, `IncreaseStockAtomicAsync:179`, `TransferStockAsync:218`, `CheckStockLevelsAsync:378` | Her stok değişimi → `CheckStockLevelsAsync` → `stockPriceChannel.TryPublish` (:388) |
| **Atomik stok düşme (race-safe)** | `OfficeStockManager.cs:102-141` | `ExecuteUpdateAsync` + `WHERE CurrentStock >= quantity` → tek SQL, affected==0 ise yetersiz. Integration test pass (`OfficeStockManagerIntegrationTests.cs:82-117`) |
| Single source of truth | `Entity/Products/BranchOfficeStock.cs` + config `:16-17` | `CurrentStock = FirstTotalStock - SoldQuantity` (PostgreSQL computed) |
| Idempotency (duplicate-order) | `OrderManager.cs` — Trendyol `:442-479` (ShipmentPackageId), N11 `:314-337`, Pazarama `:171-196` (OrderNumber) | Mükerrer sipariş → sadece status update, stok 2. kez düşmez |
| Eşzamanlı-import kilidi | `OrderManager.cs:86-102` | `pg_try_advisory_lock` (pazaryeri başına ayrı key) |
| Event yayan tetikleyiciler | Storefront checkout `CheckoutManager.cs:120`, POS `SaleManager.cs:51`, manuel `StockMovementController.cs:48`, pazaryeri sipariş `OrderManager.cs:269/402/563`, transfer, iade | Çoğu tetikleyici sync'e bağlı ✅ |
| Pazaryeri stok/fiyat sync servisleri | 6/8 çalışıyor — Trendyol (event-driven anlık), Pazarama (event+15dk polling), N11/Hepsiburada/Pttavm (15dk polling), Çiçeksepeti (5dk polling) | Hepsi sadece `Status==Published` ürünleri gönderir |
| Storefront stok okuma | `CartManager.cs:68,133` | `BranchOfficeStocks.Sum(CurrentStock)` — canlı DB, gecikmesiz |
| Multi-tenant izolasyon | `TenantDbContextFactory`, `TenantProvisioningService`, connection-per-tenant | **DB-per-tenant** → her tenant ayrı DB; stok verisi doğal izole (TenantId kolonu GEREKMEZ, doğru tasarım) |

### ❌ EKSİK / 🔴 RİSK — iş burada

| # | Bulgu | Durum | Kanıt / Etki |
|---|---|---|---|
| **R1** | **Storefront sipariş İPTALİ stok event'i yaymıyor** | 🔴 KOPUK | `OrderManager.cs:676-683` doğrudan `stock.SoldQuantity -= item.Quantity` yazıyor; `CheckStockLevelsAsync` çağrılmıyor → iptal sonrası artan stok **pazaryerlerine YANSIMIYOR**. Müşteri iptal eder, Trendyol'da stok artmaz → eksik satış fırsatı. |
| **R2** | **Storefront checkout — overselling/stoksuz-sipariş bug'ı** | 🔴 BUG | `CheckoutManager.cs:40` memory stok kontrolü TÜM depo toplamına bakıyor (`Sum(CurrentStock)`), ama `:120` atomik düşme `branchOfficeId: 1` **HARDCODED** + **dönüş değeri kontrol EDİLMİYOR** (`await ... ;` result atılıyor). Sonuç: depo-1'de stok yoksa (başka depoda varsa) memory kontrol geçer, atomik düşme sessizce affected==0 olur, **sipariş stoksuz oluşur**. Ayrıca kontrol-düşme arası window. |
| **R3** | **Amazon stok/fiyat sync implementasyonu YOK** | 🔴 TODO | `AmazonStockPriceSyncService.cs:50-52` — feed gönderimi TODO yorumu, sadece log. Amazon'a stok/fiyat HİÇ gitmiyor. |
| **R4** | **Temu stok/fiyat sync servisi YOK** | 🔴 EKSİK | Temu için sadece category importer var; stok/fiyat sync servisi hiç register edilmemiş (`AddBackgroundServices`). |
| **R5** | **Fiyat-only değişiklik anlık event yaymıyor** | ⚠️ KISMİ | `StockPriceChangedEvent` adına rağmen YALNIZCA `OfficeStockManager` (stok) yayıyor; `ProductVariantManager` fiyat güncellemesi event yaymıyor. Esnaf panelden fiyat değiştirince anlık pazaryeri sync YOK — sadece 15dk/5dk polling yansıtır (Trendyol event-driven olduğundan fiyat değişikliği Trendyol'a anında gitmez). |
| **R6** | **Eventual-consistency overselling penceresi** | ⚠️ TASARIM | Event async; POS satışı → event → background sync arası birkaç sn. Bu pencerede pazaryerinde eski stok görünür. Tasarımsal, sıfırlanamaz ama daraltılabilir (kabul edilebilir mi → TL kararı). |

> **NOT (kod-önce kuralı kurtardı):** Keşif agent'larından biri "multi-tenant izolasyon eksik, TenantId yok, overselling riski" dedi — **YANLIŞ**. Proje DB-per-tenant; TenantId kolonu gerekmez. PA teyit etti, risk listesine ALINMADI.

---

## 3. Önerilen Yaklaşım

Hepsi **boşluk kapatma** — mevcut sağlam altyapıya bağlanır. Sıfırdan mimari yok.

- **R1/R2 (HIGH):** Storefront stok yollarını `OfficeStockManager` atomik API'sine tam bağla — iptal → `IncreaseStockAtomicAsync` (event yayar); checkout → atomik düşme sonucunu KONTROL et, çoklu-depo dağıtımını ele al, hardcoded `branchOfficeId:1` yerine pazaryeri-depo eşlemesi/öncelik. Böylece overselling kapanır + iptal pazaryerlerine yansır.
- **R3/R4 (MEDIUM):** Amazon feed sync'i tamamla (JSON_LISTINGS_FEED) + Temu stok/fiyat sync servisi ekle (diğer 6'nın desenini izle). WireMock mock'larıyla (Konu 5 ile kesişir) doğrulanır.
- **R5 (MEDIUM):** Fiyat değişikliğinde de event yay — `ProductVariantManager` fiyat update'inde `StockPriceChangedEvent` (veya yeni `PriceChangedEvent`) publish. Esnaf fiyat değiştirince anlık tüm pazaryerine yansır.
- **R6 (kabul/karar):** Eventual-consistency penceresi — daraltma (event-driven sync'i tüm pazaryerlerine yay, polling'i fallback yap) maliyetli; ilk müşteri için kabul edilebilir mi TL kararı.

---

## 4. Kabul Kriterleri

1. **(R1)** Storefront müşterisi siparişini iptal edince stok geri yüklenir VE pazaryeri stok sync'i tetiklenir (event yayılır, ilgili pazaryerlerine push gider).
2. **(R2)** Depo-1'de stoğu olmayan ama başka depoda olan ürün için storefront checkout: ya doğru depodan atomik düşer ya da net "yetersiz stok" döner; **stoksuz sipariş oluşmaz**. Atomik düşme başarısızsa sipariş oluşturulmaz (sonuç kontrol edilir).
3. **(R3)** Amazon'a stok/fiyat değişikliği gerçekten gönderilir (feed submit), TODO kalkar.
4. **(R4)** Temu için stok/fiyat sync servisi çalışır; published Temu ürünleri güncellenir.
5. **(R5)** Esnaf panelden ürün fiyatı değiştirince anlık event yayılır ve pazaryeri sync'i tetiklenir (15dk beklemeden).
6. **(genel)** Mevcut atomik düşme / idempotency / advisory-lock davranışı bozulmaz (regresyon yok).

---

## 5. Manuel Test Adımları (özet — task'larda detaylı)

1. Storefront'tan sipariş ver, sonra iptal et → ilgili pazaryeri stok-sync log'unda artışın yansıdığını doğrula (R1).
2. Bir variant'ı depo-1=0, depo-2=5 yap; storefront'tan 1 adet al → ya doğru depodan düşsün ya net hata; stoksuz sipariş oluşmasın (R2).
3. Yayınlanmış Amazon ürününün stoğunu değiştir → Amazon feed gönderiminin gerçekleştiğini doğrula (R3).
4. Yayınlanmış Temu ürününün stoğunu değiştir → Temu sync'in çalıştığını doğrula (R4).
5. Esnaf panelden bir ürünün fiyatını değiştir → anlık event + pazaryeri push tetiklendiğini (15dk beklemeden) doğrula (R5).

---

## 6. Rol Dağılımı

| Bulgu | Sahip | İş |
|---|---|---|
| R1 Storefront iptal event'i | **SWE** + **QA** | `OrderManager` storefront-cancel'ı `IncreaseStockAtomicAsync`'e bağla; TDD |
| R2 Checkout overselling | **SWE** + **DB Master** (atomiklik/depo dağıtımı) + **QA** (race testi) | Atomik sonuç kontrolü + çoklu-depo + hardcoded branch fix |
| R3 Amazon feed sync | **SWE** + **DevOps** (mock) | JSON_LISTINGS_FEED submit implement |
| R4 Temu sync servisi | **SWE** + **DevOps** (mock) | Yeni HostedService (6'nın deseni) |
| R5 Fiyat event'i | **SWE** + **QA** | `ProductVariantManager` fiyat update'inde publish |
| R6 Consistency penceresi | **TL kararı** | Kabul / daraltma stratejisi |

---

## 7. Açık Sorular (TL/Kullanıcı)

1. **R2 çoklu-depo:** Storefront hangi depodan düşmeli — sabit "storefront deposu" mu, yoksa pazaryeri-depo eşlemesi gibi öncelik sırası mı? (Şu an hardcoded branch 1.)
2. **R6:** Eventual-consistency penceresi ilk müşteri için kabul edilebilir mi, yoksa tüm pazaryerlerini event-driven anlık sync'e mi geçirelim (maliyetli)?
3. **R5 event tipi:** Mevcut `StockPriceChangedEvent`'i fiyat için de mi kullanalım (adı zaten "Price" içeriyor) yoksa ayrı `PriceChangedEvent` mi?

---

## 8. Notlar

- Tüm ✅/❌ 3 paralel kod-önce keşif + PA çelişki teyidine dayanır. İki keşif çelişkisi (checkout atomiklik, multi-tenant) PA tarafından koddan çözüldü.
- R3/R4 (Amazon/Temu) Konu 5 (API+mock envanteri) ve WireMock task'larıyla kesişir — DevOps koordinasyonu.
- Bu spec sadece araştırma+plan. İlgili task'lar `tasks.json`'a S1-S5 olarak eklendi.
