# Offline Satis Yetenegii Plani

**Tarih:** 2026-03-24
**Durum:** Arastirma & Planlama (kod yazilmadi)

---

## 1. Mevcut Durum Analizi

### Uygulama Mimarisi
- **Blazor Server** (.NET 8) — tum UI render'i sunucuda yapilir, istemciye SignalR uzerinden diff gonderilir
- **PostgreSQL** uzak sunucuda (Docker, 5432 portu)
- **MudBlazor** UI framework
- **SignalR** ile real-time bildirimler
- Satis akisi: `Sales.razor.cs` → `SaleManager.MakeSale()` → `OfficeStockManager.DecreaseStockAtomicAsync()` → PostgreSQL

### Mevcut Agent (PrintAgent)
- **Teknoloji:** .NET 8 minimal Web API (`Microsoft.NET.Sdk.Web`)
- **Konum:** `Agent/Entegrasyon.PrintAgent/`
- **Gorev:** Yalnizca barkod/fis yazdirma (ZPL ve ESC/POS)
- **Calisma sekli:** `https://localhost:19100` uzerinden REST API dinliyor
- **Transport'lar:** RawTcpPrinterTransport, WindowsUsbPrinterTransport, CupsPrinterTransport
- **Guvenlik:** API key middleware, CORS ayarlari
- **Contracts:** `PrintAgent.Contracts` projesi — `SaleReceiptData`, `PrintJobRequest` vb. DTO'lar
- **Veritabani yok** — tamamen stateless, konfigurasyonu dosya sisteminden okuyor

### Satis Domain Modeli
- `Sale` → `SaleItem[]` (ProductVariantId, Quantity, UnitPrice, DiscountPercent, TaxPercentage)
- `BranchOfficeStock` — sube bazinda stok (CurrentStock computed column)
- `StockMovement` — her stok hareketinin kaydi
- `ProductVariant` — Barcode, ListPrice, SalePrice, VatRate
- Satis DTO: `MakeSaleDto(SalePersonId, CustomerId, GeneralDiscount, BranchOfficeId, SaleItems)`

### Temel Problem
Blazor Server **mimarigeregiinternet baglantisi olmadan calisamaz**. SignalR circuit koptuğunda UI tamamen erisIlemez hale gelir. Bu, fiziksel magaza ortaminda kabul edilemez bir kesinti olusturur.

---

## 2. Yaklasim Analizleri

### 2.1 PWA Yaklasimi (Blazor WASM + PWA)

#### Teknik Aciklama
Mevcut Blazor Server uygulamasinin satis modulu Blazor WebAssembly olarak yeniden yazilir. Service Worker ile offline caching, IndexedDB ile local data storage saglanir.

#### Mimari
```
[Tarayici]
  Blazor WASM (satis modulu)
  + Service Worker (asset caching)
  + IndexedDB (urunler, stoklar, offline satirlar)
  + Background Sync API (online olunca senkronizasyon)
       |
       | (online olduğunda)
       v
[Sunucu API] → PostgreSQL
```

#### Gerekli Calismalar
1. Yeni bir `Entegrasyon.SalesPWA` Blazor WASM projesi olusturma
2. Satis sayfasini (`Sales.razor` + `Sales.razor.cs`) WASM uyumlu hale getirme
3. Ortak DTO'lari paylasilan bir `Shared` projesine tasima
4. Service Worker implementasyonu (Workbox veya custom)
5. IndexedDB erisimi icin `Blazored.LocalStorage` veya JS interop
6. Sunucu tarafinda REST API endpoint'leri ekleme (mevcut Blazor Server yalnizca SignalR kullaniyor)
7. Senkronizasyon mekanizmasi

#### Avantajlar
- Kurulum gerektirmez — tarayicida calisir
- Otomatik guncelleme (Service Worker update)
- Cross-platform (Windows, Linux, macOS, tablet)
- PWA install ozelligi ile masaustu deneyimi

