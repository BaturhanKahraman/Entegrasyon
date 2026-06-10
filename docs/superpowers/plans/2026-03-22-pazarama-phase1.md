# Pazarama Phase 1: Infrastructure + Category/Brand Import — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Pazarama marketplace (MarketPlaceId=4) support with OAuth2 API client, category tree import, and brand service.

**Architecture:** REST API client with OAuth2 client_credentials token caching (SemaphoreSlim, 55-min lazy refresh). Category importer extends `BaseCategoryImporterService`, converts Pazarama's flat category list to tree structure. GUID IDs stored in existing `ExternalId` string fields on match tables (attribute/value already have string columns from Hepsiburada; only `BrandMarketPlaceMatch` needs new column).

**Tech Stack:** .NET 8, C# 12, EF Core (PostgreSQL), Blazor Server (MudBlazor), xUnit + Moq + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-22-pazarama-phase1-design.md`

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `Application/Entegrasyon.Business/Abstract/IPazaramaApiClient.cs` | API client interface |
| `Application/Entegrasyon.Business/Abstract/IPazaramaBrandService.cs` | Brand service interface |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaApiClient.cs` | OAuth2 token-cached REST client |
| `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaApiClient.cs` | Static response mock for dev/test |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs` | Response DTOs |
| `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaBrandService.cs` | Brand fetch + import |
| `Application/Entegrasyon.Business/Concrete/Import/PazaramaCategoryImporter.cs` | Category tree import + attribute import |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor` | Tree view UI |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor.cs` | Tree view code-behind |
| `Test/Entegrasyon.Test/Pazarama/PazaramaConstantsTests.cs` | Constants tests |
| `Test/Entegrasyon.Test/Pazarama/PazaramaApiClientTests.cs` | API client + token tests |
| `Test/Entegrasyon.Test/Pazarama/PazaramaCategoryImporterTests.cs` | Category importer tests |
| `Test/Entegrasyon.Test/Pazarama/PazaramaBrandServiceTests.cs` | Brand service tests |

### Modified Files
| File | Change |
|------|--------|
| `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs` | Add `PazaramaMarketPlaceId = 4` |
| `Application/Entegrasyon.Entity/Categories/ImportSource.cs` | Add `Pazarama = 103` |
| `Application/Entegrasyon.Entity/MarketPlace.cs` | Add `TokenUrl` property |
| `Application/Entegrasyon.Entity/Matches/BrandMarketPlaceMatch.cs` | Add `MarketPlaceBrandExternalId` string field |
| `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Pazarama DI block |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor` | Add Pazarama tab |
| `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs` | Pazarama state + methods |

### Key Reference Files (DO NOT modify, only read for patterns)
- `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolApiClient.cs` — API client pattern
- `Application/Entegrasyon.Business/Concrete/Import/BaseCategoryImporterService.cs` — Import base class
- `Application/Entegrasyon.Business/Concrete/Import/N11CategoryImporter.cs` — Category/attribute import pattern
- `Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor` + `.razor.cs` — Tree view UI pattern
- `Test/Entegrasyon.Test/N11/N11CategoryImporterTests.cs` — Test pattern
- `Test/Entegrasyon.Test/BaseTest.cs` — Test base class (mockContextFactory, mockIntegrationDbContext)

---

## Task 1: Constants & Enum Updates

**Files:**
- Modify: `Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs`
- Modify: `Application/Entegrasyon.Entity/Categories/ImportSource.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaConstantsTests.cs`

- [ ] **Step 1: Write failing test for PazaramaMarketPlaceId**

```csharp
// Test/Entegrasyon.Test/Pazarama/PazaramaConstantsTests.cs
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Categories;
using FluentAssertions;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaConstantsTests
{
    [Fact]
    public void PazaramaMarketPlaceId_ShouldBe4()
    {
        MarketPlaceConstants.PazaramaMarketPlaceId.Should().Be(4);
    }

    [Fact]
    public void PazaramaMarketPlaceId_ShouldNotConflictWithOtherIds()
    {
        var ids = new[]
        {
            MarketPlaceConstants.TrendyolMarketPlaceId,
            MarketPlaceConstants.N11MarketPlaceId,
            MarketPlaceConstants.HepsiburadaMarketPlaceId,
            MarketPlaceConstants.PazaramaMarketPlaceId
        };
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ImportSource_ShouldContainPazarama()
    {
        Enum.IsDefined(typeof(ImportSource), ImportSource.Pazarama).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaConstantsTests" -v n`
Expected: FAIL — `PazaramaMarketPlaceId` does not exist, `ImportSource.Pazarama` does not exist

- [ ] **Step 3: Add constant and enum value**

In `MarketPlaceConstants.cs` add after line 7:
```csharp
public const int PazaramaMarketPlaceId = 4;
```

