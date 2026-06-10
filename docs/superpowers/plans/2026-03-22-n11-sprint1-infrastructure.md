# N11 Sprint 1: Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** N11 pazaryeri entegrasyonunun altyapisini kurmak — SOAP client, sabitler, DB seed, mock servisler ve DI kaydi.

**Architecture:** N11 entegrasyonu Trendyol pattern'ini paralel olarak takip eder (IN11* interfaceleri, ayri concrete siniflar). N11 SOAP/XML kullanir, bu nedenle TrendyolApiClient'in REST/JSON yaklasiminin yerine raw HttpClient + XML envelope yaklasimi uygulanir. Auth her SOAP body'sinde `<auth><appKey/><appSecret/></auth>` olarak gider (HTTP header degil).

**Tech Stack:** .NET 8, HttpClient, System.Xml.Linq (XElement/XDocument), EF Core (PostgreSQL), xUnit + Moq + FluentAssertions

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Application/Entegrasyon.Business/Abstract/IN11SoapClient.cs` | N11 SOAP client interface |
| Create | `Application/Entegrasyon.Business/Concrete/N11/N11SoapClient.cs` | Credential-aware SOAP client implementation |
| Create | `Application/Entegrasyon.Business/Abstract/IN11ProductService.cs` | N11 product service interface (Sprint 1: stub) |
| Create | `Application/Entegrasyon.Business/Abstract/IN11StockPriceService.cs` | N11 stock/price service interface (Sprint 1: stub) |
| Create | `Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs` | Mock product service |
| Create | `Application/Entegrasyon.Business/Concrete/N11/MockN11StockPriceService.cs` | Mock stock/price service |
| Modify | `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs` | Add N11MarketPlaceId = 2 |
| Modify | `Application/Entegrasyon.Business/Utility/Constants/StringConstants.cs` | Add N11Api constant |
| Modify | `Application/Entegrasyon.Entity/Categories/ImportSource.cs` | Add N11 = 101 |
| Create | `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/<timestamp>_SeedN11MarketPlace.cs` | Seed migration |
| Modify | `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Register N11 services + HttpClient |
| Create | `Test/Entegrasyon.Test/N11/N11ConstantsTests.cs` | Unit tests for constants and enums |
| Create | `Test/Entegrasyon.Test/N11/N11SoapClientTests.cs` | Unit tests for SOAP client |
| Create | `Test/Entegrasyon.Test/N11/MockN11ProductServiceTests.cs` | Unit tests for mock product service |
| Create | `Test/Entegrasyon.Test/N11/MockN11StockPriceServiceTests.cs` | Unit tests for mock stock/price service |

---

## Task 1: MarketPlaceConstants ve StringConstants Guncelle

**Files:**
- Modify: `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs`
- Modify: `Application/Entegrasyon.Business/Utility/Constants/StringConstants.cs`
- Test: `Test/Entegrasyon.Test/N11/N11ConstantsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// Test/Entegrasyon.Test/N11/N11ConstantsTests.cs
using Entegrasyon.Business.Utility.Constants;
using FluentAssertions;

namespace Entegrasyon.Test.N11;

public class N11ConstantsTests
{
    [Fact]
    public void N11MarketPlaceId_Should_Be_2()
    {
        MarketPlaceConstants.N11MarketPlaceId.Should().Be(2);
    }

    [Fact]
    public void N11MarketPlaceId_Should_Differ_From_Trendyol()
    {
        MarketPlaceConstants.N11MarketPlaceId.Should()
            .NotBe(MarketPlaceConstants.TrendyolMarketPlaceId);
    }

    [Fact]
    public void N11Api_StringConstant_Should_Be_Defined()
    {
        StringConstants.N11Api.Should().Be("N11Api");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11ConstantsTests" -v m`
