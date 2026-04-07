# Aras Kargo Entegrasyon Tasarim Dokumani

**Tarih:** 2026-03-23
**Kapsam:** Genel kargo entegrasyonu - tum marketplace Siparişleri icin Aras Kargo SOAP API entegrasyonu

---

## 1. Genel Bakis

Aras Kargo SOAP API uzerinden kargo gonderme, takip, iptal ve sorgulama islemlerini gerceklestirecek servis katmani.

**Protokol:** SOAP 1.1 (XML over HTTP POST)
**Base URL (Test):** `https://customerservicestest.araskargo.com.tr/arascargoservice/arascargoservice.asmx`
**Base URL (Prod):** `https://customerws.araskargo.com.tr/arascargoservice.asmx`

## 2. Mimari

```
IArasKargoService (Abstract)
    |
    +-- ArasKargoService (Concrete - production)
    |       |
    |       +-- ArasKargoClient (SOAP HTTP client)
    |
    +-- MockArasKargoService (Concrete - mock/test)
```

### Mevcut Altyapi ile Entegrasyon

- `CargoCompany` entity'si uzerinden kargo firmasi secimi yapilir
- `ICargoCompaniesManager` ile kargo firmalari yonetilir
- Marketplace Siparişleri (Trendyol, Hepsiburada, N11 vb.) icin kargo gonderim islemleri bu servis uzerinden yapilir

## 3. Dosya Yapisi

```
Business/Abstract/
  IArasKargoService.cs          -- Servis interface'i

Business/Concrete/Kargo/
  ArasKargoClient.cs            -- SOAP HTTP client (low-level)
  ArasKargoService.cs           -- Business service (high-level)
  MockArasKargoService.cs       -- Mock implementasyon
  ArasKargoModels.cs            -- Request/Response modelleri

Test/Entegrasyon.Test/Kargo/
  ArasKargoServiceTests.cs      -- Unit testler
```

## 4. Interface Tasarimi

```csharp
public interface IArasKargoService
{
    /// Yeni kargo gonderisi olusturur.
    Task<IDataResult<ArasKargoOrderResult>> CreateShipmentAsync(
        ArasKargoShipmentRequest request,
        CancellationToken ct = default);

    /// Gonderiyi iptal eder.
    Task<IResult> CancelShipmentAsync(
        string integrationCode,
        CancellationToken ct = default);

    /// Entegrasyon kodu ile gonderi durumunu sorgular.
    Task<IDataResult<ArasKargoTrackingResult>> TrackShipmentAsync(
        string integrationCode,
        CancellationToken ct = default);

    /// Kargo hareket gecmisini sorgular.
    Task<IDataResult<List<ArasKargoMovement>>> GetShipmentMovementsAsync(
        string integrationCode,
        CancellationToken ct = default);

    /// Tarih araligindaki kargolari sorgular.
    Task<IDataResult<List<ArasKargoShipmentSummary>>> GetShipmentsByDateRangeAsync(
        DateTime startDate, DateTime endDate,
        CancellationToken ct = default);

    /// Teslim edilmemis kargolari listeler.
    Task<IDataResult<List<ArasKargoShipmentSummary>>> GetUndeliveredShipmentsAsync(
        CancellationToken ct = default);
}
```

## 5. Model Tasarimi

### ArasKargoConfig
```csharp
public sealed record ArasKargoConfig(
    string UserName,
    string Password,
    string CustomerCode,
    string BaseUrl,
    bool IsTestEnvironment);
```

### ArasKargoShipmentRequest
```csharp
public sealed record ArasKargoShipmentRequest(
    string IntegrationCode,       // Benzersiz gonderi referansi
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverCityName,
    string ReceiverTownName,
    string ReceiverAddress,
    int PieceCount,
    string? Description = null,
    bool IsCod = false,
    decimal CodAmount = 0,
    string? InvoiceNumber = null,
    List<ArasKargoPieceDetail>? PieceDetails = null);
```

### ArasKargoPieceDetail
```csharp
public sealed record ArasKargoPieceDetail(
    string? BarcodeNumber = null,
    decimal? Weight = null,
    decimal? VolumetricWeight = null,
    string? Description = null);
```

### ArasKargoOrderResult
```csharp
public sealed record ArasKargoOrderResult(
    string ResultCode,
    string ResultMessage,
    string? BarcodeNumber);
```