#### Dezavantajlar
- **Blazor WASM'a tasima buyuk is** — mevcut Business layer server-side, DB erisimi dogrudan EF Core; WASM'da bunlar kullanilamaz
- **Tum API katmani sifirdan yazilmali** — mevcut mimari REST API icermiyor
- **WASM performans sinirlamasi** — buyuk urun kataloglarinda yavas
- **IndexedDB limitleri** — tarayici storage kotasi (genelde 50-100MB)
- **Baski problemi** — Agent'a WASM'dan localhost uzerinden CORS ile erismek sorunlu olabilir
- **Tarayici bagimli** — kullanici tarayiciyi kapatirsa veri kaybolabilir

#### Gelistirme Maliyeti: YUKSEK
- Yeni WASM projesi + API layer + Service Worker + IndexedDB + Sync = ~6-8 hafta

---

### 2.2 Blazor Hybrid (MAUI) Masaustu Uygulamasi

#### Teknik Aciklama
.NET MAUI Blazor Hybrid ile masaustu uygulamasi olusturulur. Mevcut Razor component'lari (`.razor` + `.razor.cs`) dogrudan yeniden kullanilir. SQLite local veritabani ile offline calisir.

#### Mimari
```
[MAUI Masaustu App]
  BlazorWebView (mevcut .razor component'lari)
  + SQLite local DB (urunler, stoklar, offline satislar)
  + SyncEngine (background service)
       |
       | (online olduğunda)
       v
[Sunucu API] → PostgreSQL
```

