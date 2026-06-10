# Surat Kargo API - Gonderi Islemleri

**Son Guncelleme:** 2026-03-23

## Operasyonlar Ozeti

| # | Operasyon | Aciklama |
|---|-----------|----------|
| 1 | `CreateShipment` | Yeni gonderi olusturma |
| 2 | `QueryShipmentInfo` | Gonderi durumu sorgulama (tracking) |
| 3 | `CancelShipment` | Gonderi iptal etme |
| 4 | `CreateShipmentWithBarcode` | Barkodlu gonderi olusturma |
| 5 | `GetBarcodeByReferenceNo` | Referans numarasi ile barkod alma |
| 6 | `GetShipmentLabel` | Gonderi etiketi alma (ZPL/PDF) |

---

## 1. CreateShipment — Gonderi Olusturma

Yeni bir kargo gonderisi olusturur ve takip numarasi doner.

**SOAPAction:** `http://tempuri.org/CreateShipment`

### Request Parametreleri

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| `userName` | string | Evet | API kullanici adi |
| `password` | string | Evet | API Şifresi |
| `customerCode` | string | Evet | Musteri kodu |
| `invoiceKey` | string | Hayir | Fatura anahtari |
| `receiverName` | string | Evet | Alici adi |
| `receiverAddress` | string | Evet | Alici adresi |
| `receiverPhone1` | string | Evet | Alici telefon (birincil) |
| `receiverPhone2` | string | Hayir | Alici telefon (ikincil) |
| `receiverPhone3` | string | Hayir | Alici telefon (ucuncu) |
| `receiverCityName` | string | Evet | Alici il adi |
| `receiverTownName` | string | Evet | Alici ilce adi |
| `referenceNo` | string | Hayir | Gondericinin referans numarasi (Sipariş no) |
| `pieceCount` | int | Evet | Parca Sayısı |
| `weight` | decimal | Hayir | Agirlik (kg) |
| `volume` | decimal | Hayir | Hacim (desi) |
| `ttDocumentId` | string | Hayir | TT belge ID |
| `dcSelectedCredit` | int | Hayir | Odeme tipi (0=Gonderici, 1=Alici) |
| `dcCreditRule` | int | Hayir | Kredi kurali |
| `description` | string | Hayir | Aciklama |
| `isCOD` | bool | Hayir | Kapida odeme mi |
| `codAmount` | decimal | Hayir | Kapida odeme tutari |
| `ttDocumentSaveType` | int | Hayir | Belge kayit tipi |
| `ttInvoiceAmount` | decimal | Hayir | Fatura tutari |
| `ttCollectionType` | int | Hayir | Tahsilat tipi |

### Response

| Alan | Tip | Aciklama |
|------|-----|----------|
| `resultCode` | int | Sonuc kodu (0=Basarili) |
| `resultMessage` | string | Sonuc mesaji |
| `shippingOrderNo` | string | Kargo Sipariş numarasi (tracking number) |
| `barcodeNo` | string | Barkod numarasi |

### Ornek Request

```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:tem="http://tempuri.org/">
  <soap:Body>
    <tem:CreateShipment>
      <tem:userName>API_USER</tem:userName>
      <tem:password>API_PASS</tem:password>
      <tem:customerCode>CUST_CODE</tem:customerCode>
      <tem:receiverName>Ali Yilmaz</tem:receiverName>
      <tem:receiverAddress>Ataturk Cad. No:123</tem:receiverAddress>
      <tem:receiverPhone1>05551234567</tem:receiverPhone1>
      <tem:receiverCityName>Istanbul</tem:receiverCityName>
      <tem:receiverTownName>Kadikoy</tem:receiverTownName>
      <tem:referenceNo>ORD-2026-001</tem:referenceNo>
      <tem:pieceCount>1</tem:pieceCount>
      <tem:weight>1.5</tem:weight>
    </tem:CreateShipment>
  </soap:Body>
</soap:Envelope>
```

---

## 2. QueryShipmentInfo — Gonderi Sorgulama

Tracking number veya referans numarasi ile gonderi durumunu sorgular.

