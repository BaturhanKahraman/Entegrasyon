# Aras Kargo API - Gonderi Islemleri (Shipment API)

## 1. SetOrder - Kargo Gonderi Olusturma

Yeni bir kargo gonderisi olusturur.

### SOAPAction
```
http://ArasCargo.com/SetOrder
```

### SOAP Request

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:aras="http://ArasCargo.com/">
  <soap:Body>
    <aras:SetOrder>
      <aras:orderInfo>
        <aras:Order>
          <aras:UserName>string</aras:UserName>
          <aras:Password>string</aras:Password>
          <aras:TradingWaybillNumber>string</aras:TradingWaybillNumber>
          <aras:InvoiceNumber>string</aras:InvoiceNumber>
          <aras:IntegrationCode>string</aras:IntegrationCode>
          <aras:ReceiverName>string</aras:ReceiverName>
          <aras:ReceiverPhone>string</aras:ReceiverPhone>
          <aras:ReceiverCityName>string</aras:ReceiverCityName>
          <aras:ReceiverTownName>string</aras:ReceiverTownName>
          <aras:ReceiverAddress>string</aras:ReceiverAddress>
          <aras:SenderName>string</aras:SenderName>
          <aras:SenderPhone>string</aras:SenderPhone>
          <aras:SenderCityName>string</aras:SenderCityName>
          <aras:SenderTownName>string</aras:SenderTownName>
          <aras:SenderAddress>string</aras:SenderAddress>
          <aras:PieceCount>int</aras:PieceCount>
          <aras:IsWorldWide>int</aras:IsWorldWide>
          <aras:IsCOD>int</aras:IsCOD>
          <aras:CodAmount>decimal</aras:CodAmount>
          <aras:CodCollectionType>int</aras:CodCollectionType>
          <aras:CodBillingType>int</aras:CodBillingType>
          <aras:PayorTypeCode>int</aras:PayorTypeCode>
          <aras:Description>string</aras:Description>
          <aras:SpecialField1>string</aras:SpecialField1>
          <aras:SpecialField2>string</aras:SpecialField2>
          <aras:SpecialField3>string</aras:SpecialField3>
          <aras:PieceDetails>
            <aras:PieceDetail>
              <aras:BarcodeNumber>string</aras:BarcodeNumber>
              <aras:ProductNumber>int</aras:ProductNumber>
              <aras:VolumetricWeight>decimal</aras:VolumetricWeight>
              <aras:Weight>decimal</aras:Weight>
              <aras:Description>string</aras:Description>
            </aras:PieceDetail>
          </aras:PieceDetails>
        </aras:Order>
      </aras:orderInfo>
    </aras:SetOrder>
  </soap:Body>
</soap:Envelope>
```

### Alan Aciklamalari

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| `UserName` | string | Evet | XML servis kullanici adi |
| `Password` | string | Evet | XML servis Şifresi |
| `TradingWaybillNumber` | string | Hayir | Ticari irsaliye numarasi |
| `InvoiceNumber` | string | Hayir | Fatura numarasi |
| `IntegrationCode` | string | Evet | Entegrasyon kodu (benzersiz gonderi referansi) |
| `ReceiverName` | string | Evet | Alici adi soyadi |
| `ReceiverPhone` | string | Evet | Alici telefon numarasi |
| `ReceiverCityName` | string | Evet | Alici sehir adi |
| `ReceiverTownName` | string | Evet | Alici ilce adi |
| `ReceiverAddress` | string | Evet | Alici adresi |
| `SenderName` | string | Hayir | Gonderen adi (bos ise musteri bilgisi kullanilir) |
| `SenderPhone` | string | Hayir | Gonderen telefon |
| `SenderCityName` | string | Hayir | Gonderen sehir |
| `SenderTownName` | string | Hayir | Gonderen ilce |
| `SenderAddress` | string | Hayir | Gonderen adresi |
| `PieceCount` | int | Evet | Parca Sayısı |
| `IsWorldWide` | int | Hayir | Yurtdisi mi? (0=Hayir, 1=Evet) |
| `IsCOD` | int | Hayir | Kapida odeme? (0=Hayir, 1=Evet) |
| `CodAmount` | decimal | Hayir | Kapida odeme tutari |
| `CodCollectionType` | int | Hayir | Tahsilat tipi (0=Nakit, 1=Kredi Karti) |
| `CodBillingType` | int | Hayir | Fatura tipi |
| `PayorTypeCode` | int | Hayir | Odemeyi kim yapar (1=Gonderen, 2=Alici) |
| `Description` | string | Hayir | Gonderi aciklamasi |
| `SpecialField1-3` | string | Hayir | Ozel alanlar |

### PieceDetail Alanlari

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| `BarcodeNumber` | string | Hayir | Parca barkod numarasi |
| `ProductNumber` | int | Hayir | Urun numarasi |
| `VolumetricWeight` | decimal | Hayir | Desi agirligi |
| `Weight` | decimal | Hayir | Agirlik (kg) |
| `Description` | string | Hayir | Parca aciklamasi |

### SOAP Response

```xml
<SetOrderResponse>
  <SetOrderResult>
    <OrderResultInfo>
      <ResultCode>string</ResultCode>
      <ResultMessage>string</ResultMessage>
      <BarcodeNumber>string</BarcodeNumber>
    </OrderResultInfo>
  </SetOrderResult>
