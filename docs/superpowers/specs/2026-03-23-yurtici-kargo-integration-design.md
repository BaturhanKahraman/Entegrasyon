# Yurtici Kargo Entegrasyonu — Design Spec

**Tarih:** 2026-03-23
**Yaklasim:** Genel kargo entegrasyonu (tum marketplace siparisleri icin)

---

## 1. Genel Bakis

Entegrasyon projesine Yurtici Kargo entegrasyonu ekleniyor. SOAP web servisleri uzerinden kargo olusturma, takip, iptal islemleri yapilir. Bu, marketplace-spesifik bir entegrasyon degil; tum marketplace siparislerinin kargo sureclerini yonetecek genel bir kargo servisidir.

**WSDL Endpoint'leri:**
- Production: `http://webservices.yurticikargo.com:8080/KOPSWebServices/ShippingOrderDispatcherServices?wsdl`
- Test: `https://testapi.yurticikargo.com:9090/KOPSWebServices/ShippingOrderDispatchService?wsdl`

**Kapsam:** Kargo olusturma, kargo durumu sorgulama, kargo iptali.

---

## 2. Mimari & Dosya Yapisi

### Business Layer

```
Business/Abstract/
  IYurticiKargoService.cs          # Ana servis interface'i

Business/Concrete/Kargo/
  YurticiKargoClient.cs            # SOAP client wrapper (HttpClient + XML)
  YurticiKargoService.cs           # Is mantigi servisi
  MockYurticiKargoService.cs       # Dev/test mock
  YurticiKargoModels.cs            # Request/response DTO'lari
```

### Test

```
Test/Entegrasyon.Test/Kargo/
  YurticiKargoServiceTests.cs      # Unit testler
```

---

## 3. YurticiKargoClient

**Sablon:** `PttavmShippingService.cs` + HttpClient pattern

SOAP isteklerini HttpClient ile gonderir (WCF/System.ServiceModel kullanilmaz — .NET 8'de lightweight kalsinlar diye raw XML + HttpClient tercih edilir).

```csharp
public interface IYurticiKargoClient
{
    Task<HttpResponseMessage> SendSoapRequestAsync(
        string soapAction,
        string soapBody,
        CancellationToken ct = default);
}
```

**Multi-Tenant:** Credential'lar DB'den okunur (`ApplicationSetting` tablosu uzerinden). Client, tenant-spesifik config alir.

**Config Alanlari (DB):**
- `YurticiKargo:WsUserName`
- `YurticiKargo:WsPassword`
- `YurticiKargo:UserLanguage` (default: "TR")
- `YurticiKargo:BaseUrl` (default: production WSDL)
- `YurticiKargo:UseMock` (default: true)

---

## 4. IYurticiKargoService Interface

```csharp
public interface IYurticiKargoService
{
    /// Yeni kargo olusturur
    Task<IDataResult<YurticiCreateShipmentResponse>> CreateShipmentAsync(
        YurticiCreateShipmentRequest request,
        CancellationToken ct = default);

    /// Kargo durumunu sorgular
    Task<IDataResult<List<YurticiShipmentInfo>>> QueryShipmentAsync(
        YurticiQueryShipmentRequest request,
        CancellationToken ct = default);

    /// Kargoyu iptal eder
    Task<IResult> CancelShipmentAsync(
        string cargoKey,
        CancellationToken ct = default);
}
```

---

## 5. Model Tanimlari (YurticiKargoModels.cs)

### Request Modelleri

```csharp
public sealed record YurticiCreateShipmentRequest(
    string CargoKey,
    string InvoiceKey,
    string ReceiverCustName,
    string ReceiverAddress,
    string CityName,
    string TownName,
    string ReceiverPhone1,
    int CargoCount = 1,
    string? ReceiverPhone2 = null,
    string? ReceiverPhone3 = null,
    string? EmailAddress = null,
    decimal? Desi = null,
    decimal? Kg = null,
    string? Description = null,
    decimal? TtInvoiceAmount = null,
    int? TtCollectionType = null,
    string? TtDocumentId = null,
    int? TtDocumentSaveType = null,
    int? DcSelectedCredit = null,
    int? DcCreditRule = null);

public sealed record YurticiQueryShipmentRequest(
    string[] Keys,
    int KeyType = 0,          // 0=CargoKey, 1=InvoiceKey
    bool AddHistoricalData = false,
    bool OnlyTracking = false);
```

### Response Modelleri

```csharp
public sealed record YurticiCreateShipmentResponse(
    string OutFlag,            // "0" = fail, "1" = success
    string OutResult,
    string? JobId);

public sealed record YurticiShipmentInfo(
    string CargoKey,
    string? InvoiceKey,
    int OperationCode,
    string? OperationMessage,
    DateTime? DeliveryDate,
    string? DeliveredTo,
    int? UnitCount);

public sealed record YurticiCancelShipmentResponse(
    string OutFlag,
    string OutResult,
    string? CargoKey);
```

---

## 6. YurticiKargoService

**Sablon:** `PttavmShippingService.cs`

Primary constructor DI:

```csharp
public sealed class YurticiKargoService(
    IYurticiKargoClient client,
    IApplicationLogManager applicationLogManager,
    ILogger<YurticiKargoService> logger) : IYurticiKargoService
```

Her metod:
1. Parametre validasyonu (null/empty check)
2. SOAP XML body olusturma
3. Client ile istek gonderme
4. XML response parse etme
5. Hata/basari loglama
6. IResult/IDataResult dondurme

---

## 7. MockYurticiKargoService

Test ve development icin. Gercek API cagrisi yapmaz, sabit basarili sonuclar doner.

```csharp
public sealed class MockYurticiKargoService : IYurticiKargoService
```

---

## 8. CargoCompany Entegrasyonu

Mevcut `CargoCompany` entity'si ile iliskili:
- CargoCompany tablosunda "Yurtici Kargo" kaydi bulunur
- Siparis kargo atamasinda CargoCompanyId uzerinden Yurtici Kargo secilir
- `IYurticiKargoService` dogrudan inject edilir (CargoCompany ile loose coupling)

---

## 9. DI Kayit

`ApplicationDependencyExtension.cs` icinde:

```csharp
// Yurtici Kargo servisleri
var useYurticiMock = configuration.GetValue<bool>("YurticiKargo:UseMock", true);
if (useYurticiMock)
{
    services.AddScoped<IYurticiKargoClient, MockYurticiKargoClient>();
    services.AddScoped<IYurticiKargoService, MockYurticiKargoService>();
}
else
{
    services.AddScoped<IYurticiKargoClient, YurticiKargoClient>();
    services.AddScoped<IYurticiKargoService, YurticiKargoService>();
}
```

---

## 10. Test Plani

- `YurticiKargoServiceTests.cs`:
  - CreateShipmentAsync: basarili, bos request, API hata
  - QueryShipmentAsync: basarili, bos keys, API hata
  - CancelShipmentAsync: basarili, bos cargoKey, API hata
  - MockYurticiKargoService: tum metodlar basarili doner
