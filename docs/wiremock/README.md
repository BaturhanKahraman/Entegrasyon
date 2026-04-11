# WireMock — Lokal Dev Offline Mode

Bu klasor WireMock standalone container icin mapping ve response body dosyalarini
barindirir. Amac: gelistirici lokal ortamda internetsiz calisabilsin ve marketplace
HTTP cagrilari gercek API yerine lokal WireMock'a dusurulsun.

## Hizli Baslangic

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

Gercek sandbox API'nin response'larini fixture olarak kaydetmek icin Faz 5'te
`docs/wiremock/record.sh` scripti yazilacak. Bu script WireMock'u proxy mode'una
cevirir, gercek API'ye yonlendirir, gelen response'lari otomatik JSON'a kaydeder.

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