Expected: FAIL — `N11MarketPlaceId` and `N11Api` do not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
// MarketPlaceConstants.cs — add one line
public const int N11MarketPlaceId = 2;
```

```csharp
// StringConstants.cs — add one line
public const string N11Api = "N11Api";
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11ConstantsTests" -v m`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs \
      Application/Entegrasyon.Business/Utility/Constants/StringConstants.cs \
      Test/Entegrasyon.Test/N11/N11ConstantsTests.cs
git commit -m "feat(n11): add N11 marketplace constants (MarketPlaceId=2, N11Api)"
```

---

## Task 2: ImportSource Enum Guncelle

**Files:**
- Modify: `Application/Entegrasyon.Entity/Categories/ImportSource.cs`
- Test: `Test/Entegrasyon.Test/N11/N11ConstantsTests.cs` (extend)

- [ ] **Step 1: Write the failing test**

Add to `N11ConstantsTests.cs`:

```csharp
using Entegrasyon.Entity.Categories;

[Fact]
public void ImportSource_N11_Should_Be_101()
{
    ((int)ImportSource.N11).Should().Be(101);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ImportSource_N11" -v m`
Expected: FAIL — `ImportSource.N11` does not exist.

- [ ] **Step 3: Write minimal implementation**

Add to `ImportSource.cs` after `Trendyol = 100`:

```csharp
/// <summary>
/// N11 pazaryerinden import edilmis
/// </summary>
N11 = 101
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~ImportSource_N11" -v m`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Entity/Categories/ImportSource.cs \
      Test/Entegrasyon.Test/N11/N11ConstantsTests.cs
git commit -m "feat(n11): add N11 to ImportSource enum (101)"
```

---

## Task 3: IN11SoapClient Interface

> **Not:** Bu task interface-only — test'i Task 4'te N11SoapClient testleriyle birlikte gelir (TDD).

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IN11SoapClient.cs`

- [ ] **Step 1: Create the interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IN11SoapClient.cs
using System.Xml.Linq;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 SOAP API'ye credential-aware XML cagrilari yapan client.
/// Her istekte MarketPlace tablosundan (Id=2) appKey/appSecret ceker
/// ve SOAP envelope icindeki <auth> blogu olarak ekler.
/// </summary>
public interface IN11SoapClient
{
    /// <summary>
    /// Belirtilen WSDL endpoint'ine SOAP request gonderir.
    /// </summary>
    /// <param name="wsdlPath">Servis yolu, ornegin "CategoryService"</param>
    /// <param name="soapAction">SOAP action ismi (N11 icin bos string gonderilebilir)</param>
    /// <param name="bodyContent">SOAP Body icindeki XML elementi (auth otomatik eklenir)</param>
    /// <returns>Response SOAP body'sinin icerigini XElement olarak doner</returns>
    Task<XElement> SendAsync(string wsdlPath, string soapAction, XElement bodyContent);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IN11SoapClient.cs
git commit -m "feat(n11): add IN11SoapClient interface"
```

---

## Task 4: N11SoapClient Implementation

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/N11/N11SoapClient.cs`
- Create: `Test/Entegrasyon.Test/N11/N11SoapClientTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// Test/Entegrasyon.Test/N11/N11SoapClientTests.cs
using System.Net;
using System.Xml.Linq;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.N11;

public class N11SoapClientTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<N11SoapClient>> _loggerMock = new();

    private N11SoapClient CreateSut() => new(
        _contextFactoryMock.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    [Fact]
    public async Task SendAsync_Should_Include_Auth_In_Soap_Body()
    {
        // Arrange
        var marketplace = new MarketPlace
        {
            Id = N11MarketPlaceId,
            Name = "N11",
            ApiKey = "test-app-key",
            ApiSecret = "test-app-secret",
            BaseUrl = "https://api.n11.com/ws/"
        };

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new IntegrationDbContext(options);
        dbContext.MarketPlaces.Add(marketplace);
        await dbContext.SaveChangesAsync();

        _contextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbContext);

        // Capture the request sent to HttpClient
        string? capturedRequestBody = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedRequestBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
                        <env:Body>
                            <ns3:GetTopLevelCategoriesResponse xmlns:ns3="http://www.n11.com/ws/schemas">
                                <result><status>success</status></result>
                            </ns3:GetTopLevelCategoriesResponse>
                        </env:Body>
                    </env:Envelope>
                    """)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var sut = CreateSut();

        var bodyContent = new XElement(
            XName.Get("GetTopLevelCategoriesRequest", "http://www.n11.com/ws/schemas"));

        // Act
        var result = await sut.SendAsync("CategoryService", "", bodyContent);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain("<appKey>test-app-key</appKey>");
        capturedRequestBody.Should().Contain("<appSecret>test-app-secret</appSecret>");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_Should_Throw_When_MarketPlace_Not_Found()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new IntegrationDbContext(options);

        _contextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbContext);

        var sut = CreateSut();

        var bodyContent = new XElement("Dummy");

        // Act & Assert
        await sut.Invoking(s => s.SendAsync("CategoryService", "", bodyContent))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*N11*");
    }

    [Fact]
    public async Task SendAsync_Should_Return_Response_Body_Content()
    {
        // Arrange
        var marketplace = new MarketPlace
        {
            Id = N11MarketPlaceId,
            Name = "N11",
            ApiKey = "key",
            ApiSecret = "secret",
            BaseUrl = "https://api.n11.com/ws/"
        };

        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new IntegrationDbContext(options);
        dbContext.MarketPlaces.Add(marketplace);
        await dbContext.SaveChangesAsync();

        _contextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbContext);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
                        <env:Body>
                            <ns3:TestResponse xmlns:ns3="http://www.n11.com/ws/schemas">
                                <result><status>success</status></result>
                                <data>test-value</data>
                            </ns3:TestResponse>
                        </env:Body>
                    </env:Envelope>
                    """)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var sut = CreateSut();
        var bodyContent = new XElement(
            XName.Get("TestRequest", "http://www.n11.com/ws/schemas"));

        // Act
        var result = await sut.SendAsync("TestService", "", bodyContent);

        // Assert
        result.Should().NotBeNull();
        result.Name.LocalName.Should().Be("TestResponse");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11SoapClientTests" -v m`
Expected: FAIL — `N11SoapClient` class does not exist.

- [ ] **Step 3: Write the N11SoapClient implementation**

```csharp
// Application/Entegrasyon.Business/Concrete/N11/N11SoapClient.cs
using System.Text;
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// N11 SOAP API'ye credential-aware XML cagrilari yapan client.
/// Her istekte MarketPlace tablosundan (Id=2) appKey/appSecret ceker
/// ve SOAP envelope icindeki auth blogu olarak ekler.
///
/// N11 SOAP envelope formati:
/// <![CDATA[
/// <env:Envelope xmlns:env="http://schemas.xmlsoap.org/soap/envelope/">
///   <env:Header/>
///   <env:Body>
///     <sch:XxxRequest xmlns:sch="http://www.n11.com/ws/schemas">
///       <auth>
///         <appKey>***</appKey>
///         <appSecret>***</appSecret>
///       </auth>
///       ...request fields...
///     </sch:XxxRequest>
///   </env:Body>
/// </env:Envelope>
/// ]]>
/// </summary>
public sealed class N11SoapClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<N11SoapClient> logger) : IN11SoapClient
{
    private const string DefaultBaseUrl = "https://api.n11.com/ws/";

    private static readonly XNamespace SoapEnv =
        "http://schemas.xmlsoap.org/soap/envelope/";

    public async Task<XElement> SendAsync(string wsdlPath, string soapAction, XElement bodyContent)
    {
        // 1. DB'den credentials cek
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == N11MarketPlaceId)
            ?? throw new InvalidOperationException("N11 marketplace kaydi bulunamadı (Id=2).");

        // 2. bodyContent'in kopyasini olustur (caller'in XElement'ini mutate etmemek icin)
        var requestBody = new XElement(bodyContent);

        // 3. Auth elementini inject et (ilk child olarak)
        var authElement = new XElement("auth",
            new XElement("appKey", marketplace.ApiKey),
            new XElement("appSecret", marketplace.ApiSecret));

        requestBody.AddFirst(authElement);

        // 3. SOAP Envelope olustur
        var envelope = new XElement(SoapEnv + "Envelope",
            new XAttribute(XNamespace.Xmlns + "env", SoapEnv.NamespaceName),
            new XElement(SoapEnv + "Header"),
            new XElement(SoapEnv + "Body", requestBody));

        var xmlString = envelope.ToString(SaveOptions.DisableFormatting);

        // 4. HTTP SOAP istegi gonder
        var baseUrl = (marketplace.BaseUrl ?? DefaultBaseUrl).TrimEnd('/') + "/";
        var requestUrl = $"{baseUrl}{wsdlPath}";

        var client = httpClientFactory.CreateClient();
        var content = new StringContent(xmlString, Encoding.UTF8, "text/xml");

        if (!string.IsNullOrEmpty(soapAction))
            content.Headers.Add("SOAPAction", soapAction);

        logger.LogDebug("N11 SOAP request to {Url}: {Body}", requestUrl, xmlString);

        var response = await client.PostAsync(requestUrl, content);
        var responseXml = await response.Content.ReadAsStringAsync();

        logger.LogDebug("N11 SOAP response: {Body}", responseXml);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("N11 SOAP error (HTTP {StatusCode}): {ResponseBody}",
                (int)response.StatusCode, responseXml);
            throw new HttpRequestException(
                $"N11 SOAP istegi başarısız (HTTP {(int)response.StatusCode}). Response: {responseXml}");
        }

        // 5. Response parse et — Body'nin ilk child'ini don
        var responseDoc = XDocument.Parse(responseXml);
        var body = responseDoc.Descendants(SoapEnv + "Body").FirstOrDefault()
            ?? throw new InvalidOperationException("SOAP response'ta Body bulunamadı.");

        var firstChild = body.Elements().FirstOrDefault()
            ?? throw new InvalidOperationException("SOAP Body bos — response icerik yok.");

        return firstChild;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11SoapClientTests" -v m`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/N11/N11SoapClient.cs \
      Test/Entegrasyon.Test/N11/N11SoapClientTests.cs
git commit -m "feat(n11): implement N11SoapClient with SOAP envelope + auth injection"
```

---

## Task 5: IN11ProductService ve IN11StockPriceService Interfaceleri

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IN11ProductService.cs`
- Create: `Application/Entegrasyon.Business/Abstract/IN11StockPriceService.cs`

- [ ] **Step 1: Create IN11ProductService interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IN11ProductService.cs
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 urun servisi — urun kaydetme (SaveProduct) ve silme islemleri.
/// N11'de batch publish yerine tekil SaveProduct SOAP call'u kullanilir.
/// </summary>
public interface IN11ProductService
{
    /// <summary>
    /// Urunu N11'e kaydeder (SaveProduct SOAP call).
    /// Basariliysa N11 urun ID'sini doner.
    /// </summary>
    Task<IDataResult<long>> SaveProductAsync(Guid productId);

    /// <summary>
    /// N11'deki urunu siler (DeleteProductById).
    /// </summary>
    Task<IResult> DeleteProductAsync(Guid productId);
}
```

- [ ] **Step 2: Create IN11StockPriceService interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IN11StockPriceService.cs
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 stok/fiyat guncelleme servisi.
/// UpdateProductPriceByProductId + UpdateStockByStockId SOAP calllari.
/// </summary>
public interface IN11StockPriceService
{
    /// <summary>
    /// Bir urunun fiyatini N11'de gunceller.
    /// </summary>
    Task<IResult> UpdatePriceAsync(Guid productId, decimal newPrice);

    /// <summary>
    /// Bir urunun stogunu N11'de gunceller.
    /// </summary>
    Task<IResult> UpdateStockAsync(Guid productId, int quantity);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IN11ProductService.cs \
      Application/Entegrasyon.Business/Abstract/IN11StockPriceService.cs
git commit -m "feat(n11): add IN11ProductService and IN11StockPriceService interfaces"
```

---

## Task 6: MockN11ProductService

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs`
- Create: `Test/Entegrasyon.Test/N11/MockN11ProductServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// Test/Entegrasyon.Test/N11/MockN11ProductServiceTests.cs
using Entegrasyon.Business.Concrete.N11;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

public class MockN11ProductServiceTests
{
    private readonly Mock<ILogger<MockN11ProductService>> _loggerMock = new();

    [Fact]
    public async Task SaveProductAsync_Should_Return_Success_With_MockId()
    {
        var sut = new MockN11ProductService(_loggerMock.Object);
        var productId = Guid.NewGuid();

        var result = await sut.SaveProductAsync(productId);

        result.Success.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteProductAsync_Should_Return_Success()
    {
        var sut = new MockN11ProductService(_loggerMock.Object);
        var productId = Guid.NewGuid();

        var result = await sut.DeleteProductAsync(productId);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SaveProductAsync_Should_Return_Different_Ids_For_Different_Products()
    {
        var sut = new MockN11ProductService(_loggerMock.Object);

        var result1 = await sut.SaveProductAsync(Guid.NewGuid());
        var result2 = await sut.SaveProductAsync(Guid.NewGuid());

        result1.Data.Should().NotBe(result2.Data);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MockN11ProductServiceTests" -v m`
Expected: FAIL — `MockN11ProductService` does not exist.

- [ ] **Step 3: Write the MockN11ProductService**

```csharp
// Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// Mock N11 urun servisi — SaveProduct/Delete islemlerini loglar, success doner.
/// </summary>
public sealed class MockN11ProductService(
    ILogger<MockN11ProductService> logger) : IN11ProductService
{
    private long _mockIdCounter = 1000;

    public Task<IDataResult<long>> SaveProductAsync(Guid productId)
    {
        var n11ProductId = ++_mockIdCounter;

        logger.LogInformation("Mock: N11 SaveProduct — ProductId={ProductId}, N11Id={N11Id}",
            productId, n11ProductId);

        return Task.FromResult<IDataResult<long>>(
            new SuccessDataResult<long>(n11ProductId, "Urun N11'e kaydedildi (mock)."));
    }

    public Task<IResult> DeleteProductAsync(Guid productId)
    {
        logger.LogInformation("Mock: N11 DeleteProduct — ProductId={ProductId}", productId);

        return Task.FromResult<IResult>(
            new SuccessResult("Urun N11'den silindi (mock)."));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MockN11ProductServiceTests" -v m`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/N11/MockN11ProductService.cs \
      Test/Entegrasyon.Test/N11/MockN11ProductServiceTests.cs
git commit -m "feat(n11): implement MockN11ProductService"
```

---

## Task 7: MockN11StockPriceService

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/N11/MockN11StockPriceService.cs`
- Create: `Test/Entegrasyon.Test/N11/MockN11StockPriceServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// Test/Entegrasyon.Test/N11/MockN11StockPriceServiceTests.cs
using Entegrasyon.Business.Concrete.N11;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

public class MockN11StockPriceServiceTests
{
    private readonly Mock<ILogger<MockN11StockPriceService>> _loggerMock = new();

    [Fact]
    public async Task UpdatePriceAsync_Should_Return_Success()
    {
        var sut = new MockN11StockPriceService(_loggerMock.Object);

        var result = await sut.UpdatePriceAsync(Guid.NewGuid(), 99.99m);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStockAsync_Should_Return_Success()
    {
        var sut = new MockN11StockPriceService(_loggerMock.Object);

        var result = await sut.UpdateStockAsync(Guid.NewGuid(), 50);

        result.Success.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MockN11StockPriceServiceTests" -v m`
Expected: FAIL — `MockN11StockPriceService` does not exist.

- [ ] **Step 3: Write the MockN11StockPriceService**

```csharp
// Application/Entegrasyon.Business/Concrete/N11/MockN11StockPriceService.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// Mock N11 stok/fiyat servisi — islemleri loglar, success doner.
/// </summary>
public sealed class MockN11StockPriceService(
    ILogger<MockN11StockPriceService> logger) : IN11StockPriceService
{
    public Task<IResult> UpdatePriceAsync(Guid productId, decimal newPrice)
    {
        logger.LogInformation("Mock: N11 UpdatePrice — ProductId={ProductId}, Price={Price}",
            productId, newPrice);

        return Task.FromResult<IResult>(
            new SuccessResult($"Fiyat guncellendi: {newPrice:C} (mock)."));
    }

    public Task<IResult> UpdateStockAsync(Guid productId, int quantity)
    {
        logger.LogInformation("Mock: N11 UpdateStock — ProductId={ProductId}, Qty={Quantity}",
            productId, quantity);

        return Task.FromResult<IResult>(
            new SuccessResult($"Stok guncellendi: {quantity} adet (mock)."));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~MockN11StockPriceServiceTests" -v m`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/N11/MockN11StockPriceService.cs \
      Test/Entegrasyon.Test/N11/MockN11StockPriceServiceTests.cs
git commit -m "feat(n11): implement MockN11StockPriceService"
```

---

## Task 8: DB Migration — Seed N11 MarketPlace

**Files:**
- Create: EF Core migration via `dotnet ef migrations add`

- [ ] **Step 1: Create the migration**

Run:
```bash
dotnet ef migrations add SeedN11MarketPlace \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor
```

- [ ] **Step 2: Edit the generated migration**

Open the generated migration file and replace the `Up` method:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "MarketPlaces",
        columns: new[] { "Id", "Name", "IsDeleted", "CreatedAt" },
        values: new object[] { 2, "N11", false, new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc) });
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DeleteData(
        table: "MarketPlaces",
        keyColumn: "Id",
        keyValue: 2);
}
```

- [ ] **Step 3: Verify build after migration**

Run: `dotnet build Application/Entegrasyon.DataAccess/Entegrasyon.DataAccess.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "feat(n11): add SeedN11MarketPlace migration (Id=2)"
```

---

## Task 9: DI Registration — N11 Servisleri

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Add N11 service registrations**

In `AddApplicationDependencies`, after the Trendyol block (line ~105), add:

```csharp
// N11 servisleri
services.AddScoped<IN11SoapClient, N11SoapClient>();

// TODO: Sprint 3'te gercek implementasyonlar eklendiginde
// Trendyol pattern'i gibi N11:UseMock config ile mock/real ayrilacak.
services.AddScoped<IN11ProductService, MockN11ProductService>();
services.AddScoped<IN11StockPriceService, MockN11StockPriceService>();
```

N11 base URL'i DB'den (MarketPlace.BaseUrl) cekilir — N11SoapClient icinde. Named HttpClient
burada kaydetmeye gerek yok (Trendyol'dan farkli olarak N11 SOAP client URL'i runtime'da belirler).

Add required using directives at the top:

```csharp
using Entegrasyon.Business.Concrete.N11;
```

- [ ] **Step 2: Verify build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(n11): register N11 services in DI (SoapClient + mock product/stock)"
```

---

## Task 10: Full Test Suite Run

- [ ] **Step 1: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v m`
Expected: All tests pass (existing ~97 + new ~11 N11 tests)

- [ ] **Step 2: Run full solution build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded with no warnings in N11 files

- [ ] **Step 3: Final commit (if any fixes needed)**

```bash
git commit -m "fix(n11): resolve any remaining build/test issues"
```
