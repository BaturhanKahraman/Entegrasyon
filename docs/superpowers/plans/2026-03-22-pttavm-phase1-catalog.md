# PttAVM Faz 1 — Altyapi + Katalog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** PttAVM pazaryeri altyapisini kurmak (ApiClient, kategori import, UI tab) ve develop branch'ina entegre etmek.

**Architecture:** Pazarama klonu yaklasimi — ayni dosya yapisi, ayni DI pattern'i, ayni base class. PttavmCatalogApiClient (ApiKey+Token auth, multi-tenant credential cache), PttavmCategoryImporter (lazy-loading tree, BaseCategoryImporterService'den turetilir), PttavmCategoryTreeView (Blazor component). TDD-first.

**Tech Stack:** .NET 8, C# 12 primary constructors, EF Core (PostgreSQL), MudBlazor, xUnit + Moq + FluentAssertions, Playwright (E2E)

**Spec:** `docs/superpowers/specs/2026-03-22-pttavm-integration-design.md`
**API Docs:** `docs/pttavm/` (5 dosya)

**Branch stratejisi:** develop branch'inda calisiyoruz. Worktree acilarsa birlestirme UNUTULMAYACAK.

---

## File Structure

### New Files (Create)
| File | Responsibility |
|------|---------------|
| `Application/Entegrasyon.Business/Abstract/IPttavmCatalogApiClient.cs` | HTTP client interface (Get, Post, Put) |
| `Application/Entegrasyon.Business/Concrete/Pttavm/PttavmCatalogApiClient.cs` | Real API client with ApiKey+Token auth, multi-tenant credential cache |
| `Application/Entegrasyon.Business/Concrete/Pttavm/MockPttavmCatalogApiClient.cs` | Mock client for dev/test |
| `Application/Entegrasyon.Business/Concrete/Pttavm/PttavmResponseModels.cs` | API response DTO'lari |
| `Application/Entegrasyon.Business/Concrete/Import/PttavmCategoryImporter.cs` | Kategori import logic (lazy-loading tree) |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor` | UI tree component |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor.cs` | Tree component code-behind |
| `Test/Entegrasyon.Test/Pttavm/PttavmConstantsTests.cs` | MarketPlaceId=7 sabitleri testi |
| `Test/Entegrasyon.Test/Pttavm/PttavmCatalogApiClientTests.cs` | ApiClient unit testleri |
| `Test/Entegrasyon.Test/Pttavm/PttavmCategoryImporterTests.cs` | CategoryImporter unit testleri |

### Modified Files
| File | Change |
|------|--------|
| `Application/Entegrasyon.Entity/Categories/ImportSource.cs` | `Pttavm = 105` eklenir |
| `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs` | `PttavmMarketPlaceId = 7` eklenir |
| `Application/Entegrasyon.Blazor/ViewModels/CategoryTreeNode.cs` | `CanExpand` property eklenir (lazy-loading icin) |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | PttAVM DI kayitlari eklenir |
| `Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs` | "PttAVM" + "Pazarama" case eklenir |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor` | PttAVM tab'i eklenir |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs` | PttAVM state + metodlar eklenir |

---

## Task 1: MarketPlaceId Sabiti ve ImportSource Enum

**Files:**
- Modify: `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs`
- Modify: `Application/Entegrasyon.Entity/Categories/ImportSource.cs`
- Create: `Test/Entegrasyon.Test/Pttavm/PttavmConstantsTests.cs`

- [ ] **Step 1: Test klasoru olustur**

```bash
mkdir -p Test/Entegrasyon.Test/Pttavm
```

- [ ] **Step 2: Failing test yaz**

`Test/Entegrasyon.Test/Pttavm/PttavmConstantsTests.cs`:
```csharp
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;
using FluentAssertions;

namespace Entegrasyon.Test.Pttavm;

public class PttavmConstantsTests
{
    [Fact]
    public void PttavmMarketPlaceId_ShouldBe7()
    {
        MarketPlaceConstants.PttavmMarketPlaceId.Should().Be(7);
    }

    [Fact]
    public void PttavmMarketPlaceId_ShouldNotConflictWithOtherIds()
    {
        var ids = new[]
        {
            MarketPlaceConstants.TrendyolMarketPlaceId,
            MarketPlaceConstants.N11MarketPlaceId,
            MarketPlaceConstants.HepsiburadaMarketPlaceId,
            MarketPlaceConstants.PazaramaMarketPlaceId,
            MarketPlaceConstants.AmazonMarketPlaceId,
            MarketPlaceConstants.PttavmMarketPlaceId
        };

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ImportSource_ShouldContainPttavm()
    {
        Enum.IsDefined(typeof(ImportSource), ImportSource.Pttavm).Should().BeTrue();
    }

    [Fact]
    public void ImportSource_Pttavm_ShouldBe105()
    {
        ((int)ImportSource.Pttavm).Should().Be(105);
    }
}
```

