# Surat Kargo Entegrasyon Tasarim Dokumani

**Tarih:** 2026-03-23
**Durum:** Onaylandi

## Ozet

Surat Kargo, marketplace'den bagimsiz bir kargo entegrasyonudur. SOAP tabanli web servisini kullanarak gonderi olusturma, kargo takip, gonderi iptal ve etiket yazdirma islemlerini destekler. Tum marketplace siparisleri (Trendyol, Hepsiburada, N11, Pazarama, PttAVM, Ciceksepeti) icin ortak kullanilabilir.

## Kararlar

| Karar | Secim | Gerekce |
|-------|-------|---------|
| API Protokolu | SOAP 1.1 (XML) | Surat Kargo'nun sunduğu web servisi SOAP tabanli |
| Client yapisi | SuratKargoClient (SOAP) | N11SoapClient pattern'inden esinlendi, ancak genel amacli |
| Mock toggle | `SuratKargo:UseMock` | Mevcut pattern ile tutarli |
| DI lifetime | Scoped | Diger kargo servisleriyle tutarli, DbContext uyumlu |
| Multi-tenant | Credential cache ConcurrentDictionary | Token refresh yok, DB'ye her istekte gitmemek icin |
| CargoCompany iliskisi | Mevcut CargoCompany entity'si kullanilir | Yeni entity gereksiz |

## API Genel Bakis

| Operasyon | SOAP Action | Aciklama |
|-----------|-------------|----------|
| CreateShipment | `http://tempuri.org/CreateShipment` | Gonderi olusturma |
| QueryShipmentInfo | `http://tempuri.org/QueryShipmentInfo` | Gonderi durumu sorgulama |
| CancelShipment | `http://tempuri.org/CancelShipment` | Gonderi iptal |
| GetBarcodeByReferenceNo | `http://tempuri.org/GetBarcodeByReferenceNo` | Barkod alma |
| GetShipmentLabel | `http://tempuri.org/GetShipmentLabel` | Etiket yazdirma |

Detayli API dokumantasyonu: `docs/kargo/surat_kargo/` (2 dosya)

## Mimari

```
ISuratKargoService (interface)
  |
  +-- SuratKargoService (canli implementation)
  |     |
  |     +-- SuratKargoClient (SOAP isteklerini gonderer)
  |
  +-- MockSuratKargoService (mock implementation)
```

## Dosya Yapisi

```
Business/Abstract/
  ISuratKargoService.cs

Business/Concrete/Kargo/
  SuratKargoClient.cs         -- SOAP HTTP client
  SuratKargoService.cs        -- Is mantigi (ISuratKargoService impl)
  MockSuratKargoService.cs    -- Mock impl (development/test)
  SuratKargoModels.cs         -- Request/Response DTO'lari

Test/Entegrasyon.Test/Kargo/
  SuratKargoServiceTests.cs   -- Unit testler
```

## ISuratKargoService Interface

```csharp
public interface ISuratKargoService
{
    Task<IDataResult<SuratKargoShipmentResult>> CreateShipmentAsync(
        SuratKargoShipmentRequest request, CancellationToken ct = default);

    Task<IDataResult<SuratKargoTrackingResult>> QueryShipmentAsync(
        string trackingNumber, CancellationToken ct = default);

    Task<IResult> CancelShipmentAsync(
        string trackingNumber, CancellationToken ct = default);

    Task<IDataResult<string>> GetBarcodeAsync(
        string referenceNo, CancellationToken ct = default);

    Task<IDataResult<string>> GetShipmentLabelAsync(
        string trackingNumber, string? labelFormat = null, CancellationToken ct = default);
}
```

## SuratKargoClient

