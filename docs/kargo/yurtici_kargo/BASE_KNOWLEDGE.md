# Yurtici Kargo API - Temel Bilgiler

## Genel Bakis

Yurtici Kargo, Turkiye'nin en buyuk kargo firmalarindan biridir. Entegrasyon SOAP web servisleri uzerinden saglanir. `ShippingOrderDispatcherServices` WSDL uzerinden kargo olusturma, takip, iptal islemleri yapilir.

## WSDL Endpoint'leri

| Ortam | URL |
|---|---|
| Production | `http://webservices.yurticikargo.com:8080/KOPSWebServices/ShippingOrderDispatcherServices?wsdl` |
| Test | `https://testapi.yurticikargo.com:9090/KOPSWebServices/ShippingOrderDispatchService?wsdl` |

> **Not:** Production port 8080, Test port 9090 uzerinden calisir. Sunucu tarafinda bu portlarin acik olmasi gerekir.

## Kimlik Dogrulama (Authentication)

Her SOAP istegi icinde asagidaki alanlar gonderilir (header degil, request body icinde):

| Alan | Tip | Aciklama |
|---|---|---|
| `wsUserName` | string | Web servis kullanici adi (Yurtici Kargo tarafindan verilir) |
| `wsPassword` | string | Web servis Şifresi |
| `userLanguage` | string | Dil kodu, genellikle `"TR"` |

Yurtici Kargo entegrasyon basvurusu yapilarak bu bilgiler elde edilir. Her musteri (tenant) icin ayri credential verilir.

## Protokol & Teknik Detaylar

- **Protokol:** SOAP 1.1 / 1.2
- **Veri Formati:** XML
- **Encoding:** UTF-8
- **Transport:** HTTP (port 8080 prod, port 9090 test)
- **Namespace:** `http://kargo.yurtici.com/`

## Servis Operasyonlari

| Operasyon | Aciklama |
|---|---|
| `createShipment` | Yeni kargo olusturma |
| `queryShipment` | Kargo durumu sorgulama |
| `cancelShipment` | Kargo iptali |

## Kargo Turleri

| Tip | Aciklama |
|---|---|
| STANDART | Gondericinin odemeli standart kargo |
| PAYMENT_DOOR | Kapida odeme (nakit) |
| PAYMENT_DOOR_CC | Kapida odeme (kredi karti) |

## Hata Yonetimi

API response'lari `outFlag` ve `outResult` alanlari icerir:
- `outFlag`: Islem sonucu (`0` = başarısız, `1` = basarili)
- `outResult`: Hata veya basari mesaji

## Entegrasyon Basvurusu

Yurtici Kargo web servis entegrasyonu icin:
1. Yurtici Kargo pazarlama departmani ile İletişime gecilinir
2. Entegrasyon basvurusu yapilir
3. Test ortami credentials'lari alinir
4. Test ortaminda gelistirme yapilir
5. Production credentials'lari alinir ve canli ortama gecirilir