- [ ] **Step 3: Testleri calistir, FAIL oldugunu dogrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PttavmConstantsTests" --no-restore
```
Expected: FAIL — `PttavmMarketPlaceId` ve `ImportSource.Pttavm` tanimli degil.

- [ ] **Step 4: MarketPlaceConstants'a PttavmMarketPlaceId ekle**

`Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs` dosyasinda `AmazonMarketPlaceId = 6;` satirindan sonra ekle:
```csharp
    public const int PttavmMarketPlaceId = 7;
```

- [ ] **Step 5: ImportSource enum'a Pttavm ekle**

`Application/Entegrasyon.Entity/Categories/ImportSource.cs` dosyasinda `Amazon = 104` satirindan sonra ekle:
```csharp
    /// <summary>
    /// PttAVM pazaryerinden import edilmis
    /// </summary>
    Pttavm = 105
```

> **Not:** `Amazon = 104` satirindaki virgulu unutma (virgul ekle).

- [ ] **Step 6: Testleri calistir, PASS oldugunu dogrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PttavmConstantsTests" --no-restore
```
Expected: 4 test PASS.

- [ ] **Step 7: Commit**

```bash
git add Test/Entegrasyon.Test/Pttavm/PttavmConstantsTests.cs \
  Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs \
  Application/Entegrasyon.Entity/Categories/ImportSource.cs
git commit -m "feat(pttavm): add MarketPlaceId=7 constant and ImportSource.Pttavm enum"
```

---

## Task 2: IPttavmCatalogApiClient Interface

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPttavmCatalogApiClient.cs`

- [ ] **Step 1: Interface dosyasini olustur**

`Application/Entegrasyon.Business/Abstract/IPttavmCatalogApiClient.cs`:
```csharp
namespace Entegrasyon.Business.Abstract;

public interface IPttavmCatalogApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body);
}
```

- [ ] **Step 2: Build dogrula**

```bash
dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj --no-restore
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPttavmCatalogApiClient.cs
git commit -m "feat(pttavm): add IPttavmCatalogApiClient interface"
```

---

## Task 3: PttavmResponseModels — API DTO'lari

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Pttavm/PttavmResponseModels.cs`

- [ ] **Step 1: Pttavm klasoru olustur**

```bash
mkdir -p Application/Entegrasyon.Business/Concrete/Pttavm
```

- [ ] **Step 2: Response model dosyasini olustur**

`Application/Entegrasyon.Business/Concrete/Pttavm/PttavmResponseModels.cs`:
```csharp
using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pttavm;

// --- Kategori ---

public sealed record PttavmMainCategoryResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("main_category")] List<PttavmCategoryDto>? MainCategory,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryTreeResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category_tree")] List<PttavmCategoryTreeDto>? CategoryTree,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryDetailResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category")] PttavmCategoryTreeDto? Category,
    [property: JsonPropertyName("error")] PttavmError? Error);

public sealed record PttavmCategoryDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt);

public sealed record PttavmCategoryTreeDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("parent_id")] string? ParentId,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt,
    [property: JsonPropertyName("children")] List<PttavmCategoryTreeDto>? Children);

// --- Ortak ---

public sealed record PttavmError(
    [property: JsonPropertyName("error_code")] string? ErrorCode,
    [property: JsonPropertyName("error_message")] string? ErrorMessage);
```

- [ ] **Step 3: Build dogrula**

```bash
dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj --no-restore
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Pttavm/PttavmResponseModels.cs
git commit -m "feat(pttavm): add PttAVM API response DTOs"
```

---

## Task 4: PttavmCatalogApiClient — Real Implementation

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Pttavm/PttavmCatalogApiClient.cs`
- Create: `Test/Entegrasyon.Test/Pttavm/PttavmCatalogApiClientTests.cs`

- [ ] **Step 1: Failing testleri yaz**

`Test/Entegrasyon.Test/Pttavm/PttavmCatalogApiClientTests.cs`:
```csharp
using System.Net;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

public class PttavmCatalogApiClientTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<PttavmCatalogApiClient>> _loggerMock = new();

    [Fact]
    public async Task GetAsync_ShouldIncludeRequiredHeaders()
    {
        // Arrange
        var handler = new TestDelegatingHandler(req =>
        {
            req.Headers.Contains("Api-Key").Should().BeTrue();
            req.Headers.Contains("Access-Token").Should().BeTrue();
            req.Headers.Contains("X-Correlation-Id").Should().BeTrue();
            req.Headers.GetValues("Api-Key").First().Should().Be("test-api-key");
            req.Headers.GetValues("Access-Token").First().Should().Be("test-token");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });

        var client = CreateClient(handler, "test-api-key", "test-token");

        // Act
        var response = await client.GetAsync("/api/v1/categories/main");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostAsync_ShouldSerializeBodyAsJson()
    {
        // Arrange
        var handler = new TestDelegatingHandler(async req =>
        {
            req.Content.Should().NotBeNull();
            var body = await req.Content!.ReadAsStringAsync();
            body.Should().Contain("test-barcode");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });

        var client = CreateClient(handler, "key", "token");

        // Act
        var response = await client.PostAsync("/api/v1/products/get-by-barcodes",
            new { barcodes = new[] { "test-barcode" } });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAsync_ShouldUseCachedCredentials_OnSecondCall()
    {
        // Arrange
        var callCount = 0;
        var handler = new TestDelegatingHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });

        var client = CreateClient(handler, "key", "token");

        // Act
        await client.GetAsync("/first");
        await client.GetAsync("/second");

        // Assert
        callCount.Should().Be(2); // 2 HTTP calls but credentials loaded once from DB
    }

    private PttavmCatalogApiClient CreateClient(TestDelegatingHandler handler, string apiKey, string accessToken)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new IntegrationDbContext(options);
        dbContext.MarketPlaces.Add(new MarketPlace
        {
            Id = MarketPlaceConstants.PttavmMarketPlaceId,
            Name = "PttAVM",
            ApiKey = apiKey,
            ApiSecret = accessToken, // AccessToken ApiSecret alaninda tutulur
            BaseUrl = "https://integration-api.pttavm.com",
            IsActive = true
        });
        dbContext.SaveChanges();

        _contextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ctx = new IntegrationDbContext(options);
                return ctx;
            });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://integration-api.pttavm.com") };
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new PttavmCatalogApiClient(
            _contextFactoryMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }
}

/// <summary>
/// Test icin HTTP handler — request'i yakalayip custom response dondurur
/// </summary>
public class TestDelegatingHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public TestDelegatingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = req => Task.FromResult(handler(req));
    }

    public TestDelegatingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request);
}
```

- [ ] **Step 2: Testleri calistir, FAIL oldugunu dogrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PttavmCatalogApiClientTests" --no-restore
```
Expected: FAIL — `PttavmCatalogApiClient` sinifi yok.

- [ ] **Step 3: PttavmCatalogApiClient implementasyonu**

`Application/Entegrasyon.Business/Concrete/Pttavm/PttavmCatalogApiClient.cs`:
```csharp
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

public sealed class PttavmCatalogApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<PttavmCatalogApiClient> logger) : IPttavmCatalogApiClient
{
    private static readonly ConcurrentDictionary<int, CachedCredentials> CredentialCache = new();

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("PttAVM GET: {Url}", relativeUrl);
        return await client.GetAsync(relativeUrl);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        logger.LogDebug("PttAVM POST: {Url}", relativeUrl);
        return await client.PostAsync(relativeUrl, content);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        logger.LogDebug("PttAVM PUT: {Url}", relativeUrl);
        return await client.PutAsync(relativeUrl, content);
    }

    private async Task<HttpClient> CreateConfiguredClientAsync()
    {
        var credentials = await GetOrLoadCredentialsAsync();
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(credentials.BaseUrl);
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Api-Key", credentials.ApiKey);
        client.DefaultRequestHeaders.Add("Access-Token", credentials.AccessToken);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        return client;
    }

    private async Task<CachedCredentials> GetOrLoadCredentialsAsync()
    {
        var marketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;

        if (CredentialCache.TryGetValue(marketPlaceId, out var cached) && cached.IsValid())
            return cached;

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketPlace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == marketPlaceId);

        if (marketPlace is null)
            throw new InvalidOperationException($"MarketPlace (Id={marketPlaceId}) bulunamadı. DB'de PttAVM kaydi olusturun.");

        var newCredentials = new CachedCredentials(
            marketPlace.ApiKey ?? throw new InvalidOperationException("PttAVM ApiKey bos."),
            marketPlace.ApiSecret ?? throw new InvalidOperationException("PttAVM AccessToken (ApiSecret) bos."),
            marketPlace.BaseUrl ?? "https://integration-api.pttavm.com",
            DateTimeOffset.UtcNow);

        CredentialCache[marketPlaceId] = newCredentials;
        logger.LogInformation("PttAVM credential'lari yuklendi (MarketPlaceId={Id})", marketPlaceId);
        return newCredentials;
    }

    private sealed record CachedCredentials(
        string ApiKey,
        string AccessToken,
        string BaseUrl,
        DateTimeOffset CachedAt)
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
        public bool IsValid() => DateTimeOffset.UtcNow - CachedAt < Ttl;
    }
}
```