SOAP XML isteklerini HTTP POST ile gonderir. N11SoapClient'tan farkli olarak:
- SOAP namespace: `http://tempuri.org/`
- Auth bilgileri SOAP Body icinde parametre olarak gonderilir (Header'da degil)
- DB'den credential'lar alinir (CargoCompany tablosu + ApplicationSetting)

**Multi-tenant tasarim:**
- Credential cache: `ConcurrentDictionary<int, SuratKargoCredentials>` (TTL: 5dk)
- Token yenileme yok (her istekte username/password)

```csharp
public interface ISuratKargoClient
{
    Task<XElement> SendAsync(string soapAction, string operationName,
        Dictionary<string, string> parameters, CancellationToken ct = default);
}
```

## Model Tanimlari

### SuratKargoShipmentRequest

```csharp
public sealed record SuratKargoShipmentRequest(
    string ReceiverName,
    string ReceiverAddress,
    string ReceiverPhone,
    string ReceiverCityName,
    string ReceiverTownName,
    int PieceCount,
    string? ReferenceNo = null,
    decimal? Weight = null,
    string? ReceiverPhone2 = null,
    string? Description = null,
    bool IsCOD = false,
    decimal? CodAmount = null);
```

### SuratKargoShipmentResult

```csharp
public sealed record SuratKargoShipmentResult(
    string TrackingNumber,
    string? BarcodeNo,
    int ResultCode,
    string? ResultMessage);
```

### SuratKargoTrackingResult

```csharp
public sealed record SuratKargoTrackingResult(
    string TrackingNumber,
    string Status,
    DateTime? DeliveryDate,
    List<SuratKargoMovement> Movements);

public sealed record SuratKargoMovement(
    DateTime Date,
    string Location,
    string Description);
```

## DI Kayit

`ApplicationDependencyExtension.cs` icinde:

```csharp
// Surat Kargo
var useSuratKargoMock = configuration.GetValue<bool>("SuratKargo:UseMock", true);
if (useSuratKargoMock)
{
    services.AddScoped<ISuratKargoService, MockSuratKargoService>();
}
else
{
    services.AddScoped<ISuratKargoClient, SuratKargoClient>();
    services.AddScoped<ISuratKargoService, SuratKargoService>();
}
```

## Configuration (appsettings.json)

```json
{
  "SuratKargo": {
    "UseMock": true,
    "BaseUrl": "http://webservices.suratkargo.com.tr/services.asmx",
    "UserName": "",
    "Password": "",
    "CustomerCode": ""
  }
}
```

## Test Plani

| Test | Aciklama |
|------|----------|
| CreateShipmentAsync_ValidRequest_ReturnsTrackingNumber | Basarili gonderi olusturma |
| CreateShipmentAsync_EmptyReceiverName_ReturnsError | Bos alici adi hatasi |
| QueryShipmentAsync_ValidTracking_ReturnsStatus | Basarili sorgulama |
| QueryShipmentAsync_EmptyTracking_ReturnsError | Bos tracking hatasi |
| CancelShipmentAsync_ValidTracking_ReturnsSuccess | Basarili iptal |
| CancelShipmentAsync_EmptyTracking_ReturnsError | Bos tracking hatasi |
| GetBarcodeAsync_ValidReference_ReturnsBarcodeData | Basarili barkod alma |
| GetBarcodeAsync_EmptyReference_ReturnsError | Bos referans hatasi |
| GetShipmentLabelAsync_ValidTracking_ReturnsLabelData | Basarili etiket alma |
| GetShipmentLabelAsync_EmptyTracking_ReturnsError | Bos tracking hatasi |
| MockSuratKargoService_CreateShipment_ReturnsFakeTracking | Mock servis gonderi test |
| MockSuratKargoService_QueryShipment_ReturnsFakeStatus | Mock servis sorgulama test |

## CargoCompany Entegrasyonu

Mevcut `CargoCompany` entity'si kullanilir. Surat Kargo icin DB'de bir kayit olusturulur:

```
CargoCompany { Name = "Sürat Kargo", Code = "SURAT" }
```

Configuration bilgileri (userName, password, customerCode) `ApplicationSetting` tablosundan cekilir veya `appsettings.json`'dan okunur. Multi-tenant geciste tenant-specific ayarlar DB'den gelecektir.
