# Aras Kargo API - Temel Bilgiler

## Genel Bakis

Aras Kargo, Turkiye'nin en buyuk kargo Şirketlerinden biridir. API entegrasyonu **SOAP (XML Web Service)** protokolu uzerinden calisir.

## Kimlik Dogrulama (Authentication)

Aras Kargo API'si her SOAP request'inde su credential'lari ister:

| Alan | Aciklama |
|------|----------|
| `UserName` | XML servis kullanici adi (Aras Kargo tarafindan verilir) |
| `Password` | XML servis Şifresi |
| `CustomerCode` | Musteri kodu (Aras Kargo sozlesmesindeki musteri numarasi) |

Credential'lar `https://esasweb.araskargo.com.tr/` uzerinden "Entegrasyon Uyelikleri" sayfasindan olusturulur. Kayit sirasinda servis metodu olarak `GetQueryJSON` secilmelidir.

## Base URL'ler

| Ortam | URL |
|-------|-----|
| **Test** | `https://customerservicestest.araskargo.com.tr/arascargoservice/arascargoservice.asmx` |
| **Production** | `https://customerws.araskargo.com.tr/arascargoservice.asmx` |
| **WSDL (Test)** | `https://customerservicestest.araskargo.com.tr/arascargoservice/arascargoservice.asmx?wsdl` |

## Protokol

- **Protokol:** SOAP 1.1 / SOAP 1.2
- **Content-Type:** `text/xml; charset=utf-8` (SOAP 1.1) veya `application/soap+xml; charset=utf-8` (SOAP 1.2)
- **HTTP Method:** POST
- **SOAPAction Header:** Her metod icin farkli (ornegin `http://ArasCargo.com/SetOrder`)

## SOAP Envelope Yapisi

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:tem="http://tempuri.org/"
               xmlns:aras="http://ArasCargo.com/">
  <soap:Body>
    <aras:MethodName>
      <aras:userName>XML_USER</aras:userName>
      <aras:password>XML_PASS</aras:password>
      <!-- Method-specific parameters -->
    </aras:MethodName>
  </soap:Body>
</soap:Envelope>
```

## Mevcut SOAP Metodlari

| Metod | Aciklama | Kategori |
|-------|----------|----------|
| `SetOrder` | Kargo gonderi olusturma | Gonderi |
| `CancelDispatch` | Gonderi iptal etme | Gonderi |
| `GetOrderWithIntegrationCode` | Entegrasyon koduyla gonderi sorgulama | Sorgulama |
| `GetQueryJSON` | Genel JSON sorgulama (tarih, durum vb.) | Sorgulama |
| `SetDispatchXML` | Toplu gonderi XML gonderi olusturma | Gonderi |
| `GetCargoTransaction` | Kargo islem detaylari | Sorgulama |
| `GetCargoTransactionByWaybillId` | Irsaliye ID ile islem sorgulama | Sorgulama |

## Query Tipleri (GetQueryJSON)

PHP API kutuphanesinden cikarilan sorgulama metodlari:

| Metod | Aciklama | Parametre |
|-------|----------|-----------|
| `getCargoInformation` | Tek kargo durum sorgulama | Kargo numarasi |
| `getCargoMovementDate` | Belirli tarihte gonderilen kargolar | Tarih |
| `getCargoDevileryDate` | Teslim edilen kargolar (tarih bazli) | Tarih |
| `getCargoUnDevilered` | Teslim edilmemis bekleyen kargolar | - |
| `getCargoRedirectDate` | Yonlendirilen kargolar | Tarih |
| `getCargoSenderReturnDate` | Gonderene iade edilen kargolar | Tarih |
| `getCargoMovementInformation` | Kargo hareket gecmisi | Kargo numarasi |
| `getAllBranchs` | Tum sube bilgileri | - |
| `getCargoWaybillBetweenDate` | Tarih araliginda kargolar | Başlangıç/Bitiş tarihi |
| `getCargoRealInformation` | Kapsamli kargo durum bilgisi | Kargo numarasi |
| `getCargoInvoice` | Fatura bilgisi (fatura/e-fatura) | Fatura no, tip |
| `getCargoCountToday` | Bugunku teslim Sayısı | - |
| `getCampaignCode` | Kampanya kodu bilgisi | Kampanya kodu |

## Tarih Formati

- API tarihleri `dd-MM-yyyy` formatinda kabul eder
- Alternatif olarak Unix timestamp da kullanilabilir

## Response Formati

SOAP response'lari XML icerisinde doner. `GetQueryJSON` metodu JSON string olarak sonuc dondurur (XML-wrapped JSON).

## Onemli Notlar

1. API credential'lari Aras Kargo ile sozlesme yapildiktan sonra verilir
2. Test ortaminda calismak icin test credential'lari gereklidir
3. Her musteriye ozel `CustomerCode` atanir
4. SetOrder ile gonderi olustururken sehir/ilce kodlari Aras Kargo'nun kendi kodlamasina uygun olmalidir
5. Production gecisi icin Aras Kargo'dan onay alinmasi gerekebilir

## Referanslar

- [GitHub - ismail0234/aras-kargo-php-api](https://github.com/ismail0234/aras-kargo-php-api) - PHP API wrapper
- [GitHub - tulparstudyo/kargo-net](https://github.com/tulparstudyo/kargo-net) - .NET kargo kutuphanesi
- [GitHub - ercancavusoglu/aras-cargo-entegrasyon](https://github.com/ercancavusoglu/aras-cargo-entegrasyon) - Laravel entegrasyon