In `ImportSource.cs` add after `Hepsiburada = 102`:
```csharp
/// <summary>
/// Pazarama pazaryerinden import edilmiş
/// </summary>
Pazarama = 103
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaConstantsTests" -v n`
Expected: 3 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Business/Utility/Constants/MarketPlaceConstants.cs Application/Entegrasyon.Entity/Categories/ImportSource.cs Test/Entegrasyon.Test/Pazarama/PazaramaConstantsTests.cs
git commit -m "feat(pazarama): add PazaramaMarketPlaceId=4 constant and ImportSource.Pazarama enum"
```

---

## Task 2: MarketPlace Entity + Match Table + Migration

**Files:**
- Modify: `Application/Entegrasyon.Entity/MarketPlace.cs`
- Modify: `Application/Entegrasyon.Entity/Matches/BrandMarketPlaceMatch.cs`

**Important context:**
- `CategoryAttributeMarketPlaceMatch` already has `MarketPlaceCategoryAttributeExternalId` (string) — added for Hepsiburada. Pazarama will use this.
- `CategoryAttributeValueMarketPlaceMatch` already has `MarketPlaceCategoryAttributeValueExternalId` (string) — same.
- Only `BrandMarketPlaceMatch` is missing a string field.
- `CategoryMarketplace.ExternalCategoryId` (string) handles category GUIDs.

- [ ] **Step 1: Add TokenUrl to MarketPlace entity**

In `Application/Entegrasyon.Entity/MarketPlace.cs` add after `UserAgentPrefix` property:
```csharp
/// <summary>
/// OAuth2 token endpoint URL — Pazarama: https://isortagimgiris.pazarama.com/connect/token
/// </summary>
[StringLength(maximumLength: 200)]
public string? TokenUrl { get; set; }
```

- [ ] **Step 2: Add string field to BrandMarketPlaceMatch**

In `Application/Entegrasyon.Entity/Matches/BrandMarketPlaceMatch.cs` add after `MarketPlaceBrandId`:
```csharp
/// <summary>
/// String tipinde harici marka ID'si (Pazarama gibi GUID ID kullanan marketplace'ler için).
/// </summary>
public string? MarketPlaceBrandExternalId { get; set; }
```

- [ ] **Step 3: Generate EF migration**

Run: `dotnet ef migrations add AddPazaramaMarketplaceSupport -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor`

- [ ] **Step 4: Verify migration file is correct**

Read the generated migration and verify it adds:
- `TokenUrl` column to `MarketPlaces` table
- `MarketPlaceBrandExternalId` column to `BrandMarketPlaceMatches` table

- [ ] **Step 5: Create Pazarama seed migration**

Run: `dotnet ef migrations add SeedPazaramaMarketPlace -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor`

Then edit the generated migration file `Up` method:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "MarketPlaces",
        columns: new[] { "Id", "Name", "BaseUrl", "TokenUrl", "IsDeleted", "CreatedAt" },
        values: new object[] {
            4, "Pazarama",
            "https://isortagimapi.pazarama.com",
            "https://isortagimgiris.pazarama.com/connect/token",
            false, new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc)
        });
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DeleteData(
        table: "MarketPlaces",
        keyColumn: "Id",
        keyValue: 4);
}
```

- [ ] **Step 6: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Entity/MarketPlace.cs Application/Entegrasyon.Entity/Matches/BrandMarketPlaceMatch.cs Application/Entegrasyon.DataAccess/
git commit -m "feat(pazarama): add TokenUrl, BrandMarketPlaceMatch string field, seed MarketPlace Id=4"
```

---

## Task 3: Response DTOs

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs`

- [ ] **Step 1: Create response model file**

```csharp
// Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs
using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed record PazaramaResponse<T>(
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("messageCode")] string? MessageCode,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("userMessage")] string? UserMessage,
    [property: JsonPropertyName("fromCache")] bool FromCache);

public sealed record PazaramaCategoryDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("parentId")] Guid? ParentId,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("parentCategories")] List<string>? ParentCategories,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("displayOrder")] int DisplayOrder,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("leaf")] bool Leaf);

public sealed record PazaramaCategoryWithAttributesDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("attributes")] List<PazaramaCategoryAttributeDto> Attributes);

public sealed record PazaramaCategoryAttributeDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("isVariantable")] bool IsVariantable,
    [property: JsonPropertyName("isRequired")] bool IsRequired,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("attributeValues")] List<PazaramaCategoryAttributeValueDto> AttributeValues);

public sealed record PazaramaCategoryAttributeValueDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("value")] string Value);

public sealed record PazaramaBrandDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("logoUrl")] string? LogoUrl,
    [property: JsonPropertyName("website")] string? Website,
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("seoName")] string? SeoName);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaResponseModels.cs
git commit -m "feat(pazarama): add Pazarama response DTOs"
```

---

