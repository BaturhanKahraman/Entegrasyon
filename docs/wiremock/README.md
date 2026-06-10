# WireMock — Lokal Dev Offline Mode

Bu klasor WireMock standalone container icin mapping ve response body dosyalarini
barindirir. Amac: gelistirici lokal ortamda internetsiz calisabilsin ve marketplace
HTTP cagrilari gercek API yerine lokal WireMock'a dusurulsun.

## Hizli Başlangıç

```bash
# Docker compose'u dev override ile baslat
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# WireMock admin UI kontrol
curl http://localhost:8080/__admin/mappings

# MVC uygulamasi artik marketplace HTTP cagrilarini WireMock'a yapacak
# Browser: http://localhost:5100 → /marketplaces → Trendyol sync buton
# Gelen istekler WireMock admin UI'da gorulur:
curl http://localhost:8080/__admin/requests | jq
```

## Klasor Yapisi

```
docs/wiremock/
├── README.md                    # Bu dosya
├── mappings/                    # Request-response eslestirme kurallari
│   ├── trendyol/*.json
│   ├── hepsiburada/*.json
│   └── ...
└── __files/                     # Response body dosyalari
    ├── trendyol/orders-page1.json
    ├── hepsiburada/listing.json
    └── ...
```

## Stub Dosya Formati

WireMock mapping dosyalari standart WireMock JSON formatinda. Ornek
`mappings/trendyol/get-orders.json`:

```json
{
  "request": {
    "method": "GET",
    "urlPathPattern": "/suppliers/[0-9]+/orders"
  },
  "response": {
    "status": 200,
    "headers": { "Content-Type": "application/json" },
    "bodyFileName": "trendyol/orders-page1.json"
  }
}
```

Response body `__files/trendyol/orders-page1.json`:
```json
{
  "page": 0,
  "size": 20,
  "content": [
    {
      "orderNumber": "TY-001",
      "status": "Created",
      "grossAmount": 299.90
    }
  ]
}
```

## Gercek API Toggle

Bir marketplace icin gercek sandbox API'ye gitmek istiyorsan
`appsettings.Development.json`'da:

```json
"DevMode": {
  "WireMockUrl": "http://wiremock:8080",
  "RealApiMarketplaces": ["Trendyol"]
}
```

DevWireMockSeeder bu listeyi okur ve listelenen marketplace'in BaseUrl'ini
WireMock'a cevirmez. Admin Panel'den MarketPlace.BaseUrl alanini manuel olarak
gercek sandbox URL'ine yaz, seeder dokunmaz.

## Fixture Guncelleme (Recording)

Gercek sandbox API'nin response'larini fixture olarak kaydetmek icin `record.sh`
scripti WireMock'u proxy mode'una cevirir, gercek API'ye yonlendirir, gelen
response'lari otomatik JSON'a kaydeder.

### On kosullar

- WireMock container ayakta (`docker-compose.dev.yml` ile)
- `jq` kurulu (`sudo dnf install jq`)
- Gercek sandbox API credential'lari MarketPlace tablosunda mevcut
- `RealApiMarketplaces` listesine ilgili marketplace eklenmis (seeder dokunmasin)

### Kullanim

```bash
# Trendyol sandbox'a kaydet
./docs/wiremock/record.sh trendyol https://stageapigw.trendyol.com

# Script asamalari:
# 1. Proxy mapping kaydeder (* → target-url)
# 2. "Enter'a bas" mesaji — MVC'den akislari tetikleme zamani
# 3. Snapshot alir: recorded-YYYYMMDD-HHMMSS.json
# 4. sanitize.sh otomatik calisir (credential temizligi)
```

### Sanitize — Credential Temizligi

`sanitize.sh` mapping dosyasindan hassas bilgileri temizler:

**Request headers:** `Authorization`, `x-api-key`, `X-API-Key`, `Cookie`,
`x-amz-access-token` → `REDACTED`

**Response headers:** `Set-Cookie` → `REDACTED`

**Response body:** `"access_token"`, `"refresh_token"`, `"refreshToken"`,
`"apiSecret"`, `"password"` field'lari → `REDACTED`

**ONEMLI:** Regex-based sanitize mukemmel degil. Commit etmeden once manuel
goz gezdir:
```bash
jq '.mappings[] | .response.body' docs/wiremock/mappings/trendyol/recorded-*.json | head -50
```

### Recorded Fixture'u Kullanma

Recording cikti dosyasi tek bir mega-mapping JSON'i icerir. Bunu integration
test stub'inda kullanmak icin iki secenek:

**Opsiyon A — Tek dosya import:** WireMock standalone container otomatik olarak
`mappings/` altindaki tum JSON dosyalarini yukler. Recorded dosyayi olduu gibi
`docs/wiremock/mappings/<marketplace>/` altina biraktir.

**Opsiyon B — Parcalara bol:** Ozellikle integration test'lerde her stub'i
ayri JSON olarak tutmak istersen, `jq` ile mapping'leri endpoint bazli bol:
```bash
jq '.mappings[] | select(.request.url // .request.urlPattern // .request.urlPathPattern | test("orders"))' \
    recorded-20260411.json > mappings/trendyol/get-orders.json
```

Sonra `TrendyolStubs.cs` iskelet metodlarindaki TODO'lari doldur ve
`WithBodyFromFile("Fixtures/Trendyol/orders-page1.json")` ile bodyleri
integration test fixture klasorunde referans al.

### Fixture Guncellik

Marketplace API'leri yilda 2-3 kez degisir. Bir endpoint'in stub'i eskirse:

1. Ilgili marketplace icin `record.sh` calistir
2. Yeni recorded dosyayi mevcut stub ile diff'le (`diff -u old.json new.json`)
3. Breaking change varsa integration test beklentilerini guncelle
4. Eski recorded dosyasini sil, yenisini commit et

CI'da otomatik bir "fixture drift detection" job'u opsiyonel — gelecekteki bir
is olarak ele alinabilir.

## Stub'lari Gelistirici Tarafindan Doldurma

Faz 3'te her marketplace icin iskelet `XxxStubs.cs` sinifi olusturuldu
(`Test/Entegrasyon.IntegrationTest/Fixtures/WireMockStubs/`). Integration test
yazilirken bu iskeletler doldurulacak ve karsilik gelen JSON fixture dosyalari
bu klasore eklenecek.

Mevcut iskelet siniflari:
- TrendyolStubs.cs
- HepsiburadaStubs.cs
- N11Stubs.cs
- PazaramaStubs.cs
- AmazonStubs.cs
- PttavmStubs.cs
- CiceksepetiStubs.cs
- TemuStubs.cs

## Sorun Giderme

**MVC uygulamasi marketplace cagrilarinda 404 aliyor:** Stub yok. WireMock admin
UI'dan (`http://localhost:8080/__admin/mappings`) eslestirilemeyen request'i gor,
mapping ekle.

**WireMock container baslamiyor:** Port 8080 cakismasi olabilir.
`docker-compose.dev.yml`'deki port mapping'i degistir.

**Gelistirici gercek API'ye gitmek istiyor:** `RealApiMarketplaces` listesine
marketplace adini ekle, MVC'yi restart et.