### ArasKargoTrackingResult
```csharp
public sealed record ArasKargoTrackingResult(
    string IntegrationCode,
    string Status,
    string? ReceiverName,
    string? DeliveryDate,
    string? BarcodeNumber);
```

### ArasKargoMovement
```csharp
public sealed record ArasKargoMovement(
    string Date,
    string Status,
    string Location,
    string? Description);
```

### ArasKargoShipmentSummary
```csharp
public sealed record ArasKargoShipmentSummary(
    string IntegrationCode,
    string Status,
    string ReceiverName,
    string? Date);
```

## 6. ArasKargoClient - SOAP Client

`ArasKargoClient` low-level SOAP mesajlarini olusturur ve gonderir.

```csharp
public sealed class ArasKargoClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ArasKargoClient> logger)
```

**Sorumluluklar:**
- SOAP XML envelope olusturma
- HTTP POST ile SOAP request gonderme
- XML response parse etme
- SOAPAction header'i yonetme

**Metodlar:**
- `SetOrderAsync(...)` - SOAP SetOrder cagirisi
- `CancelDispatchAsync(...)` - SOAP CancelDispatch cagirisi
- `GetQueryJsonAsync(...)` - SOAP GetQueryJSON cagirisi

## 7. ArasKargoService

```csharp
public sealed class ArasKargoService(
    ArasKargoClient client,
    IApplicationLogManager applicationLogManager,
    ILogger<ArasKargoService> logger) : IArasKargoService
```

**Sorumluluklar:**
- Input validasyonu
- ArasKargoClient cagirisi
- Application loglama (Turkce, admin icin)
- Hata yonetimi ve Result donusumu
- ILogger ile teknik loglama

## 8. MockArasKargoService

Test ve development ortami icin mock implementasyon.

```csharp
public sealed class MockArasKargoService(
    ILogger<MockArasKargoService> logger) : IArasKargoService
```

Tum metodlar basarili sonuc doner, gercek API cagirisi yapmaz.

## 9. DI Kayit

`ApplicationDependencyExtension.cs` icinde:

```csharp
// Aras Kargo servisleri
var useArasKargoMock = configuration.GetValue<bool>("ArasKargo:UseMock", true);
if (useArasKargoMock)
{
    services.AddScoped<IArasKargoService, MockArasKargoService>();
}
else
{
    services.AddScoped<ArasKargoClient>();
    services.AddScoped<IArasKargoService, ArasKargoService>();
}
```

## 10. Konfigürasyon (appsettings.json)

```json
{
  "ArasKargo": {
    "UseMock": true,
    "UserName": "",
    "Password": "",
    "CustomerCode": "",
    "BaseUrl": "https://customerservicestest.araskargo.com.tr/arascargoservice/arascargoservice.asmx",
    "IsTestEnvironment": true
  }
}
```

> **Multi-tenant not:** Production'da bu degerler DB'den okunmali, appsettings'e hardcode edilmemeli.

## 11. Multi-Tenant Uyum

- `ArasKargoClient` scoped olarak kayitli, singleton state yok
- Konfigürasyon DB'den okunabilir hale getirilecek
- Tenant bazli credential yonetimi ileride eklenebilir

## 12. Test Plani

### Unit Testler (ArasKargoServiceTests.cs)

1. `CreateShipmentAsync` - basarili gonderi olusturma
2. `CreateShipmentAsync` - bos IntegrationCode ile hata
3. `CreateShipmentAsync` - bos ReceiverName ile hata
4. `CreateShipmentAsync` - API hatasi durumunda error donmesi
5. `CancelShipmentAsync` - basarili iptal
6. `CancelShipmentAsync` - bos integrationCode ile hata
7. `CancelShipmentAsync` - API hatasi durumunda error
8. `TrackShipmentAsync` - basarili takip
9. `TrackShipmentAsync` - bos integrationCode ile hata
10. `GetShipmentMovementsAsync` - basarili hareket listesi
11. `GetShipmentsByDateRangeAsync` - basarili tarih araliginda sorgulama
12. `GetUndeliveredShipmentsAsync` - basarili teslim edilmemis liste
13. `MockArasKargoService` - tum metodlar basarili doner

## 13. Gelecek Fazlar

- Kargo etiketi (label) PDF olusturma
- Toplu gonderi olusturma (SetDispatchXML)
- Fatura sorgulama
- Kampanya kodu sorgulama
- Blazor UI: kargo takip sayfasi
- Webhook/polling ile otomatik durum guncelleme
