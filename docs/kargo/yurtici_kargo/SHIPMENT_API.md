# Yurtici Kargo API - Kargo Islemleri

## 1. createShipment - Kargo Olusturma

Musteri, gondermek istedigi kargo bilgilerini bu metod ile Yurtici Kargo sistemine aktarir. Sistem, tasima belgesi ve barkod etiketi uretir.

### Request: ShippingOrderVO

| Alan | Tip | Zorunlu | Aciklama |
|---|---|---|---|
| `cargoKey` | string | Evet | Kargo anahtari (barkod uzerinde gorunur, benzersiz olmali) |
| `invoiceKey` | string | Evet | Fatura anahtari (gonderi basina benzersiz) |
| `receiverCustName` | string | Evet | Alici adi soyadi (min 5 karakter, min 4 harf) |
| `receiverAddress` | string | Evet | Teslimat adresi (5-200 karakter) |
| `cityName` | string | Evet | Sehir adi |
| `townName` | string | Evet | Ilce adi |
| `receiverPhone1` | string | Evet | Alici telefon (10 hane, alan kodu dahil) |
| `receiverPhone2` | string | Hayir | Alternatif telefon |
| `receiverPhone3` | string | Hayir | Alternatif telefon |
| `emailAddress` | string | Hayir | Alici e-posta |
| `cargoCount` | int | Hayir | Koli sayisi (varsayilan: 1) |
| `desi` | decimal | Hayir | Hacimsel agirlik (desi) |
| `kg` | decimal | Hayir | Gercek agirlik (kg) |
| `description` | string | Hayir | Gonderi icerigi aciklamasi |
| `waybillNo` | string | Hayir | Ticari gonderi irsaliye no |
| `custProdId` | string | Hayir | Musteri urun kodu |
| `specialField1` | string | Hayir | Ozel alan 1 |
| `specialField2` | string | Hayir | Ozel alan 2 |
| `specialField3` | string | Hayir | Ozel alan 3 |
| `taxOfficeId` | string | Hayir | Vergi dairesi kodu |
| `taxNumber` | string | Hayir | Vergi numarasi |
| `taxOfficeName` | string | Hayir | Vergi dairesi adi |
| `orgGeoCode` | string | Hayir | Organizasyon geo kodu |
| `privilegeOrder` | string | Hayir | Oncelikli gonderi |
| `orgReceiverCustId` | string | Hayir | Organizasyon alici musteri ID |

### Kapida Odeme (COD) Alanlari

| Alan | Tip | Aciklama |
|---|---|---|
| `ttDocumentId` | string | Tahsilat fatura numarasi |
| `ttInvoiceAmount` | decimal | Tahsilat tutari |
| `ttCollectionType` | int | 0 = Nakit, 1 = Kredi karti |
| `ttDocumentSaveType` | int | 0 = Ayni fatura, 1 = Ayri fatura |
| `dcSelectedCredit` | int | Taksit sayisi |
| `dcCreditRule` | int | 0 = Musteri sectigi, 1 = Tek cekim |

### Response

| Alan | Tip | Aciklama |
|---|---|---|
| `outFlag` | string | `"0"` = basarisiz, `"1"` = basarili |
| `outResult` | string | Sonuc mesaji |
| `jobId` | string | Is kimlik numarasi |
| `shippingOrderDetailVO` | object | Olusturulan gonderinin detay bilgisi |

---

## 2. queryShipment - Kargo Sorgulama

Musteriler bu metod ile kargo teslimat bilgilerini sorgular.

### Request

| Alan | Tip | Zorunlu | Aciklama |
|---|---|---|---|
| `keys` | string[] | Evet | Kargo anahtarlari veya fatura anahtarlari dizisi |
| `keyType` | int | Evet | `0` = Kargo anahtari, `1` = Fatura anahtari |
| `addHistoricalData` | bool | Hayir | Tasima hareket bilgilerini ekle (varsayilan: false) |
| `onlyTracking` | bool | Hayir | Sadece takip linki don (varsayilan: false) |

### Response: ShippingDeliveryDetailVO

| Alan | Tip | Aciklama |
|---|---|---|
| `operationCode` | int | Islem kodu |
| `operationMessage` | string | Islem mesaji |
| `invKeys` | string | Fatura anahtari |
| `cargoKey` | string | Kargo anahtari |
| `deliveryDate` | datetime | Teslim tarihi |
| `deliveredTo` | string | Teslim edilen kisi |
| `unitCount` | int | Parca sayisi |

### Kargo Durum Kodlari

| Kod | Aciklama |
|---|---|
| 1 | Kargo bilgisi alindi |
| 2 | Kargo kabul edildi (subede) |
| 3 | Dagitimda |
| 4 | Teslim edildi |
| 5 | Iade |
| 6 | Devir |

---

## 3. cancelShipment - Kargo Iptali

Kargo, Yurtici Kargo subesine fiziksel teslimden once veya teslimden sonra fatura kesilmeden once iptal edilebilir.

### Request

| Alan | Tip | Zorunlu | Aciklama |
|---|---|---|---|
| `cargoKeys` | string | Evet | Iptal edilecek kargo anahtari (virgul ile ayrilmis birden fazla olabilir) |

### Response: ShippingCancelDetailVO

| Alan | Tip | Aciklama |
|---|---|---|
| `outFlag` | string | `"0"` = basarisiz, `"1"` = basarili |
| `outResult` | string | Sonuc mesaji |
| `cargoKey` | string | Iptal edilen kargo anahtari |

---

## 4. queryShipmentDetail - Detayli Kargo Sorgulama (Opsiyonel)

`queryShipment` ile ayni parametreleri alir, ek olarak:

| Alan | Tip | Aciklama |
|---|---|---|
| `jsonData` | bool | `true` ise JSON formatinda detayli hareket bilgisi doner |

Response'ta tum kargo hareketi asamalari (milestone) yer alir.

---

## Ornek SOAP Request (createShipment)

```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/"
                  xmlns:ship="http://kargo.yurtici.com/">
  <soapenv:Body>
    <ship:createShipment>
      <wsUserName>USERNAME</wsUserName>
      <wsPassword>PASSWORD</wsPassword>
      <userLanguage>TR</userLanguage>
      <ShippingOrderVO>
        <cargoKey>ORDER-12345</cargoKey>
        <invoiceKey>INV-12345</invoiceKey>
        <receiverCustName>Ahmet Yilmaz</receiverCustName>
        <receiverAddress>Ataturk Cad. No:1 Kadikoy</receiverAddress>
        <cityName>Istanbul</cityName>
        <townName>Kadikoy</townName>
        <receiverPhone1>5321234567</receiverPhone1>
        <cargoCount>1</cargoCount>
      </ShippingOrderVO>
    </ship:createShipment>
  </soapenv:Body>
</soapenv:Envelope>
```

## Onemli Notlar

- `cargoKey` her gonderi icin benzersiz olmali
- Telefon numarasi 10 haneli olmali (alan kodu dahil, 0 haric)
- `receiverCustName` minimum 5 karakter, en az 4'u harf olmali
- `receiverAddress` 5-200 karakter arasinda olmali
- Kapida odeme (COD) alanlari yalnizca ilgili kargo turlerinde kullanilir
- Production ve test ortami icin farkli credentials kullanilir