#### Gerekli Calismalar
1. Yeni `Entegrasyon.SalesPOS` MAUI Blazor Hybrid projesi
2. Mevcut `Sales.razor` / `Sales.razor.cs` component'larini tasima
3. SQLite veritabani + EF Core SQLite provider
4. Offline SaleManager (local SQLite'a yazar)
5. SyncEngine — online olunca PostgreSQL ile senkronize eder
6. Urun/stok/fiyat verilerini local'e indirme mekanizmasi

#### Avantajlar
- **Mevcut Razor component'lari %80+ yeniden kullanilabilir**
- SQLite ile guclu local storage (limit yok)
- EF Core SQLite ile ayni ORM pattern'i (Business layer mantigi tasInabilir)
- Native masaustu performansi
- Yazici erisimi native API uzerinden (Agent'a bile gerek kalmayabilir)

#### Dezavantajlar
- **MAUI yalnizca Windows ve macOS** (Linux destegi resmi olarak yok)
- Kurulum/guncelleme yonetimi gerekir (MSIX, ClickOnce vb.)
- MAUI'nin kendi bug/kararsizlik sorunlari (ozellikle Linux'ta)
- Iki ayri UI kodu dallanmasi riski (server vs. hybrid)
- .NET 8 MAUI olgunluk endIseleri

#### Gelistirme Maliyeti: ORTA-YUKSEK
- MAUI projesi + SQLite + Sync = ~4-6 hafta
- Ancak Linux desteği yoksa kullanici profili ile uyumlu olmayabilir

---

### 2.3 Mevcut Agent'i Genisletme (ONERILEN)

#### Teknik Aciklama
Halihazirda masaustunde calisan `Entegrasyon.PrintAgent` (.NET 8 minimal Web API) genisletilerek offline satis yeteneği eklenir. Agent zaten:
- .NET 8 Web API (Kestrel)
- Masaustunde calisIyor
- API key guvenliği var
- Yazici erisimi var (ZPL + ESC/POS)

Bu agent'a SQLite veritabani, satis endpoint'leri ve senkronizasyon motoru eklenir. Blazor Server UI, Agent'i `https://localhost:19100` uzerinden zaten kullanIyor; internet kesildigi anda Blazor Server erisIlemez olur, ancak Agent bir **fallback web UI** sunabilir veya Blazor Server uygulamasi Agent'in durumunu kontrol edip offline moda gecebilir.

#### Mimari (Iki Alternatif)

**Alternatif A: Agent + Minimal Fallback UI**
```
[Normal Mod - Internet var]
  Blazor Server (tam ozellik) → PostgreSQL
  Agent (yalnizca yazdirma)

[Offline Mod - Internet yok]
  Agent genisletilmis versiyon:
    + Kendi basina calisabilen minimal web UI (Razor Pages veya static HTML+JS)
    + SQLite local DB
    + Satis endpoint'leri
    + Stok sorgu endpoint'leri
    + Fis yazdirma (mevcut)
         |
         | (internet gelince)
         v
    SyncEngine → Sunucu API → PostgreSQL
```

**Alternatif B: Agent + Blazor WASM Embedded**
```
[Agent genisletilmis]
  Kestrel Web Server
    + Blazor WASM static dosyalari serve edilir (wwwroot)
    + REST API endpoint'leri (satis, urun arama, stok)
    + SQLite local DB
    + SyncEngine
    + Yazici erisimi (mevcut)
```

#### Gerekli Calismalar

**Faz 1 — Temel Altyapi (~2 hafta)**
1. `Agent/Entegrasyon.PrintAgent/` projesine EF Core SQLite eklenmesi
2. Local entity'ler: `LocalProduct`, `LocalProductVariant`, `LocalBranchOfficeStock`, `LocalSale`, `LocalSaleItem`
3. Veri indirme endpoint'i: Sunucudan urun katalogu + stok bilgisini SQLite'a cekme
4. `/api/offline/status` endpoint'i — sync durumu, son guncelleme zamani

**Faz 2 — Offline Satis (~2 hafta)**
5. `POST /api/sales` — offline satis olusturma (SQLite'a yazar)
6. `GET /api/products/search?barcode=XXX` — barkod ile urun arama (SQLite'dan)
7. `GET /api/products/search?query=XXX` — isim ile arama
8. `GET /api/stock/{branchOfficeId}` — sube stok durumu
9. Stok dusme mantigi (local SQLite'da atomic)
10. Fis yazdirma entegrasyonu (mevcut `PrintService` kullanilir)

**Faz 3 — Senkronizasyon (~2 hafta)**
11. `SyncEngine` background service — periyodik olarak sunucuya baglanma denemesi
12. Offline satislari sunucuya gonderme (queue-based)
13. Sunucudan guncel urun/stok verisi cekme
14. Conflict resolution (asagida detaylandirildi)

**Faz 4 — UI (~1-2 hafta)**
15. Agent icine minimal satis UI'i ekleme (iki secenek):
    - **Secenek 1:** Razor Pages (server-rendered, Agent icinde) — basit ve hizli
    - **Secenek 2:** Static HTML + Vanilla JS (Agent wwwroot'a konur) — en hafif
    - **Secenek 3:** Blazor WASM (satis component'lari paylasIlir) — en zengin UX ama en fazla is

#### Avantajlar
- **Mevcut altyapi uzerine insa** — Agent zaten calisIyor, masaustunde kurulu, API key mekanizmasi var
- **Ayni teknoloji stack'i** (.NET 8) — ekip ek dil/framework ogrenmez
- **Yazici entegrasyonu hazir** — fis yazdirma icin ek is yok
- **Linux + Windows** — Agent zaten her ikisinde calisIyor (Kestrel + CUPS/Windows transport)
- **Blazor Server'dan bagimsiz** — internet olmasa bile Agent ayakta
- **En dusuk risk** — mevcut sistemi bozmadan, Agent'a ek moduller eklenir
- **Incremental delivery** — her faz bagimsiz test edilebilir

#### Dezavantajlar
- Agent'in UI'i Blazor Server kadar zengin olmayacak (MudBlazor yerine daha sade UI)
- Iki ayri satis akisi yonetilmeli (online: Blazor Server, offline: Agent)
- Agent guncellemesi kullanici makinesinde yapilmali
- SQLite ile PostgreSQL arasinda schema uyumu saglanmali

#### Gelistirme Maliyeti: DUSUK-ORTA
- Mevcut Agent + SQLite + Sync + Minimal UI = ~6-8 hafta (4 faz)
- Ancak her faz bagimsiz deploy edilebilir; Faz 1-2 ile temel offline satis ~4 haftada hazir

---

### 2.4 Electron/Tauri Wrapper

#### Teknik Aciklama
Blazor WASM uygulamasini Electron veya Tauri ile masaustu uygulamasina cevirme.

#### Mimari
```
[Electron/Tauri Shell]
  Blazor WASM
  + Local storage / SQLite (Tauri icin Rust backend)
  + Sync mekanizmasi
```

#### Avantajlar
- Cross-platform (Windows, Linux, macOS)
- Tauri ile kucuk binary boyutu (~10MB vs Electron ~150MB)
- Web teknolojileri ile masaustu deneyimi

#### Dezavantajlar
- **Blazor WASM'a gecis gerekli** — 2.1'deki tum dezavantajlar gecerli
- Electron: buyuk binary, yuksek RAM kullanimi
- Tauri: Rust bilgisi gerekir (backend icin), .NET ekosisteminden kopma
- Agent ile cakisma — iki ayri masaustu uygulamasi (Agent + Electron/Tauri)
- **Mevcut Agent'in yetenekleri zaten bunu kapsiyor** — gereksiz karmasiklik

#### Gelistirme Maliyeti: YUKSEK
- WASM gecisi + Electron/Tauri wrapper + Sync = ~8-10 hafta

---

## 3. Senkronizasyon Stratejisi (Tum Yaklasimlar Icin Gecerli)

### 3.1 Offline'da Gerekli Veriler

| Veri | Boyut Tahmini | Guncelleme Sikligi |
|------|---------------|-------------------|
| Urunler (Product + ProductVariant) | ~5-50K kayit, ~10-50MB | Gunluk |
| Stoklar (BranchOfficeStock) | Sube basina ~5-50K kayit | Saat basI |
| Musteriler (Customer) | ~1-10K kayit | Haftalik |
| Fiyatlar (ListPrice, SalePrice) | Variant icerisinde | Gunluk |
| Barkodlar | Variant icerisinde | Gunluk |
| Indirim kuponlari (DiscountVoucher) | ~100-1K kayit | Gunluk |

**Toplam tahmini:** ~50-100MB (rahat SQLite kapasitesi icerisinde)

### 3.2 Senkronizasyon Modeli: Queue-Based Outbox Pattern

```
[Offline Satis Yapilir]
     |
     v
[SQLite: LocalSale tablosu] ← Status: Pending
     |
     | (internet gelince SyncEngine aktif)
     v
[SyncEngine]
  1. Pending satislari siraya al (FIFO — CreatedAt'e gore)
  2. Her satis icin sunucu API'yi cagir
  3. Basariliysa → Status: Synced, SyncedAt = now
  4. Basarisizsa → Status: Failed, RetryCount++, hata logu
  5. Conflict varsa → Status: Conflict, cozum icin isaretle
     |
     v
[Sunucu API]
  POST /api/sync/sales  (batch)
  Response: { syncedIds: [...], conflicts: [...] }
```

### 3.3 Conflict Resolution Stratejisi

**Ana Senaryo:** Ayni urun, farkli yerlerde satildi ve toplam stok yetersiz.

```
Ornek:
  Sunucu stok: Urun X = 5 adet
  Magaza A (offline): 3 adet satti
  Magaza B (offline): 4 adet satti
  Toplam talep: 7 > 5 = CONFLICT
```

**Cozum: "First-Sync-Wins" + Negatif Stok Uyarisi**

1. **First-Sync-Wins:** Ilk senkronize olan satis kabul edilir, stok duser.
2. **Negatif stok uyarisi:** Ikinci senkronizasyonda stok yetersizse:
   - Satis yine kaydedilir (urun zaten fiziksel olarak satilmis, geri alinamaz)
   - `ForceDecreaseStockAsync` kullanilir (mevcut metod — negatife izin verir)
   - Admin'e bildirim gonderilir: "Stok tutarsizligi — Urun X, Sube B"
   - Stok sayimi/duzeltme workflow'u tetiklenir

**Neden bu strateji?**
- Fiziksel satisi geri almak mumkun degil — musteri urtinu aldi
- Negatif stok, envanter sayimi ile duzeltilir
- Mevcut `ForceDecreaseStockAsync` metodu tam olarak bu senaryo icin yazilmis

### 3.4 Veri Indirme (Sunucu → Local)

```
[SyncEngine — Periyodik]
  1. GET /api/sync/catalog?since={lastSyncTimestamp}
     → Degisen urunler + variantlar (delta sync)
  2. GET /api/sync/stock/{branchOfficeId}
     → Guncel stok durumlari
  3. GET /api/sync/customers?since={lastSyncTimestamp}
     → Degisen musteriler
  4. GET /api/sync/vouchers/active
     → Aktif indirim kuponlari
```

**Delta Sync:** Yalnizca `UpdatedAt > lastSyncTimestamp` olan kayitlar cekilir. Ilk senkronizasyonda tam veri indirilir.

### 3.5 Offline Stok Yonetimi

```
[Offline Satis]
  1. Local SQLite'dan stok kontrol et
  2. Yeterliyse: stok dus, satisi kaydet
  3. Yetersizse: kullaniciya uyari goster
     - "Stok yetersiz (local veri). Yine de satmak istiyor musunuz?"
     - Evet → satis yapilir, senkronizasyonda cozulur
     - Hayir → iptal

[Online Donusu]
  SyncEngine sunucudan guncel stok ceker
  Local stok guncellenir
```

---

## 4. Karsilastirma Tablosu

| Kriter | PWA (WASM) | MAUI Hybrid | Agent Genisletme | Electron/Tauri |
|--------|-----------|-------------|-----------------|----------------|
| **Gelistirme maliyeti** | Yuksek (6-8 hf) | Orta-Yuksek (4-6 hf) | Dusuk-Orta (6-8 hf, fazli) | Yuksek (8-10 hf) |
| **Mevcut kod yeniden kullanimi** | Dusuk (%20-30) | Orta-Yuksek (%60-80) | Orta (%40-50 logic) | Dusuk (%20-30) |
| **Kullanici deneyimi** | Iyi (tarayici) | Cok iyi (native) | Yeterli-Iyi | Iyi |
| **Offline guvenilirlik** | Orta (tarayici limitleri) | Yuksek | Yuksek | Orta-Yuksek |
| **Linux destegi** | Evet | Hayir (resmi degil) | Evet | Evet |
| **Windows destegi** | Evet | Evet | Evet | Evet |
| **Kurulum gereksinimi** | Hayir | Evet | Zaten kurulu | Evet |
| **Yazici entegrasyonu** | Sorunlu (CORS) | Native | Hazir | Sorunlu |
| **Bakim maliyeti** | Yuksek | Orta-Yuksek | Dusuk | Yuksek |
| **Risk seviyesi** | Yuksek | Orta | Dusuk | Yuksek |
| **Incremental delivery** | Zor | Orta | Kolay (faz faz) | Zor |
| **Mevcut sisteme etki** | Yuksek (API layer) | Orta | Dusuk | Yuksek |
| **Multi-tenant uyumu** | Orta | Orta | Iyi (tenant config) | Orta |

---

## 5. Oneri

### Birincil Oneri: Agent'i Genisletme (Yaklasim 2.3)

**Gerekce:**

1. **En dusuk risk:** Mevcut Blazor Server sistemi hic degismez. Agent bagimsiz bir uygulama olarak genisler.

2. **Altyapi hazir:** Agent zaten masaustunde calisIyor, .NET 8, Kestrel, yazici erisimi, API key guvenliği — tumu mevcut.

3. **Ayni teknoloji:** Ekip .NET 8 biliyor. SQLite + EF Core, PostgreSQL + EF Core ile neredeyse ayni. Business logic (KDV hesaplama, stok dusme mantigi) kopyalanabilir.

4. **Fazli teslimat:** Her faz bagimsiz calisir:
   - Faz 1 bitince: Agent veritabani hazir, veri indirilebilir
   - Faz 2 bitince: Offline satis yapilabilir (UI olmadan, baska bir client ile)
   - Faz 3 bitince: Otomatik senkronizasyon
   - Faz 4 bitince: Tam kullanici deneyimi

5. **Fiziksel magaza gercekligi:** Satis noktasinda bir bilgisayar zaten var (agent oraya kurulu). Bu bilgisayara ek bir uygulama kurmak yerine, mevcut agent'i genisletmek en dogal yol.

6. **ForceDecreaseStockAsync zaten var:** Conflict resolution icin gerekli olan "negatif stoka izin ver" metodu mevcut code base'de implement edilmis.

### Uygulama Yol Haritasi

```
Faz 1 (Hafta 1-2): Temel Altyapi
  ├─ Agent'a EF Core SQLite ekleme
  ├─ Local entity'ler ve migration'lar
  ├─ Sunucu tarafinda sync API endpoint'leri
  ├─ Veri indirme mekanizmasi (catalog + stock)
  └─ Unit testler

Faz 2 (Hafta 3-4): Offline Satis API
  ├─ POST /api/sales endpoint'i
  ├─ GET /api/products/search endpoint'i (barkod + isim)
  ├─ Local stok yonetimi
  ├─ Fis yazdirma entegrasyonu
  └─ Unit + Integration testler

Faz 3 (Hafta 5-6): Senkronizasyon
  ├─ SyncEngine background service
  ├─ Outbox pattern ile satis senkronizasyonu
  ├─ Delta sync ile veri guncelleme
  ├─ Conflict resolution + admin bildirimi
  └─ Integration testler

Faz 4 (Hafta 7-8): Kullanici Arayuzu
  ├─ Satis ekrani (barkod okuma, sepet, odeme)
  ├─ Stok goruntuleme
  ├─ Sync durumu gostergesi
  ├─ Offline/online mod gostergesi
  └─ E2E testler
```

### Onerilen UI Yaklasimi (Faz 4)

Agent icin **Razor Pages (server-rendered)** onerilir:
- Agent zaten `Microsoft.NET.Sdk.Web` — Razor Pages hazir
- MudBlazor yerine basit Bootstrap veya Tailwind CSS (Agent hafif kalmali)
- Satis ekrani tek sayfa: barkod input + sepet listesi + toplam + odeme butonu
- Karmasik UI gerekmez — POS ekrani sade olmali

Ilerleyen donemde kullanici deneyimi yetersiz bulunursa, Agent icine Blazor WASM embed edilebilir (Secenek 3). Bu, Faz 4'un scope'unu genisletir ama Faz 1-3'u etkilemez.

---

## 6. Sunucu Tarafi Gereksinimler

Agent'in senkronizasyonu icin Blazor Server uygulamasina eklenmesi gereken API endpoint'leri:

```
GET  /api/sync/catalog?since={timestamp}&branchOfficeId={id}
     → Urun + variant + fiyat + barkod (delta)

GET  /api/sync/stock/{branchOfficeId}
     → Guncel stok durumlari

GET  /api/sync/customers?since={timestamp}
     → Musteri listesi (delta)

GET  /api/sync/vouchers/active
     → Aktif indirim kuponlari

POST /api/sync/sales
     → Offline satislari toplu gonderme
     Request:  { sales: [MakeSaleDto[]] }
     Response: { synced: [...], conflicts: [...] }

GET  /api/sync/status
     → Son sync zamani, bekleyen islem sayisi
```

Bu endpoint'ler Blazor Server projesine `Endpoints/SyncEndpoints.cs` olarak minimal API seklinde eklenebilir (mevcut `Entegrasyon.Blazor/Endpoints/` klasoru zaten var).

---

## 7. Guvenlik Hususlari

1. **Agent-Sunucu iletisimi:** Mevcut API key mekanizmasi + HTTPS
2. **Local SQLite sifreleme:** `Microsoft.Data.Sqlite` ile `Password` connection string parametresi (SQLCipher)
3. **Offline satis yetkilendirme:** Agent config'inde `AllowedSalePersonIds` — yalnizca yetkili kullanicilar offline satis yapabilir
4. **Sync token:** JWT veya API key ile sunucu kimlik dogrulama
5. **Veri butunlugu:** Her offline satis icin UUID + timestamp + checksum

---

## 8. Izleme ve Gozlemlenebilirlik

1. Agent loglarI: Serilog ile dosyaya + opsiyonel olarak sunucuya (online oldugunda)
2. Sync metrikleri: Bekleyen satis sayisi, son sync zamani, basarisiz sync sayisi
3. Admin panelinde goruntuleme: "Offline Satislar" sayfasi — sync durumu, conflict'ler
4. Bildirim: Conflict veya sync hatasi oldugunda admin'e SignalR + email bildirimi

---

## 9. Sonuc

**Agent'i genisletme yaklasimi**, mevcut altyapinin uzerine en dusuk riskle ve en hizli sekilde offline satis yeteneği kazandirmanin yoludur. Mevcut Blazor Server sistemi degismez, Agent bagimsiz olarak evrilir. Sync stratejisi olarak queue-based outbox pattern ve "first-sync-wins + force decrease" conflict resolution, fiziksel perakende gercekligine en uygun yaklasimdir.