## Task 4: IPazaramaApiClient Interface + PazaramaApiClient + MockPazaramaApiClient

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPazaramaApiClient.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaApiClient.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaApiClient.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaApiClientTests.cs`

**Reference:** `Application/Entegrasyon.Business/Concrete/Trendyol/TrendyolApiClient.cs` (credential fetch + configured client pattern)
**Reference:** `Application/Entegrasyon.Business/Abstract/ITrendyolApiClient.cs` (interface pattern)

- [ ] **Step 1: Write failing tests for PazaramaApiClient**

```csharp
// Test/Entegrasyon.Test/Pazarama/PazaramaApiClientTests.cs
using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaApiClientTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<PazaramaApiClient>> _loggerMock = new();

    private PazaramaApiClient CreateSut() => new(
        mockContextFactory.Object,
        _httpClientFactoryMock.Object,
        _loggerMock.Object);

    // NOTE: PazaramaApiClient uses IDbContextFactory + EF Core FirstOrDefaultAsync.
    // Mocking EF Core async queries with Moq is complex. The implementor should
    // check existing test patterns in the project (e.g., how TrendyolApiClient or
    // N11SoapClient tests mock DbContext queries) and adapt accordingly.
    // Common approaches:
    //   a) Use MockQueryable.Moq NuGet package for async LINQ mocking
    //   b) Use InMemory EF Core provider for integration-style tests
    //   c) Create a DbSetMockHelper utility similar to what other tests use
    //
    // The test cases below describe WHAT to test — adapt the mock setup to
    // whichever approach works with the existing test infrastructure.

    [Fact]
    public async Task GetAsync_ShouldFetchTokenAndMakeRequest()
    {
        // Arrange: Setup MarketPlace mock (Id=4, Pazarama) in mockIntegrationDbContext.MarketPlaces
        // Setup IHttpClientFactory to return HttpClient with mock handler:
        //   - First call (token endpoint): return {"access_token":"test-token","expires_in":3600,"token_type":"Bearer"}
        //   - Second call (API endpoint): return 200 OK with "{}"
        // Act: call sut.GetAsync("category/getCategoryTree")
        // Assert: result.StatusCode == HttpStatusCode.OK
        throw new NotImplementedException("Adapt mock setup to project's test infrastructure");
    }

    [Fact]
    public async Task GetAsync_WhenMarketPlaceNotFound_ShouldThrow()
    {
        // Arrange: Setup empty MarketPlaces DbSet (no Id=4 record)
        // Act & Assert: sut.GetAsync("test") should throw InvalidOperationException with "*Pazarama*"
        throw new NotImplementedException("Adapt mock setup to project's test infrastructure");
    }

    [Fact]
    public async Task GetAsync_ShouldCacheTokenOnSecondCall()
    {
        // Arrange: Setup MarketPlace + HttpClient mock
        // Act: call sut.GetAsync() twice
        // Assert: token endpoint called only once (cached on second call)
        throw new NotImplementedException("Adapt mock setup to project's test infrastructure");
    }

    [Fact]
    public async Task GetAsync_ShouldAddBearerAuthorizationHeader()
    {
        // Arrange: Setup MarketPlace + HttpClient mock
        // Act: call sut.GetAsync()
        // Assert: verify the API request has Authorization: Bearer test-token header
        throw new NotImplementedException("Adapt mock setup to project's test infrastructure");
    }
}
```

**Note:** `TestHelpers.CreateMockDbSet` should be available in the test project, or you may need to create a similar helper. Check existing tests for the pattern used. The tests above are a starting point — add more during implementation based on what `BaseTest` provides.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaApiClientTests" -v n`
Expected: FAIL — `IPazaramaApiClient` and `PazaramaApiClient` do not exist