**SOAPAction:** `http://tempuri.org/QueryShipmentInfo`

### Request Parametreleri

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| `userName` | string | Evet | API kullanici adi |
| `password` | string | Evet | API Şifresi |
| `shippingOrderNo` | string | Kosullu | Kargo Sipariş no (tracking) |
| `referenceNo` | string | Kosullu | Referans numarasi |

En az biri (`shippingOrderNo` veya `referenceNo`) verilmelidir.

### Response

| Alan | Tip | Aciklama |
|------|-----|----------|
| `resultCode` | int | Sonuc kodu |
| `resultMessage` | string | Sonuc mesaji |
| `shipmentStatus` | string | Gonderi durumu |
| `deliveryDate` | datetime? | Teslim tarihi (teslim edildiyse) |
| `movements` | array | Hareket gecmisi |

**Durum Degerleri:**

| Durum | Aciklama |
|-------|----------|
| `Kargoya Verildi` | Gonderi sisteme girildi |
| `Transfer Merkezinde` | Dagitim merkezinde |
| `Dagitimda` | Kurye dagitima cikti |
| `Teslim Edildi` | Aliciya teslim edildi |
| `Iade` | Gonderi iade edildi |
| `Iptal` | Gonderi iptal edildi |

---

## 3. CancelShipment — Gonderi Iptal

Henuz kargoya verilmemis bir gonderiyi iptal eder.

**SOAPAction:** `http://tempuri.org/CancelShipment`

### Request Parametreleri

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| `userName` | string | Evet | API kullanici adi |
| `password` | string | Evet | API Şifresi |
| `shippingOrderNo` | string | Evet | Kargo Sipariş numarasi |

### Response

| Alan | Tip | Aciklama |
|------|-----|----------|
| `resultCode` | int | Sonuc kodu (0=Basarili) |
| `resultMessage` | string | Sonuc mesaji |

---

## 4. CreateShipmentWithBarcode — Barkodlu Gonderi

Gonderi olusturur ve aninda barkod bilgisi doner. CreateShipment ile benzerdir ancak response'ta barkod detaylari yer alir.

**SOAPAction:** `http://tempuri.org/CreateShipmentWithBarcode`

Parametreler `CreateShipment` ile aynidir. Response'ta ek olarak:

| Alan | Tip | Aciklama |
|------|-----|----------|
| `barcodeBase64` | string | Base64 encoded barkod goruntüsü |
| `barcodeUrl` | string | Barkod indirme URL'i |

---

## 5. GetBarcodeByReferenceNo — Referans ile Barkod

Referans numarasi uzerinden barkod bilgisi doner.

**SOAPAction:** `http://tempuri.org/GetBarcodeByReferenceNo`

### Request Parametreleri

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| `userName` | string | Evet | API kullanici adi |
| `password` | string | Evet | API Şifresi |
| `referenceNo` | string | Evet | Referans numarasi |

### Response

| Alan | Tip | Aciklama |
|------|-----|----------|
| `resultCode` | int | Sonuc kodu |
| `barcodeNo` | string | Barkod numarasi |
| `barcodeBase64` | string | Base64 encoded barkod |

---

## 6. GetShipmentLabel — Gonderi Etiketi

Gonderi etiketi alir (yazici icin formatlari destekler).

**SOAPAction:** `http://tempuri.org/GetShipmentLabel`

### Request Parametreleri

| Parametre | Tip | Zorunlu | Aciklama |
|-----------|-----|---------|----------|
| `userName` | string | Evet | API kullanici adi |
| `password` | string | Evet | API Şifresi |
| `shippingOrderNo` | string | Evet | Kargo Sipariş numarasi |
| `labelFormat` | string | Hayir | Etiket formati (ZPL, PDF, vb.) |

### Response

| Alan | Tip | Aciklama |
|------|-----|----------|
| `resultCode` | int | Sonuc kodu |
| `resultMessage` | string | Sonuc mesaji |
| `labelData` | string | Base64 encoded etiket verisi |
| `labelFormat` | string | Etiket formati |
