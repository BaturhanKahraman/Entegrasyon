# Surat Kargo API - Temel Bilgi

**Son Guncelleme:** 2026-03-23

## Genel Bakis

Surat Kargo, Turkiye'nin buyuk kargo Şirketlerinden biridir. Entegrasyon icin SOAP tabanli bir web servisi sunmaktadir.

| Bilgi | Deger |
|-------|-------|
| WSDL URL | `http://webservices.suratkargo.com.tr/services.asmx?wsdl` |
| Service URL | `http://webservices.suratkargo.com.tr/services.asmx` |
| Protokol | SOAP 1.1 / SOAP 1.2 |
| Format | XML (SOAP Envelope) |
| Target Namespace | `http://tempuri.org/` |

## Kimlik Dogrulama (Authentication)

Her SOAP isteginde asagidaki bilgiler parametre olarak gonderilir:

| Parametre | Aciklama |
|-----------|----------|
| `userName` | Surat Kargo musteri numarasi veya API kullanici adi |
| `password` | API Şifresi |
| `customerCode` | Musteri kodu (bazi operasyonlarda ek olarak gerekir) |

**NOT:** OAuth veya token tabanli bir auth yoktur. Her istekte credential'lar SOAP body icinde gonderilir.

## Calisma Modeli

- Surat Kargo API'si SOAP 1.1 ve SOAP 1.2 destekler
- Tum istekler HTTP POST ile yapilir
- Content-Type: `text/xml; charset=utf-8` (SOAP 1.1) veya `application/soap+xml; charset=utf-8` (SOAP 1.2)
- SOAPAction header'i gereklidir (SOAP 1.1)
- Yanit XML formatinda doner

## Temel SOAP Envelope Yapisi

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:tem="http://tempuri.org/">
  <soap:Header/>
  <soap:Body>
    <tem:OperasyonAdi>
      <tem:userName>KULLANICI_ADI</tem:userName>
      <tem:password>Şifre</tem:password>
      <!-- Diger parametreler -->
    </tem:OperasyonAdi>
  </soap:Body>
</soap:Envelope>
```

## Hata Yonetimi

SOAP hatalari `soap:Fault` elementi icinde doner:

```xml
<soap:Fault>
  <faultcode>soap:Server</faultcode>
  <faultstring>Hata aciklamasi</faultstring>
  <detail>Detayli hata bilgisi</detail>
</soap:Fault>
```

Ayrica operasyonlarin kendi response'lari icinde de `resultCode` ve `resultMessage` alanlari bulunabilir.

## Ortam Bilgileri

| Ortam | URL |
|-------|-----|
| Production | `http://webservices.suratkargo.com.tr/services.asmx` |

**NOT:** Bilinen ayri bir sandbox/test ortami yoktur. Test islemleri genellikle canli ortamda, test musteri kodlari ile yapilir.

## Entegrasyondaki Yerimiz

Bu kargo entegrasyonu marketplace'lerden bagimsizdir. Tum marketplace Siparişleri (Trendyol, Hepsiburada, N11, Pazarama, PttAVM, Ciceksepeti vb.) icin ortak kullanilabilir. `CargoCompany` entity'si uzerinden baglanti yapilir.