</SetOrderResponse>
```

### Ornek Hata Mesajlari
- `"Sevk adresi bulunamadı"` - Gecersiz sehir/ilce kombinasyonu
- `"Entegrasyon kodu daha once kullanilmis"` - Tekrarlanan IntegrationCode

---

## 2. CancelDispatch - Gonderi Iptal

Olusturulmus bir gonderiyi iptal eder.

### SOAPAction
```
http://ArasCargo.com/CancelDispatch
```

### SOAP Request

```xml
<soap:Body>
  <aras:CancelDispatch>
    <aras:userName>string</aras:userName>
    <aras:password>string</aras:password>
    <aras:integrationCode>string</aras:integrationCode>
  </aras:CancelDispatch>
</soap:Body>
```

### Alan Aciklamalari

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| `userName` | string | Evet | XML servis kullanici adi |
| `password` | string | Evet | XML servis Şifresi |
| `integrationCode` | string | Evet | Iptal edilecek gonderinin entegrasyon kodu |

---

## 3. GetOrderWithIntegrationCode - Gonderi Sorgulama

Entegrasyon kodu ile gonderi bilgilerini sorgular.

### SOAPAction
```
http://ArasCargo.com/GetOrderWithIntegrationCode
```

### SOAP Request

```xml
<soap:Body>
  <aras:GetOrderWithIntegrationCode>
    <aras:userName>string</aras:userName>
    <aras:password>string</aras:password>
    <aras:integrationCode>string</aras:integrationCode>
  </aras:GetOrderWithIntegrationCode>
</soap:Body>
```

---

## 4. GetQueryJSON - Genel Sorgulama

JSON formatinda kargo sorgulama. Farkli `QueryType` degerleri ile cesitli sorgulamalar yapilabilir.

### SOAPAction
```
http://ArasCargo.com/GetQueryJSON
```

### SOAP Request

```xml
<soap:Body>
  <aras:GetQueryJSON>
    <aras:loginInfo>
      <aras:UserName>string</aras:UserName>
      <aras:Password>string</aras:Password>
      <aras:CustomerCode>string</aras:CustomerCode>
    </aras:loginInfo>
    <aras:queryInfo>
      <aras:QueryType>int</aras:QueryType>
      <aras:IntegrationCode>string</aras:IntegrationCode>
      <aras:Date1>string</aras:Date1>
      <aras:Date2>string</aras:Date2>
    </aras:queryInfo>
  </aras:GetQueryJSON>
</soap:Body>
```

### QueryType Degerleri

| QueryType | Aciklama | Gerekli Parametreler |
|-----------|----------|---------------------|
| 1 | Kargo bilgi sorgulama | IntegrationCode |
| 2 | Tarihte gonderilen kargolar | Date1 |
| 3 | Tarihte teslim edilen kargolar | Date1 |
| 4 | Teslim edilmemis kargolar | - |
| 5 | Yonlendirilen kargolar | Date1 |
| 6 | Iade edilen kargolar | Date1 |
| 7 | Kargo hareket bilgisi | IntegrationCode |
| 8 | Tum subeler | - |
| 9 | Tarih araliginda kargolar | Date1, Date2 |
| 10 | Kapsamli kargo bilgisi | IntegrationCode |
| 11 | Fatura sorgulama | IntegrationCode |
| 12 | Bugunku teslim Sayısı | - |
| 13 | Kampanya kodu | IntegrationCode |

### Response
JSON string XML-wrapped olarak doner. Icerik QueryType'a gore degisir.

---

## 5. SetDispatchXML - Toplu Gonderi

Birden fazla gonderiyi tek seferde olusturur.

### SOAPAction
```
http://ArasCargo.com/SetDispatchXML
```

Birden fazla `<Order>` elemani icerir.

---

## Durum Kodlari

| Kod | Aciklama |
|-----|----------|
| 0 | Basarili |
| 1 | Hata |
| 2 | Kismi basarili |

## Kargo Durum Tipleri

| Durum | Aciklama |
|-------|----------|
| Kabul | Kargo kabul edildi |
| Aktarma | Transfer merkezinde |
| Dagitimda | Dagitima cikti |
| Teslim Edildi | Aliciya teslim edildi |
| Iade | Gonderene iade |

## Entegrasyon Adimlari

1. Aras Kargo ile sozlesme yap
2. `esasweb.araskargo.com.tr` uzerinden XML servis credential'larini al
3. Test ortaminda `SetOrder` ile gonderi olustur
4. `GetQueryJSON` ile kargo durumlarini sorgula
5. Production URL'ye gecis yap