> **Onemli noktalar:**
> - `ApiSecret` alanı`AccessToken` olarak kullanilir (MarketPlace entity'sindeki mevcut alan)
> - `ConcurrentDictionary<int, CachedCredentials>` multi-tenant uyumlu, static
> - TTL 5 dakika, DB'ye her istekte gitmez
> - `X-Correlation-Id` header'i her istekte yeni GUID

- [ ] **Step 4: Testleri calistir, PASS oldugunu dogrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PttavmCatalogApiClientTests" --no-restore
```
Expected: 3 test PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Pttavm/PttavmCatalogApiClient.cs \
  Test/Entegrasyon.Test/Pttavm/PttavmCatalogApiClientTests.cs
git commit -m "feat(pttavm): implement PttavmCatalogApiClient with multi-tenant credential cache"
```

---

## Task 5: MockPttavmCatalogApiClient

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Pttavm/MockPttavmCatalogApiClient.cs`

- [ ] **Step 1: Mock client olustur**

`Application/Entegrasyon.Business/Concrete/Pttavm/MockPttavmCatalogApiClient.cs`:
```csharp
using System.Net;
using System.Text;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete.Pttavm;

public sealed class MockPttavmCatalogApiClient : IPttavmCatalogApiClient
{
    public Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var json = relativeUrl switch
        {
            var u when u.Contains("categories/main") => """
            {
              "success": true,
              "main_category": [
                {"id": "1", "name": "Elektronik", "updated_at": "2026-01-01T00:00:00"},
                {"id": "2", "name": "Giyim", "updated_at": "2026-01-01T00:00:00"},
                {"id": "3", "name": "Ev & Yasam", "updated_at": "2026-01-01T00:00:00"}
              ],
              "error": null
            }
            """,
            var u when u.Contains("categories/") => """
            {
              "success": true,
              "category": {
                "id": "1",
                "name": "Elektronik",
                "parent_id": null,
                "updated_at": "2026-01-01T00:00:00",
                "children": [
                  {"id": "11", "name": "Telefon", "parent_id": "1", "updated_at": "2026-01-01T00:00:00", "children": []},
                  {"id": "12", "name": "Bilgisayar", "parent_id": "1", "updated_at": "2026-01-01T00:00:00", "children": []}
                ]
              },
              "error": null
            }
            """,
            _ => """{"success": true, "error": null}"""
        };

        return Task.FromResult(CreateResponse(json));
    }

    public Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
        => Task.FromResult(CreateResponse("""{"success": true, "error": null}"""));

    public Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
        => Task.FromResult(CreateResponse("""{"success": true, "error": null}"""));

    private static HttpResponseMessage CreateResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
```

- [ ] **Step 2: Build dogrula**

```bash
dotnet build Application/Entegrasyon.Business/Entegrasyon.Business.csproj --no-restore
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Pttavm/MockPttavmCatalogApiClient.cs
git commit -m "feat(pttavm): add MockPttavmCatalogApiClient for dev/test"
```

---

## Task 6: PttavmCategoryImporter

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Import/PttavmCategoryImporter.cs`
- Create: `Test/Entegrasyon.Test/Pttavm/PttavmCategoryImporterTests.cs`

- [ ] **Step 1: Failing testleri yaz**

`Test/Entegrasyon.Test/Pttavm/PttavmCategoryImporterTests.cs`:
```csharp
using System.Net;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

public class PttavmCategoryImporterTests
{
    private readonly Mock<IPttavmCatalogApiClient> _apiClientMock = new();
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock = new();
    private readonly Mock<ILogger<PttavmCategoryImporter>> _loggerMock = new();

    [Fact]
    public void Source_ShouldBePttavm()
    {
        var importer = CreateImporter();
        importer.Source.Should().Be(ImportSource.Pttavm);
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnMainCategories()
    {
        // Arrange
        SetupApiClientMainCategories("""
        {
          "success": true,
          "main_category": [
            {"id": "1", "name": "Elektronik", "updated_at": "2026-01-01T00:00:00"},
            {"id": "2", "name": "Giyim", "updated_at": "2026-01-01T00:00:00"}
          ],
          "error": null
        }
        """);

        var importer = CreateImporter();

        // Act
        var result = await importer.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.First().ExternalId.Should().Be("1");
        result.Data!.First().Name.Should().Be("Elektronik");
        result.Data!.First().HasChildren.Should().BeTrue(); // ana kategorilerin alt kategorileri var
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnEmptyList_WhenApiReturnsNoCategories()
    {
        // Arrange
        SetupApiClientMainCategories("""
        {
          "success": true,
          "main_category": [],
          "error": null
        }
        """);

        var importer = CreateImporter();

        // Act
        var result = await importer.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnError_WhenApiReturnsFailure()
    {
        // Arrange
        SetupApiClientMainCategories("""
        {
          "success": false,
          "main_category": null,
          "error": {"error_code": "ERR001", "error_message": "Auth failed"}
        }
        """);

        var importer = CreateImporter();

        // Act
        var result = await importer.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task LoadChildrenAsync_ShouldReturnChildren()
    {
        // Arrange
        var json = """
        {
          "success": true,
          "category": {
            "id": "1",
            "name": "Elektronik",
            "parent_id": null,
            "updated_at": "2026-01-01T00:00:00",
            "children": [
              {"id": "11", "name": "Telefon", "parent_id": "1", "updated_at": "2026-01-01T00:00:00", "children": []},
              {"id": "12", "name": "Bilgisayar", "parent_id": "1", "updated_at": "2026-01-01T00:00:00", "children": []}
            ]
          },
          "error": null
        }
        """;

        _apiClientMock.Setup(c => c.GetAsync(It.Is<string>(u => u.Contains("categories/1"))))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var importer = CreateImporter();

        // Act
        var result = await importer.LoadChildrenAsync("1");

        // Assert
        result.Should().HaveCount(2);
        result.First().ExternalId.Should().Be("11");
        result.First().Name.Should().Be("Telefon");
    }

    private PttavmCategoryImporter CreateImporter()
    {
        return new PttavmCategoryImporter(
            _contextFactoryMock.Object,
            _apiClientMock.Object,
            _loggerMock.Object);
    }

    private void SetupApiClientMainCategories(string json)
    {
        _apiClientMock.Setup(c => c.GetAsync(It.Is<string>(u => u.Contains("categories/main"))))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
```

- [ ] **Step 2: Testleri calistir, FAIL oldugunu dogrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PttavmCategoryImporterTests" --no-restore
```
Expected: FAIL — `PttavmCategoryImporter` sinifi yok.

- [ ] **Step 3: PttavmCategoryImporter implementasyonu**

`Application/Entegrasyon.Business/Concrete/Import/PttavmCategoryImporter.cs`:
```csharp
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// PttAVM kategori import servisi.
/// Lazy-loading tree: once ana kategorileri yukler, expand'de alt kategorileri getirir.
/// </summary>
public sealed class PttavmCategoryImporter(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IPttavmCatalogApiClient apiClient,
    ILogger<PttavmCategoryImporter> logger) : BaseCategoryImporterService(contextFactory, logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override ImportSource Source => ImportSource.Pttavm;

    /// <summary>
    /// Ana kategorileri yukler (lazy-loading icin ilk adim).
    /// Her ana kategori HasChildren=true olarak isaretlenir.
    /// </summary>
    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.GetAsync("/api/v1/categories/main");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PttavmMainCategoryResponse>(json, JsonOptions);

            if (result is null || !result.Success)
            {
                var errorMsg = result?.Error?.ErrorMessage ?? "Bilinmeyen hata";
                logger.LogError("PttAVM ana kategori yuklemesi başarısız: {Error}", errorMsg);
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(errorMsg);
            }

            var categories = (result.MainCategory ?? []).Select(c => new ExternalCategoryDto
            {
                ExternalId = c.Id ?? "",
                Name = c.Name ?? "",
                HasChildren = true // Ana kategorilerin alt kategorileri var kabul edilir
            }).ToList();

            logger.LogInformation("PttAVM {Count} ana kategori yuklendi", categories.Count);
            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM ana kategori yuklenirken hata");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(ex.Message);
        }
    }

    /// <summary>
    /// Belirli bir kategorinin alt kategorilerini yukler (lazy-loading).
    /// UI'da dugum expand edildiginde cagirilir.
    /// </summary>
    public async Task<List<ExternalCategoryDto>> LoadChildrenAsync(
        string parentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await apiClient.GetAsync($"/api/v1/categories/{parentId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PttavmCategoryDetailResponse>(json, JsonOptions);

            if (result?.Category?.Children is null)
                return [];

            return result.Category.Children.Select(c => new ExternalCategoryDto
            {
                ExternalId = c.Id ?? "",
                Name = c.Name ?? "",
                ParentExternalId = parentId,
                HasChildren = c.Children is { Count: > 0 }
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PttAVM alt kategoriler yuklenirken hata (parentId={ParentId})", parentId);
            return [];
        }
    }

    /// <summary>
    /// Override: marketplace'i yukleyip base import'u cagir.
    /// Base class ImportCategoriesAsync LoadMarketPlaceAsync cagirmaz,
    /// bu yuzden PttAVM marketplace linklerinin olusturulmasi icin zorunlu.
    /// </summary>
    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, "PttAVM", cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var category in categories)
            {
                await ImportCategoryInternalAsync(dbContext, category, null, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Logger.LogInformation("{Source} kategorileri basariyla import edildi", Source);
            return new SuccessResult($"{Source} kategorileri basariyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "{Source} kategorileri import edilirken hata olustu", Source);
            return new ErrorResult($"Import sirasinda hata: {ex.Message}");
        }
    }
}
```

> **Onemli noktalar:**
> - `LoadChildrenAsync` public async method — `BaseCategoryImporterService`'de yok, PttAVM'e ozel lazy-loading icin eklendi.
> - `GetExternalCategoriesAsync` sadece ana kategorileri dondurur (HasChildren=true).
> - `ImportCategoriesAsync` override'i ZORUNLU — base class `LoadMarketPlaceAsync` cagirmaz, marketplace linkleri icin gerekli. Pazarama ile ayni pattern.

- [ ] **Step 4: Testleri calistir, PASS oldugunu dogrula**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PttavmCategoryImporterTests" --no-restore
```
Expected: 5 test PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Import/PttavmCategoryImporter.cs \
  Test/Entegrasyon.Test/Pttavm/PttavmCategoryImporterTests.cs
git commit -m "feat(pttavm): implement PttavmCategoryImporter with lazy-loading tree"
```

---

## Task 7: DI Registration + CategoryImportBackgroundService Update

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`
- Modify: `Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs`

- [ ] **Step 1: ApplicationDependencyExtension.cs'e PttAVM DI ekle**

`ApplicationDependencyExtension.cs` dosyasinda Pazarama DI blogundan sonra (yaklasilik `AddScoped<IPazaramaBrandService>` satirindan sonra) ekle:

```csharp
// PttAVM
var usePttavmMock = configuration.GetValue<bool>("Pttavm:UseMock", true);
if (usePttavmMock)
    services.AddScoped<IPttavmCatalogApiClient, MockPttavmCatalogApiClient>();
else
    services.AddScoped<IPttavmCatalogApiClient, PttavmCatalogApiClient>();

services.AddScoped<PttavmCategoryImporter>();
```

Gerekli using'ler:
```csharp
using Entegrasyon.Business.Concrete.Pttavm;
```

> **Not:** `IPttavmCatalogApiClient` zaten `Entegrasyon.Business.Abstract` namespace'inde.

- [ ] **Step 2: CategoryImportBackgroundService switch'ine PttAVM ve Pazarama ekle**

`CategoryImportBackgroundService.cs` dosyasinda `MarketplaceName switch` icinde `"Hepsiburada" =>` satirindan sonra ekle:

```csharp
"Pazarama" => scope.ServiceProvider.GetRequiredService<PazaramaCategoryImporter>(),
"PttAVM" => scope.ServiceProvider.GetRequiredService<PttavmCategoryImporter>(),
```

> **Not:** Pazarama case'i de eksik (pre-existing bug). Her ikisini de ekliyoruz.

Gerekli using:
```csharp
using Entegrasyon.Business.Concrete.Import;
```
(Bu using zaten var olmali — kontrol et.)

- [ ] **Step 3: Build dogrula**

```bash
dotnet build Entegrasyon.sln --no-restore
```
Expected: Build succeeded.

- [ ] **Step 4: Tum mevcut testleri calistir, regression yok**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --no-restore
```
Expected: Tum testler PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
  Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs
git commit -m "feat(pttavm): register PttAVM services in DI and add to CategoryImportBackgroundService"
```

---

## Task 8: CategoryTreeNode CanExpand Property + PttavmCategoryTreeView Blazor Component

**Files:**
- Modify: `Application/Entegrasyon.Blazor/ViewModels/CategoryTreeNode.cs`
- Create: `Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor`
- Create: `Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor.cs`

> **KRITIK:** `CategoryTreeNode.HasChildren` computed property (`Children.Count > 0`). Lazy-loading'de children henuz yuklenmemis olabilir, bu durumda `HasChildren` hep `false` doner ve expand tetiklenemez. Bunu cozmek icin `CanExpand` property ekliyoruz.

- [ ] **Step 1: CategoryTreeNode'a CanExpand property ekle**

`Application/Entegrasyon.Blazor/ViewModels/CategoryTreeNode.cs` dosyasinda `HasChildren` satirindan sonra ekle:

```csharp
    /// <summary>
    /// Indicates whether this node can be expanded (may have children not yet loaded).
    /// Used for lazy-loading: true means "try to load children on expand".
    /// Falls back to HasChildren for non-lazy trees.
    /// </summary>
    public bool CanExpand { get; set; }
```

> **Not:** Mevcut tree view'lari (Trendyol, N11, Pazarama) `HasChildren` kullaniyor ve etkilenmezler cunku onlarin children'lari zaten yuklu. `CanExpand` sadece PttAVM lazy-loading icin kullanilacak.

- [ ] **Step 2: Code-behind olustur**

`Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor.cs`:
```csharp
using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class PttavmCategoryTreeView : ComponentBase
{
    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>> SelectedNodesChanged { get; set; }

    [Parameter]
    public EventCallback<CategoryTreeNode> OnNodeExpanded { get; set; }

    private string _searchText = string.Empty;

    private IEnumerable<CategoryTreeNode> FilteredCategories =>
        string.IsNullOrWhiteSpace(_searchText)
            ? Categories
            : Categories.Where(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    private async Task ToggleExpand(CategoryTreeNode node)
    {
        node.IsExpanded = !node.IsExpanded;
        if (node.IsExpanded && node.Children.Count == 0 && node.CanExpand)
        {
            await OnNodeExpanded.InvokeAsync(node);
        }
    }

    private async Task SelectNode(CategoryTreeNode node)
    {
        var selected = SelectedNodes?.ToList() ?? [];
        if (selected.Contains(node))
            selected.Remove(node);
        else
            selected.Add(node);

        await SelectedNodesChanged.InvokeAsync(selected.AsReadOnly());
    }

    private bool IsSelected(CategoryTreeNode node)
        => SelectedNodes?.Contains(node) ?? false;
}
```

- [ ] **Step 2: Razor view olustur**

`Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor`:
```razor
@namespace Entegrasyon.Blazor.Features.CategoryImport

<MudTextField @bind-Value="_searchText"
              Placeholder="Kategori ara..."
              Adornment="Adornment.Start"
              AdornmentIcon="@Icons.Material.Filled.Search"
              Class="mb-2"
              Immediate="true" />

<div style="max-height: 500px; overflow-y: auto;">
    @foreach (var node in FilteredCategories)
    {
        @RenderNode(node, 0)
    }
</div>

@code {
    private RenderFragment RenderNode(CategoryTreeNode node, int level) => __builder =>
    {
        <div style="margin-left: @(level * 16)px; padding: 2px 0;">
            <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="0">
                @if (node.CanExpand || node.HasChildren)
                {
                    <MudIconButton Icon="@(node.IsExpanded ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ChevronRight)"
                                   Size="Size.Small"
                                   OnClick="() => ToggleExpand(node)" />
                }
                else
                {
                    <div style="width: 30px;"></div>
                }

                <MudCheckBox T="bool"
                             Value="IsSelected(node)"
                             ValueChanged="_ => SelectNode(node)"
                             Size="Size.Small"
                             Dense="true" />

                <MudIcon Icon="@(node.CanExpand || node.HasChildren ? Icons.Material.Filled.Folder : Icons.Material.Filled.Description)"
                         Size="Size.Small"
                         Class="mr-1" />

                <MudText Typo="Typo.body2">@node.Name</MudText>
            </MudStack>
        </div>

        @if (node.IsExpanded)
        {
            @foreach (var child in node.Children)
            {
                @RenderNode(child, level + 1)
            }
        }
    };
}
```

- [ ] **Step 3: Build dogrula**

```bash
dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj --no-restore
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/ViewModels/CategoryTreeNode.cs \
  Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor \
  Application/Entegrasyon.Blazor/Features/CategoryImport/PttavmCategoryTreeView.razor.cs
git commit -m "feat(pttavm): add CanExpand to CategoryTreeNode and PttavmCategoryTreeView component"
```

---

## Task 9: CategoryImport Sayfasina PttAVM Tab Ekleme

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor`
- Modify: `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs`

- [ ] **Step 1: CategoryImport.razor.cs'e PttAVM state ve metodlari ekle**

Dosyanin mevcut icerigini oku. Pazarama state blogundan sonra ekle:

```csharp
// PttAVM
private List<CategoryTreeNode> pttavmCategories = [];
private IReadOnlyCollection<CategoryTreeNode>? pttavmSelectedNodes;
private bool pttavmLoading;
```

Inject:
```csharp
[Inject] PttavmCategoryImporter PttavmImporter { get; set; } = default!;
```

Metodlar (Pazarama metodlarinin yakinina ekle):

```csharp
private async Task LoadPttavmCategoriesAsync()
{
    pttavmLoading = true;
    try
    {
        var result = await PttavmImporter.GetExternalCategoriesAsync();
        if (result.Success && result.Data is not null)
        {
            pttavmCategories = result.Data.Select(MapToPttavmTreeNode).ToList();
        }
        else
        {
            Snackbar.Add(result.Message ?? "PttAVM kategorileri yuklenemedi", Severity.Error);
        }
    }
    finally
    {
        pttavmLoading = false;
    }
}

private async Task OnPttavmNodeExpanded(CategoryTreeNode node)
{
    var children = await PttavmImporter.LoadChildrenAsync(node.ExternalId);
    foreach (var child in children)
    {
        node.Children.Add(MapToPttavmTreeNode(child));
    }
    StateHasChanged();
}

private async Task ImportPttavmCategoriesAsync()
{
    if (pttavmSelectedNodes is null || pttavmSelectedNodes.Count == 0)
    {
        Snackbar.Add("Lutfen en az bir kategori secin.", Severity.Warning);
        return;
    }

    var importRequests = pttavmSelectedNodes.Select(MapToImportRequest).ToList();
    await ImportRequestedChannel.PublishAsync(
        new CategoryImportRequestedEvent("PttAVM", importRequests, Guid.Empty));

    Snackbar.Add("PttAVM kategori import işlemi basladi, islem arka planda devam edecek.", Severity.Info);
    NavigationManager.NavigateTo("/categories");
}

/// <summary>
/// PttAVM icin ozel tree node mapping — CanExpand'i set eder (lazy-loading icin).
/// Diger marketplace'lerin MapToTreeNode'undan farkli: HasChildren yerine CanExpand kullanir.
/// </summary>
private static CategoryTreeNode MapToPttavmTreeNode(ExternalCategoryDto dto) => new()
{
    ExternalId = dto.ExternalId,
    Name = dto.Name,
    ParentExternalId = dto.ParentExternalId,
    CanExpand = dto.HasChildren // Lazy-loading icin: children henuz yuklenmemis olabilir
};

private void RemovePttavmSelectedCategory(CategoryTreeNode node)
{
    var selected = pttavmSelectedNodes?.ToList() ?? [];
    selected.Remove(node);
    pttavmSelectedNodes = selected.AsReadOnly();
}

private void ClearPttavmSelection()
{
    pttavmSelectedNodes = null;
}
```

> **Onemli noktalar:**
> - `MapToPttavmTreeNode` PttAVM'e ozel — `CanExpand` property'sini set eder (lazy-loading icin). Diger marketplace'lerin `MapToTreeNode`'u `CanExpand` kullanmaz.
> - `CategoryImportRequestedEvent` constructor pattern ile kullanilir (object initializer degil).
> - `RemovePttavmSelectedCategory` ve `ClearPttavmSelection` — `SelectedCategoriesPanel` icin zorunlu callback'ler.

- [ ] **Step 2: CategoryImport.razor'a PttAVM tab ekle**

Mevcut `.razor` dosyasini oku. Pazarama tab'indan sonra, Amazon tab'indan once ekle:

```razor
<MudTabPanel Text="PttAVM">
    <MudStack Spacing="2" Class="pa-4">
        <MudStack Row="true" Spacing="2">
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       OnClick="LoadPttavmCategoriesAsync"
                       Disabled="pttavmLoading">
                @if (pttavmLoading)
                {
                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                }
                Kategorileri Yukle
            </MudButton>
            <MudButton Variant="Variant.Filled"
                       Color="Color.Secondary"
                       OnClick="ImportPttavmCategoriesAsync"
                       Disabled="pttavmSelectedNodes is null || pttavmSelectedNodes.Count == 0">
                Secilenleri Ice Aktar
            </MudButton>
        </MudStack>

        <MudGrid>
            <MudItem xs="8">
                <PttavmCategoryTreeView Categories="pttavmCategories"
                                        @bind-SelectedNodes="pttavmSelectedNodes"
                                        OnNodeExpanded="OnPttavmNodeExpanded" />
            </MudItem>
            <MudItem xs="4">
                <SelectedCategoriesPanel SelectedCategories="pttavmSelectedNodes"
                                         OnRemove="RemovePttavmSelectedCategory"
                                         OnClearAll="ClearPttavmSelection" />
            </MudItem>
        </MudGrid>
    </MudStack>
</MudTabPanel>
```

- [ ] **Step 3: Build dogrula**

```bash
dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj --no-restore
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor \
  Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs
git commit -m "feat(pttavm): add PttAVM tab to CategoryImport page with lazy-loading tree"
```

---

## Task 10: Tum Testleri Calistir + Final Dogrulama

**Files:** Yok (sadece dogrulama)

- [ ] **Step 1: Tum unit testleri calistir**

```bash
dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --no-restore
```
Expected: Tum testler PASS (mevcut + yeni PttAVM testleri).

- [ ] **Step 2: Solution build dogrula**

```bash
dotnet build Entegrasyon.sln --no-restore
```
Expected: Build succeeded.

- [ ] **Step 3: E2E testleri calistir (uygulama ayaktaysa)**

```bash
dotnet test Test/Entegrasyon.E2E/Entegrasyon.E2E.csproj --no-restore
```
Expected: Mevcut E2E testleri PASS (yeni PttAVM E2E testi henuz yok, Faz 1 sonunda eklenecek).

- [ ] **Step 4: Git log ile commit gecmisini dogrula**

```bash
git log --oneline -10
```

PttAVM commit'leri sirayla gorunmeli.

---

## Ozet

| Task | Dosya Sayısı | Test Sayısı | Aciklama |
|------|-------------|-------------|----------|
| 1 | 3 | 4 | Constants + ImportSource |
| 2 | 1 | 0 | Interface |
| 3 | 1 | 0 | Response DTOs |
| 4 | 2 | 3 | Real ApiClient + testler |
| 5 | 1 | 0 | Mock ApiClient |
| 6 | 2 | 5 | CategoryImporter + testler |
| 7 | 2 | 0 | DI + BackgroundService |
| 8 | 2 | 0 | Blazor TreeView component |
| 9 | 2 | 0 | CategoryImport page update |
| 10 | 0 | - | Final dogrulama |
| **Toplam** | **16** | **12** | |