- [ ] **Step 3: Create IPazaramaApiClient interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IPazaramaApiClient.cs
namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Pazarama API'ye OAuth2 Bearer token ile HTTP çağrıları yapan wrapper.
/// MarketPlace tablosundan (Id=4) clientId/clientSecret çeker,
/// token caching ile otomatik yenileme yapar.
/// </summary>
public interface IPazaramaApiClient
{
    Task<HttpResponseMessage> GetAsync(string relativeUrl);
    Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body);
    Task<HttpResponseMessage> DeleteAsync(string relativeUrl);
}
```

- [ ] **Step 4: Create PazaramaApiClient implementation**

```csharp
// Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaApiClient.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama API'ye OAuth2 Bearer token ile credential-aware HTTP çağrıları yapan client.
/// Token in-memory cache'lenir, expire'a 5 dk kala lazy refresh yapılır.
/// Thread-safe: SemaphoreSlim ile concurrent token refresh korunur.
/// </summary>
public sealed class PazaramaApiClient(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<PazaramaApiClient> logger) : IPazaramaApiClient
{
    private const string DefaultBaseUrl = "https://isortagimapi.pazarama.com";
    private const string DefaultTokenUrl = "https://isortagimgiris.pazarama.com/connect/token";
    private static readonly TimeSpan TokenRefreshBuffer = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public async Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Pazarama GET: {Url}", relativeUrl);
        return await client.GetAsync(relativeUrl);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Pazarama POST: {Url}", relativeUrl);
        return await client.PostAsJsonAsync(relativeUrl, body);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Pazarama PUT: {Url}", relativeUrl);
        return await client.PutAsJsonAsync(relativeUrl, body);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string relativeUrl)
    {
        var client = await CreateConfiguredClientAsync();
        logger.LogDebug("Pazarama DELETE: {Url}", relativeUrl);
        return await client.DeleteAsync(relativeUrl);
    }

    private async Task<HttpClient> CreateConfiguredClientAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == PazaramaMarketPlaceId)
            ?? throw new InvalidOperationException("Pazarama marketplace kaydı bulunamadı (Id=4).");

        var baseUrl = marketplace.BaseUrl ?? DefaultBaseUrl;
        var tokenUrl = marketplace.TokenUrl ?? DefaultTokenUrl;

        await EnsureValidTokenAsync(marketplace.ApiKey!, marketplace.ApiSecret!, tokenUrl);

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        return client;
    }

    private async Task EnsureValidTokenAsync(string clientId, string clientSecret, string tokenUrl)
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt - TokenRefreshBuffer)
            return;

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt - TokenRefreshBuffer)
                return;

            logger.LogInformation("Pazarama OAuth2 token alınıyor...");

            var client = httpClientFactory.CreateClient();

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = "merchantgatewayapi.fullaccess"
            });

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>()
                ?? throw new InvalidOperationException("Pazarama token yanıtı boş.");

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            logger.LogInformation("Pazarama OAuth2 token alındı, {ExpiresIn}s geçerli", tokenResponse.ExpiresIn);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType);
}
```

- [ ] **Step 5: Create MockPazaramaApiClient**

```csharp
// Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaApiClient.cs
using System.Net;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Geliştirme/test için statik Pazarama API yanıtları döner.
/// Pazarama:UseMock=true olduğunda DI tarafından kullanılır.
/// </summary>
public sealed class MockPazaramaApiClient(ILogger<MockPazaramaApiClient> logger) : IPazaramaApiClient
{
    public Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        logger.LogDebug("MockPazarama GET: {Url}", relativeUrl);
        var json = GetMockResponse(relativeUrl);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    public Task<HttpResponseMessage> PostAsync<T>(string relativeUrl, T body)
    {
        logger.LogDebug("MockPazarama POST: {Url}", relativeUrl);
        var json = """{"data":null,"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false}""";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    public Task<HttpResponseMessage> PutAsync<T>(string relativeUrl, T body) => PostAsync(relativeUrl, body);
    public Task<HttpResponseMessage> DeleteAsync(string relativeUrl) => GetAsync(relativeUrl);

    private static string GetMockResponse(string relativeUrl) => relativeUrl switch
    {
        _ when relativeUrl.Contains("category/getCategoryTree") => """
        {"data":[],"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false}
        """,
        _ when relativeUrl.Contains("brand/getBrands") => """
        {"data":[],"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false}
        """,
        _ when relativeUrl.Contains("category/getCategoryWithAttributes") => """
        {"data":{"id":"00000000-0000-0000-0000-000000000000","name":"Mock","displayName":"Mock","description":null,"attributes":[]},"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false}
        """,
        _ => """{"data":null,"success":true,"messageCode":null,"message":null,"userMessage":null,"fromCache":false}"""
    };
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaApiClientTests" -v n`
Expected: PASS (adjust tests based on actual BaseTest/mock patterns if needed)

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPazaramaApiClient.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaApiClient.cs Application/Entegrasyon.Business/Concrete/Pazarama/MockPazaramaApiClient.cs Test/Entegrasyon.Test/Pazarama/PazaramaApiClientTests.cs
git commit -m "feat(pazarama): add PazaramaApiClient with OAuth2 token caching + mock"
```

---

## Task 5: DI Registration

**Files:**
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Add Pazarama DI block**

In `ApplicationDependencyExtension.cs`, after the N11 services block (after line 151), add:
```csharp
// Pazarama servisleri
var usePazaramaMock = configuration.GetValue<bool>("Pazarama:UseMock", true);
if (usePazaramaMock)
{
    services.AddScoped<IPazaramaApiClient, MockPazaramaApiClient>();
}
else
{
    services.AddScoped<IPazaramaApiClient, PazaramaApiClient>();
}
```

Add required usings at the top:
```csharp
using Entegrasyon.Business.Concrete.Pazarama;
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 3: Run all existing tests to ensure nothing is broken**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(pazarama): register Pazarama services in DI with mock toggle"
```

---

## Task 6: PazaramaCategoryImporter

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Import/PazaramaCategoryImporter.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaCategoryImporterTests.cs`

**Reference:** `Application/Entegrasyon.Business/Concrete/Import/N11CategoryImporter.cs`
**Reference:** `Application/Entegrasyon.Business/Concrete/Import/BaseCategoryImporterService.cs`
**Reference:** `Test/Entegrasyon.Test/N11/N11CategoryImporterTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/Pazarama/PazaramaCategoryImporterTests.cs
using System.Net;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Categories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaCategoryImporterTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaCategoryImporter>> _loggerMock = new();

    private PazaramaCategoryImporter CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _loggerMock.Object);

    private HttpResponseMessage CreateCategoryResponse(List<PazaramaCategoryDto> categories)
    {
        var response = new PazaramaResponse<List<PazaramaCategoryDto>>(categories, true, null, null, null, false);
        var json = JsonSerializer.Serialize(response);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public void Source_ShouldBePazarama()
    {
        var sut = CreateSut();
        sut.Source.Should().Be(ImportSource.Pazarama);
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldConvertFlatListToTree()
    {
        // Arrange — 3-level hierarchy: Root > Child > Leaf
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var leafId = Guid.NewGuid();

        var flatCategories = new List<PazaramaCategoryDto>
        {
            new(rootId, null, null, new List<string> { "Elektronik" }, "Elektronik", "Elektronik", 1, null, false),
            new(childId, rootId, null, new List<string> { "Elektronik", "Telefon" }, "Telefon", "Telefon", 1, null, false),
            new(leafId, childId, null, new List<string> { "Elektronik", "Telefon", "Akıllı Telefon" }, "Akıllı Telefon", "Akıllı Telefon", 1, null, true)
        };

        _apiClientMock
            .Setup(c => c.GetAsync(It.Is<string>(u => u.Contains("category/getCategoryTree"))))
            .ReturnsAsync(CreateCategoryResponse(flatCategories));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        var roots = result.Data!.ToList();
        roots.Should().HaveCount(1);
        roots[0].Name.Should().Be("Elektronik");
        roots[0].HasChildren.Should().BeTrue();
        roots[0].Children.Should().HaveCount(1);
        roots[0].Children.First().Name.Should().Be("Telefon");
        roots[0].Children.First().Children.Should().HaveCount(1);
        roots[0].Children.First().Children.First().Name.Should().Be("Akıllı Telefon");
        roots[0].Children.First().Children.First().HasChildren.Should().BeFalse(); // leaf
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_WhenApiReturnsEmpty_ShouldReturnEmptyList()
    {
        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateCategoryResponse(new List<PazaramaCategoryDto>()));

        var sut = CreateSut();

        var result = await sut.GetExternalCategoriesAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_WhenApiFails_ShouldReturnError()
    {
        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API bağlantı hatası"));

        var sut = CreateSut();

        var result = await sut.GetExternalCategoriesAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("API bağlantı hatası");
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_OrphanedCategory_ShouldTreatAsRoot()
    {
        // parentId points to a non-existent category
        var orphanId = Guid.NewGuid();
        var missingParentId = Guid.NewGuid();

        var flatCategories = new List<PazaramaCategoryDto>
        {
            new(orphanId, missingParentId, null, null, "Orphan", "Orphan", 1, null, true)
        };

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateCategoryResponse(flatCategories));

        var sut = CreateSut();

        var result = await sut.GetExternalCategoriesAsync();

        result.Success.Should().BeTrue();
        result.Data!.Should().HaveCount(1);
        result.Data!.First().Name.Should().Be("Orphan");
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_LeafCategory_ShouldHaveNoChildren()
    {
        var id = Guid.NewGuid();
        var flatCategories = new List<PazaramaCategoryDto>
        {
            new(id, null, null, null, "Leaf", "Leaf", 1, null, true) // leaf=true
        };

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateCategoryResponse(flatCategories));

        var sut = CreateSut();
        var result = await sut.GetExternalCategoriesAsync();

        result.Data!.First().HasChildren.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaCategoryImporterTests" -v n`
Expected: FAIL — `PazaramaCategoryImporter` does not exist

- [ ] **Step 3: Implement PazaramaCategoryImporter**

```csharp
// Application/Entegrasyon.Business/Concrete/Import/PazaramaCategoryImporter.cs
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Import;

public class PazaramaCategoryImporter : BaseCategoryImporterService
{
    private readonly IPazaramaApiClient _apiClient;

    public override ImportSource Source => ImportSource.Pazarama;

    public PazaramaCategoryImporter(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IPazaramaApiClient apiClient,
        ILogger<PazaramaCategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _apiClient = apiClient;
    }

    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _apiClient.GetAsync("category/getCategoryTree");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<PazaramaResponse<List<PazaramaCategoryDto>>>(
                cancellationToken: cancellationToken);

            if (apiResponse?.Data == null || !apiResponse.Success)
            {
                Logger.LogWarning("Pazarama kategori API'si başarısız yanıt döndü: {Message}", apiResponse?.Message);
                return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null, apiResponse?.Message ?? "Pazarama kategori API yanıtı boş.");
            }

            var tree = BuildTreeFromFlatList(apiResponse.Data);
            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(tree);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Pazarama kategorileri çekilirken hata oluştu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(null, $"Hata: {ex.Message}");
        }
    }

    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, "Pazarama", cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var category in categories)
            {
                await ImportCategoryInternalAsync(dbContext, category, null, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Logger.LogInformation("{Source} kategorileri başarıyla import edildi", Source);
            return new SuccessResult($"{Source} kategorileri başarıyla import edildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Logger.LogError(ex, "{Source} kategorileri import edilirken hata oluştu", Source);
            return new ErrorResult($"Import sırasında hata: {ex.Message}");
        }
    }

    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;
        if (MarketPlace == null) return;

        try
        {
            var response = await _apiClient.GetAsync($"category/getCategoryWithAttributes?Id={category.ExternalCategoryId}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<PazaramaResponse<PazaramaCategoryWithAttributesDto>>(
                cancellationToken: cancellationToken);

            if (apiResponse?.Data?.Attributes == null || !apiResponse.Data.Attributes.Any()) return;

            var categoryAttributeCategories = new List<CategoryAttributeCategory>();

            foreach (var attr in apiResponse.Data.Attributes)
            {
                var dbCatAttr = await GetOrCreateAttributeAsync(dbContext, attr, cancellationToken);

                categoryAttributeCategories.Add(new CategoryAttributeCategory
                {
                    Category = category,
                    CategoryAttribute = dbCatAttr.Attribute,
                    IsRequired = attr.IsRequired,
                    IsVarianter = attr.IsVariantable,
                    IsSlicer = false
                });

                if (dbCatAttr.IsNew)
                {
                    foreach (var val in attr.AttributeValues)
                    {
                        await AddAttributeValueAsync(dbContext, val, dbCatAttr.Attribute, cancellationToken);
                    }
                }
            }

            await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Pazarama kategori özellikleri import edilirken hata: {CategoryId}", category.ExternalCategoryId);
        }
    }

    private List<ExternalCategoryDto> BuildTreeFromFlatList(List<PazaramaCategoryDto> flatList)
    {
        var lookup = new Dictionary<Guid, ExternalCategoryDto>();
        var roots = new List<ExternalCategoryDto>();

        // First pass: create all nodes
        foreach (var dto in flatList)
        {
            lookup[dto.Id] = new ExternalCategoryDto
            {
                ExternalId = dto.Id.ToString(),
                Name = dto.Name,
                ParentExternalId = dto.ParentId?.ToString(),
                HasChildren = !dto.Leaf,
                Children = new List<ExternalCategoryDto>()
            };
        }

        // Second pass: build parent-child relationships
        foreach (var dto in flatList)
        {
            var node = lookup[dto.Id];

            if (dto.ParentId.HasValue && lookup.TryGetValue(dto.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                if (dto.ParentId.HasValue)
                {
                    Logger.LogWarning("Orphaned kategori root olarak ele alınıyor: {Id} (parentId={ParentId})",
                        dto.Id, dto.ParentId);
                }
                roots.Add(node);
            }
        }

        return roots;
    }

    private async Task<(CategoryAttribute Attribute, bool IsNew)> GetOrCreateAttributeAsync(
        IntegrationDbContext dbContext,
        PazaramaCategoryAttributeDto attr,
        CancellationToken cancellationToken)
    {
        var guidString = attr.Id.ToString();

        var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
            .Include(m => m.ApplicationCategoryAttribute)
            .FirstOrDefaultAsync(
                m => m.MarketPlaceCategoryAttributeExternalId == guidString
                  && m.MarketPlaceId == MarketPlace!.Id,
                cancellationToken);

        if (existingMatch != null)
            return (existingMatch.ApplicationCategoryAttribute, false);

        var newAttr = new CategoryAttribute
        {
            CategoryAttributeKey = attr.Name,
            CategoryAttributeHumanized = attr.DisplayName ?? attr.Name,
            AllowCustom = false,
            CategoryAttributeValues = new List<CategoryAttributeValue>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.CategoryAttributes.AddAsync(newAttr, cancellationToken);

        var match = new CategoryAttributeMarketPlaceMatch
        {
            MarketPlace = MarketPlace!,
            ApplicationCategoryAttribute = newAttr,
            MarketPlaceCategoryAttributeId = 0,
            MarketPlaceCategoryAttributeExternalId = guidString
        };

        await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(match, cancellationToken);

        return (newAttr, true);
    }

    private async Task AddAttributeValueAsync(
        IntegrationDbContext dbContext,
        PazaramaCategoryAttributeValueDto val,
        CategoryAttribute categoryAttribute,
        CancellationToken cancellationToken)
    {
        var value = new CategoryAttributeValue
        {
            Name = val.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        categoryAttribute.CategoryAttributeValues.Add(value);

        if (MarketPlace != null)
        {
            var valueMatch = new CategoryAttributeValueMarketPlaceMatch
            {
                MarketPlace = MarketPlace,
                ApplicationCategoryAttributeValue = value,
                MarketPlaceCategoryAttributeValueId = 0,
                MarketPlaceCategoryAttributeValueExternalId = val.Id.ToString()
            };
            await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(valueMatch, cancellationToken);
        }
    }
}
```

- [ ] **Step 4: Add DI registration for PazaramaCategoryImporter**

In `ApplicationDependencyExtension.cs`, in the Pazarama DI block add:
```csharp
services.AddScoped<PazaramaCategoryImporter>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaCategoryImporterTests" -v n`
Expected: All tests PASS

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Import/PazaramaCategoryImporter.cs Test/Entegrasyon.Test/Pazarama/PazaramaCategoryImporterTests.cs Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(pazarama): add PazaramaCategoryImporter with flat-to-tree conversion"
```

---

## Task 7: Blazor UI — Pazarama Tab + TreeView

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor`
- Create: `Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor.cs`
- Modify: `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor`
- Modify: `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs`

**Reference:** `N11CategoryTreeView.razor` + `.razor.cs` (same folder)

- [ ] **Step 1: Create PazaramaCategoryTreeView.razor.cs**

```csharp
// Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor.cs
using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class PazaramaCategoryTreeView
{
    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>?> SelectedNodesChanged { get; set; }

    private void ToggleExpand(CategoryTreeNode node)
    {
        node.IsExpanded = !node.IsExpanded;
    }

    private void SelectNode(CategoryTreeNode node)
    {
        var list = SelectedNodes?.ToList() ?? new List<CategoryTreeNode>();
        if (list.Contains(node))
            list.Remove(node);
        else
            list.Add(node);
        SelectedNodes = list;
        SelectedNodesChanged.InvokeAsync(SelectedNodes);
    }

    private bool IsSelected(CategoryTreeNode node) => SelectedNodes?.Contains(node) == true;
}
```

- [ ] **Step 2: Create PazaramaCategoryTreeView.razor**

```razor
@* Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor *@
@using Entegrasyon.Blazor.ViewModels

<MudPaper Elevation="0" Outlined="true" Class="pa-4" Style="height: 600px; overflow-y: auto;">
    <MudText Typo="Typo.subtitle1" Class="mb-2">Pazarama Kategori Agaci</MudText>

    @if (!Categories.Any())
    {
        <MudText Align="Align.Center" Color="Color.Secondary" Class="mt-4">
            Kategori bulunamadı. "Kategorileri Yukle" butonuna tiklayin.
        </MudText>
    }
    else
    {
        @foreach (var node in Categories)
        {
            @RenderNode(node, 0)
        }
    }
</MudPaper>

@code {
    private RenderFragment RenderNode(CategoryTreeNode node, int level) =>
        @<div>
            <div style="display: flex; align-items: center; padding: 6px 0; margin-left: @(level * 16)px;">
                @if (node.Children.Any())
                {
                    <MudIconButton Icon="@(node.IsExpanded ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ChevronRight)"
                                   Size="Size.Small"
                                   Style="margin-right: 4px;"
                                   OnClick="@(() => ToggleExpand(node))" />
                }
                else
                {
                    <div style="width: 30px;"></div>
                }

                <MudCheckBox T="bool"
                             Value="IsSelected(node)"
                             ValueChanged="@(_ => SelectNode(node))"
                             Color="Color.Primary"
                             Style="margin-right: 4px;" />

                <MudIcon Icon="@(node.Children.Any() ? Icons.Material.Filled.Folder : Icons.Material.Filled.Description)"
                         Size="Size.Small"
                         Style="margin-right: 4px;" />

                <MudText>@node.Name</MudText>
            </div>

            @if (node.IsExpanded && node.Children.Any())
            {
                @foreach (var child in node.Children)
                {
                    @RenderNode(child, level + 1)
                }
            }
        </div>;
}
```

- [ ] **Step 3: Add Pazarama state and methods to CategoryImport.razor.cs**

Add at the top of `CategoryImport.razor.cs` (imports and inject):
```csharp
[Inject]
private PazaramaCategoryImporter PazaramaImporter { get; set; } = null!;
```

Add state fields (after `hbLoading`):
```csharp
// Pazarama state
private List<CategoryTreeNode> pazaramaCategories = [];
private IReadOnlyCollection<CategoryTreeNode>? pazaramaSelectedNodes;
private bool pazaramaLoading;
```

Add methods (after `ClearHbSelection()`):
```csharp
// Pazarama methods
private async Task LoadPazaramaCategoriesAsync()
{
    pazaramaLoading = true;
    var result = await PazaramaImporter.GetExternalCategoriesAsync();
    if (result.Success && result.Data != null)
    {
        pazaramaCategories = result.Data.Select(MapToTreeNode).ToList();
        var totalCount = CountAllCategories(pazaramaCategories);
        Snackbar.Add($"{totalCount} Pazarama kategori yuklendi.", Severity.Success);
    }
    else
    {
        Snackbar.Add(result.Message ?? "Pazarama kategorileri yuklenemedi.", Severity.Error);
    }
    pazaramaLoading = false;
}

private async Task ImportPazaramaCategoriesAsync()
{
    if (pazaramaSelectedNodes == null || !pazaramaSelectedNodes.Any())
    {
        Snackbar.Add("Lutfen en az bir kategori secin.", Severity.Warning);
        return;
    }

    importing = true;
    try
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var importRequests = pazaramaSelectedNodes.Select(MapToImportRequest).ToList();
        var importEvent = new CategoryImportRequestedEvent("Pazarama", importRequests, userId);
        await ImportRequestedChannel.PublishAsync(importEvent);
        Snackbar.Add("Pazarama kategori ice aktarma işlemi baslatildi.", Severity.Info);
        pazaramaSelectedNodes = null;
        NavigationManager.NavigateTo("/categories");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Pazarama category import request failed");
        Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
    }
    finally
    {
        importing = false;
    }
}

private void RemovePazaramaSelectedCategory(CategoryTreeNode node)
{
    if (pazaramaSelectedNodes != null)
    {
        var list = pazaramaSelectedNodes.ToList();
        list.Remove(node);
        pazaramaSelectedNodes = list;
    }
}

private void ClearPazaramaSelection()
{
    pazaramaSelectedNodes = null;
}
```

- [ ] **Step 4: Add Pazarama MudTabPanel to CategoryImport.razor**

Read the existing `CategoryImport.razor` to find the N11 or Hepsiburada tab panel, then add a new `MudTabPanel` for Pazarama following the same pattern. The tab should include:
- Loading button
- `PazaramaCategoryTreeView` component
- Selected categories sidebar
- Import button

- [ ] **Step 5: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor Application/Entegrasyon.Blazor/Features/CategoryImport/PazaramaCategoryTreeView.razor.cs Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs
git commit -m "feat(pazarama): add Pazarama tab to CategoryImport page with tree view"
```

---

## Task 8: PazaramaBrandService

**Files:**
- Create: `Application/Entegrasyon.Business/Abstract/IPazaramaBrandService.cs`
- Create: `Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaBrandService.cs`
- Create: `Test/Entegrasyon.Test/Pazarama/PazaramaBrandServiceTests.cs`
- Modify: `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Test/Entegrasyon.Test/Pazarama/PazaramaBrandServiceTests.cs
using System.Net;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaBrandServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaBrandService>> _loggerMock = new();

    private PazaramaBrandService CreateSut() => new(
        _apiClientMock.Object,
        mockContextFactory.Object,
        _loggerMock.Object);

    [Fact]
    public async Task GetBrandsAsync_ShouldReturnBrands()
    {
        // Arrange
        var brands = new List<PazaramaBrandDto>
        {
            new(Guid.NewGuid(), "TestMarka", null, null, true, null)
        };
        var response = new PazaramaResponse<List<PazaramaBrandDto>>(brands, true, null, null, null, false);
        var json = JsonSerializer.Serialize(response);

        _apiClientMock
            .Setup(c => c.GetAsync(It.Is<string>(u => u.Contains("brand/getBrands"))))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetBrandsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Name.Should().Be("TestMarka");
    }

    [Fact]
    public async Task GetBrandsAsync_WhenApiFails_ShouldReturnError()
    {
        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();
        var result = await sut.GetBrandsAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task GetBrandsAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        var response = new PazaramaResponse<List<PazaramaBrandDto>>(new List<PazaramaBrandDto>(), true, null, null, null, false);
        var json = JsonSerializer.Serialize(response);

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var sut = CreateSut();
        var result = await sut.GetBrandsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaBrandServiceTests" -v n`
Expected: FAIL

- [ ] **Step 3: Create IPazaramaBrandService interface**

```csharp
// Application/Entegrasyon.Business/Abstract/IPazaramaBrandService.cs
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaBrandService
{
    Task<IDataResult<IEnumerable<PazaramaBrandDto>>> GetBrandsAsync(string? nameFilter = null, CancellationToken cancellationToken = default);
    Task<IResult> ImportBrandsAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Create PazaramaBrandService implementation**

```csharp
// Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaBrandService.cs
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

public sealed class PazaramaBrandService(
    IPazaramaApiClient apiClient,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<PazaramaBrandService> logger) : IPazaramaBrandService
{
    public async Task<IDataResult<IEnumerable<PazaramaBrandDto>>> GetBrandsAsync(
        string? nameFilter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "brand/getBrands?Page=1&Size=100000";
            if (!string.IsNullOrWhiteSpace(nameFilter))
                url += $"&name={Uri.EscapeDataString(nameFilter)}";

            var response = await apiClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<PazaramaResponse<List<PazaramaBrandDto>>>(
                cancellationToken: cancellationToken);

            if (apiResponse?.Data == null || !apiResponse.Success)
            {
                return new ErrorDataResult<IEnumerable<PazaramaBrandDto>>(null, apiResponse?.Message ?? "Marka listesi alinamadi.");
            }

            return new SuccessDataResult<IEnumerable<PazaramaBrandDto>>(apiResponse.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama markalari cekilirken hata olustu");
            return new ErrorDataResult<IEnumerable<PazaramaBrandDto>>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> ImportBrandsAsync(CancellationToken cancellationToken = default)
    {
        var brandsResult = await GetBrandsAsync(cancellationToken: cancellationToken);
        if (!brandsResult.Success || brandsResult.Data == null)
            return new ErrorResult(brandsResult.Message ?? "Markalar alinamadi.");

        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MarketPlaceConstants.PazaramaMarketPlaceId, cancellationToken);

        if (marketplace == null)
            return new ErrorResult("Pazarama marketplace kaydi bulunamadı.");

        int created = 0, matched = 0;

        foreach (var brandDto in brandsResult.Data)
        {
            var guidString = brandDto.Id.ToString();

            // Check if already matched
            var existingMatch = await dbContext.BrandMarketPlaceMatches
                .AnyAsync(m => m.MarketPlaceBrandExternalId == guidString
                            && m.MarketPlaceId == marketplace.Id, cancellationToken);
            if (existingMatch) continue;

            // Find existing brand by name (dedup)
            var existingBrand = await dbContext.Brands
                .FirstOrDefaultAsync(b => b.Name == brandDto.Name, cancellationToken);

            if (existingBrand == null)
            {
                existingBrand = new Brand { Name = brandDto.Name, CreatedAt = DateTimeOffset.UtcNow };
                await dbContext.Brands.AddAsync(existingBrand, cancellationToken);
                created++;
            }

            var match = new BrandMarketPlaceMatch
            {
                ApplicationBrand = existingBrand,
                MarketPlaceId = marketplace.Id,
                MarketPlaceBrandId = 0,
                MarketPlaceBrandExternalId = guidString
            };
            await dbContext.BrandMarketPlaceMatches.AddAsync(match, cancellationToken);
            matched++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Pazarama marka import: {Created} yeni, {Matched} eslestirme", created, matched);
        return new SuccessResult($"{created} yeni marka olusturuldu, {matched} eslestirme yapildi.");
    }
}
```

- [ ] **Step 5: Add DI registration**

In `ApplicationDependencyExtension.cs`, in the Pazarama block add:
```csharp
services.AddScoped<IPazaramaBrandService, PazaramaBrandService>();
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~PazaramaBrandServiceTests" -v n`
Expected: All tests PASS

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IPazaramaBrandService.cs Application/Entegrasyon.Business/Concrete/Pazarama/PazaramaBrandService.cs Test/Entegrasyon.Test/Pazarama/PazaramaBrandServiceTests.cs Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs
git commit -m "feat(pazarama): add PazaramaBrandService for brand list fetching"
```

---

## Task 9: Final Verification

- [ ] **Step 1: Run full build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v n`
Expected: All tests pass (existing + new Pazarama tests)

- [ ] **Step 3: Run Pazarama tests specifically**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Pazarama" -v n`
Expected: ~15-20 tests pass

- [ ] **Step 4: Apply migration (if DB is available)**

Run: `dotnet ef database update -p Application/Entegrasyon.DataAccess --startup-project Application/Entegrasyon.Blazor`
Expected: Migration applied successfully

- [ ] **Step 5: Verify in DB**

Check that MarketPlace table has Id=4, Name="Pazarama", TokenUrl set. Check BrandMarketPlaceMatches has `MarketPlaceBrandExternalId` column.
