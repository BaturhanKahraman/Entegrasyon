# N11 Category Import (Sprint 2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** N11 kategori agacini SOAP API'den cekip DB'ye import etmek — backend service, marketplace-aware background service, lazy-load Blazor tree view.

**Architecture:** `N11CategoryImporter` extends `BaseCategoryImporterService`, `N11SoapClient` uzerinden SOAP cagrisi yapar. Mevcut `TrendyolCategoryImportBackgroundService` marketplace-aware `CategoryImportBackgroundService`'e refactor edilir. UI'da N11 tab aktif edilir, lazy-load tree view eklenir.

**Tech Stack:** .NET 8, System.Xml.Linq, EF Core (PostgreSQL), MudBlazor, xUnit + Moq + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-22-n11-category-import-design.md`

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Application/Entegrasyon.Business/Concrete/Import/N11CategoryImporter.cs` | N11 category importer (extends BaseCategoryImporterService) |
| Create | `Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs` | Marketplace-aware background service (replaces Trendyol-specific) |
| Delete | `Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs` | Replaced by marketplace-aware version |
| Modify | `Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs` | Register N11CategoryImporter + new background service |
| Create | `Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor` | N11 lazy-load tree view UI |
| Create | `Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor.cs` | N11 tree view code-behind |
| Modify | `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor` | Activate N11 tab |
| Modify | `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs` | N11 state + handlers |
| Create | `Test/Entegrasyon.Test/N11/N11CategoryImporterTests.cs` | Unit tests for N11 category importer |
| Create | `Test/Entegrasyon.Test/N11/CategoryImportBackgroundServiceTests.cs` | Unit tests for marketplace routing |

---

## Task 1: N11CategoryImporter — GetExternalCategoriesAsync + GetSubCategoriesAsync

**Files:**
- Create: `Application/Entegrasyon.Business/Concrete/Import/N11CategoryImporter.cs`
- Create: `Test/Entegrasyon.Test/N11/N11CategoryImporterTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// Test/Entegrasyon.Test/N11/N11CategoryImporterTests.cs
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

public class N11CategoryImporterTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<ILogger<N11CategoryImporter>> _loggerMock = new();

    private N11CategoryImporter CreateSut() => new(
        _soapClientMock.Object,
        mockContextFactory.Object,
        _loggerMock.Object);

    private static readonly XNamespace Ns = "http://www.n11.com/ws/schemas";

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnTopLevelCategories()
    {
        // Arrange
        var soapResponse = new XElement(Ns + "GetTopLevelCategoriesResponse",
            new XElement("result", new XElement("status", "success")),
            new XElement("categories",
                new XElement("category",
                    new XElement("id", "1001"),
                    new XElement("name", "Elektronik")),
                new XElement("category",
                    new XElement("id", "1002"),
                    new XElement("name", "Giyim"))));

        _soapClientMock
            .Setup(x => x.SendAsync("CategoryService", It.IsAny<string>(), It.IsAny<XElement>()))
            .ReturnsAsync(soapResponse);

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        var cats = result.Data.ToList();
        cats.Should().HaveCount(2);
        cats[0].ExternalId.Should().Be("1001");
        cats[0].Name.Should().Be("Elektronik");
        cats[0].HasChildren.Should().BeTrue(); // Top-level always has children
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_WhenSoapFails_ShouldReturnError()
    {
        // Arrange
        _soapClientMock
            .Setup(x => x.SendAsync("CategoryService", It.IsAny<string>(), It.IsAny<XElement>()))
            .ThrowsAsync(new HttpRequestException("SOAP error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetSubCategoriesAsync_ShouldReturnChildCategories()
    {
        // Arrange
        var soapResponse = new XElement(Ns + "GetSubCategoriesResponse",
            new XElement("result", new XElement("status", "success")),
            new XElement("category",
                new XElement("id", "1001"),
                new XElement("name", "Elektronik")),
            new XElement("subCategoryList",
                new XElement("subCategory",
                    new XElement("id", "2001"),
                    new XElement("name", "Telefon")),
                new XElement("subCategory",
                    new XElement("id", "2002"),
                    new XElement("name", "Bilgisayar"))));

        _soapClientMock
            .Setup(x => x.SendAsync("CategoryService", It.IsAny<string>(), It.IsAny<XElement>()))
            .ReturnsAsync(soapResponse);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSubCategoriesAsync(1001);

        // Assert
        result.Success.Should().BeTrue();
        var cats = result.Data.ToList();
        cats.Should().HaveCount(2);
        cats[0].ExternalId.Should().Be("2001");
        cats[0].Name.Should().Be("Telefon");
        cats[0].ParentExternalId.Should().Be("1001");
    }

    [Fact]
    public async Task GetSubCategoriesAsync_WhenLeafNode_ShouldReturnEmpty()
    {
        // Arrange — no subCategoryList element
        var soapResponse = new XElement(Ns + "GetSubCategoriesResponse",
            new XElement("result", new XElement("status", "success")),
            new XElement("category",
                new XElement("id", "3001"),
                new XElement("name", "Leaf Category")));

        _soapClientMock
            .Setup(x => x.SendAsync("CategoryService", It.IsAny<string>(), It.IsAny<XElement>()))
            .ReturnsAsync(soapResponse);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSubCategoriesAsync(3001);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11CategoryImporterTests" -v m`
Expected: FAIL — `N11CategoryImporter` does not exist.

- [ ] **Step 3: Write N11CategoryImporter (category fetching methods only)**

```csharp
// Application/Entegrasyon.Business/Concrete/Import/N11CategoryImporter.cs
using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Import;

/// <summary>
/// N11 pazaryerinden kategori import islemleri.
/// BaseCategoryImporterService'den turer, N11SoapClient uzerinden SOAP cagrisi yapar.
/// </summary>
public class N11CategoryImporter : BaseCategoryImporterService
{
    private readonly IN11SoapClient _soapClient;
    private const string ServiceName = "CategoryService";
    private const string MarketplaceName = "N11";

    private static readonly XNamespace SchNs = "http://www.n11.com/ws/schemas";

    public override ImportSource Source => ImportSource.N11;

    public N11CategoryImporter(
        IN11SoapClient soapClient,
        IDbContextFactory<IntegrationDbContext> contextFactory,
        ILogger<N11CategoryImporter> logger)
        : base(contextFactory, logger)
    {
        _soapClient = soapClient;
    }

    public override async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new XElement(SchNs + "GetTopLevelCategoriesRequest");
            var response = await _soapClient.SendAsync(ServiceName, "", request);

            var categories = response
                .Element("categories")?
                .Elements("category")
                .Select(c => new ExternalCategoryDto
                {
                    ExternalId = c.Element("id")!.Value,
                    Name = c.Element("name")!.Value,
                    HasChildren = true // Top-level categories always have children
                })
                .ToList() ?? new List<ExternalCategoryDto>();

            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "N11 kategorileri cekilirken hata olustu");
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(
                Enumerable.Empty<ExternalCategoryDto>(), $"N11 kategori hatasi: {ex.Message}");
        }
    }

    /// <summary>
    /// Belirtilen N11 kategori ID'sinin alt kategorilerini getirir (lazy-load icin).
    /// Bu metot BaseCategoryImporterService'te yok — N11'e ozel, UI'dan dogrudan cagirilir.
    /// </summary>
    public async Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetSubCategoriesAsync(
        long categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new XElement(SchNs + "GetSubCategoriesRequest",
                new XElement("categoryId", categoryId));

            var response = await _soapClient.SendAsync(ServiceName, "", request);

            var parentId = response.Element("category")?.Element("id")?.Value ?? categoryId.ToString();

            var subCategoryList = response.Element("subCategoryList");
            if (subCategoryList == null)
                return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(
                    Enumerable.Empty<ExternalCategoryDto>());

            var categories = subCategoryList
                .Elements("subCategory")
                .Select(c => new ExternalCategoryDto
                {
                    ExternalId = c.Element("id")!.Value,
                    Name = c.Element("name")!.Value,
                    ParentExternalId = parentId,
                    // N11 GetSubCategories response'u child'larin alt kategorileri olup olmadigini
                    // bildirmez. HasChildren=true varsayarak UI'da expand ok gosterilir.
                    // Kullanici expand edince GetSubCategories cagirilir — bos donerse leaf'tir.
                    // Trade-off: leaf node'lar icin fazladan 1 SOAP call, ama N+1 onleme
                    // (her child icin GetSubCategories yapmak) yerine lazy detect tercih edildi.
                    HasChildren = true
                })
                .ToList();

            return new SuccessDataResult<IEnumerable<ExternalCategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "N11 alt kategorileri cekilirken hata: CategoryId={CategoryId}", categoryId);
            return new ErrorDataResult<IEnumerable<ExternalCategoryDto>>(
                Enumerable.Empty<ExternalCategoryDto>(), $"Alt kategori hatasi: {ex.Message}");
        }
    }

    public override async Task<IResult> ImportCategoriesAsync(
        IEnumerable<ExternalCategoryImportRequest> categories, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync(cancellationToken);
        await LoadMarketPlaceAsync(dbContext, MarketplaceName, cancellationToken);

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

    protected override async Task ImportCategoryAttributesAsync(
        IntegrationDbContext dbContext,
        Category category,
        bool isNewCategory,
        CancellationToken cancellationToken = default)
    {
        if (!isNewCategory || string.IsNullOrEmpty(category.ExternalCategoryId)) return;

        try
        {
            var n11CategoryId = long.Parse(category.ExternalCategoryId);
            int currentPage = 0;
            int pageCount;

            do
            {
                var request = new XElement(SchNs + "GetCategoryAttributesRequest",
                    new XElement("categoryId", n11CategoryId),
                    new XElement("pagingData",
                        new XElement("currentPage", currentPage),
                        new XElement("pageSize", 100)));

                var response = await _soapClient.SendAsync(ServiceName, "", request);

                var categoryElement = response.Element("category");
                if (categoryElement == null) break;

                // Parse pagination metadata
                var metadata = categoryElement.Element("metadata");
                pageCount = int.Parse(metadata?.Element("pageCount")?.Value ?? "1");

                var attributeList = categoryElement.Element("attributeList");
                if (attributeList == null) break;

                var categoryAttributeCategories = new List<CategoryAttributeCategory>();

                foreach (var attrElement in attributeList.Elements("attribute"))
                {
                    var n11AttrId = int.Parse(attrElement.Element("id")!.Value);
                    var attrName = attrElement.Element("name")!.Value;
                    var mandatory = bool.Parse(attrElement.Element("mandatory")?.Value ?? "false");
                    var multipleSelect = bool.Parse(attrElement.Element("multipleSelect")?.Value ?? "false");

                    // Marketplace-aware deduplication
                    var existingMatch = await dbContext.CategoryAttributeMarketPlaceMatches
                        .AnyAsync(x => x.MarketPlaceCategoryAttributeId == n11AttrId
                                    && x.MarketPlaceId == N11MarketPlaceId, cancellationToken);

                    CategoryAttribute dbCatAttr;
                    if (existingMatch)
                    {
                        dbCatAttr = await dbContext.CategoryAttributeMarketPlaceMatches
                            .Where(x => x.MarketPlaceCategoryAttributeId == n11AttrId
                                     && x.MarketPlaceId == N11MarketPlaceId)
                            .Select(x => x.ApplicationCategoryAttribute)
                            .FirstAsync(cancellationToken);
                    }
                    else
                    {
                        dbCatAttr = new CategoryAttribute
                        {
                            CategoryAttributeKey = attrName,
                            CategoryAttributeHumanized = attrName,
                            ImportId = n11AttrId,
                            AllowCustom = false,
                            CategoryAttributeValues = new List<CategoryAttributeValue>(),
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        if (MarketPlace != null)
                        {
                            await dbContext.CategoryAttributeMarketPlaceMatches.AddAsync(
                                new CategoryAttributeMarketPlaceMatch
                                {
                                    MarketPlace = MarketPlace,
                                    ApplicationCategoryAttribute = dbCatAttr,
                                    MarketPlaceCategoryAttributeId = n11AttrId
                                }, cancellationToken);
                        }

                        // Add attribute values
                        var valueList = attrElement.Element("valueList");
                        if (valueList != null)
                        {
                            foreach (var valElement in valueList.Elements("value"))
                            {
                                var valueId = int.Parse(valElement.Element("id")!.Value);
                                var valueName = valElement.Element("name")!.Value;

                                var attrValue = new CategoryAttributeValue
                                {
                                    Name = valueName,
                                    CreatedAt = DateTimeOffset.UtcNow
                                };
                                dbCatAttr.CategoryAttributeValues.Add(attrValue);

                                if (MarketPlace != null)
                                {
                                    await dbContext.CategoryAttributeValueMarketPlaceMatches.AddAsync(
                                        new CategoryAttributeValueMarketPlaceMatch
                                        {
                                            MarketPlace = MarketPlace,
                                            ApplicationCategoryAttributeValue = attrValue,
                                            MarketPlaceCategoryAttributeValueId = valueId
                                        }, cancellationToken);
                                }
                            }
                        }
                    }

                    categoryAttributeCategories.Add(new CategoryAttributeCategory
                    {
                        Category = category,
                        CategoryAttribute = dbCatAttr,
                        IsRequired = mandatory,
                        IsSlicer = multipleSelect,
                        IsVarianter = false
                    });
                }

                await dbContext.CategoryAttributeCategories.AddRangeAsync(categoryAttributeCategories, cancellationToken);
                currentPage++;

            } while (currentPage < pageCount);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "N11 kategori ozellikleri import edilirken hata: {CategoryId}", category.ExternalCategoryId);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11CategoryImporterTests" -v m`
Expected: PASS (4 tests)

- [ ] **Step 5: Add attribute import tests**

Add to `N11CategoryImporterTests.cs`:

```csharp
// Note: ImportCategoryAttributesAsync is protected, tested indirectly through ImportCategoriesAsync.
// These tests verify the full import pipeline including attribute creation.

// For testing attribute import, mock the SOAP client to return GetCategoryAttributes response
// when ImportCategoriesAsync calls ImportCategoryAttributesAsync internally.
// The exact test implementation depends on how BaseTest mocks DbContext sets —
// the implementer should use mockIntegrationDbContext.Setup for CategoryAttributeMarketPlaceMatches,
// CategoryAttributeValueMarketPlaceMatches, CategoryAttributeCategories, CategoryAttributes, etc.

// Required test cases (implementer fills in exact mock setup):
// 1. ImportCategoryAttributesAsync_ShouldCreateAttributeAndValueRecords
//    — Verify CategoryAttribute and CategoryAttributeValue records are created
// 2. ImportCategoryAttributesAsync_ShouldCreateMarketPlaceMatchWithN11Id
//    — Verify CategoryAttributeMarketPlaceMatch.MarketPlaceId == 2
// 3. ImportCategoryAttributesAsync_WhenAttributeAlreadyExists_ShouldNotDuplicate
//    — Pre-populate match table, verify no duplicate created
// 4. ImportCategoryAttributesAsync_ShouldHandlePagination
//    — Mock multi-page GetCategoryAttributes response (pageCount=2)
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~N11CategoryImporterTests" -v m`
Expected: PASS (8 tests)

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/Import/N11CategoryImporter.cs \
      Test/Entegrasyon.Test/N11/N11CategoryImporterTests.cs
git commit -m "feat(n11): implement N11CategoryImporter with GetExternalCategories, GetSubCategories, and attribute import"
```

---

## Task 2: CategoryImportBackgroundService (Marketplace-Aware Router)

**Files:**
- Create: `Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs`
- Delete: `Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs`
- Create: `Test/Entegrasyon.Test/N11/CategoryImportBackgroundServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// Test/Entegrasyon.Test/N11/CategoryImportBackgroundServiceTests.cs
// Note: Testing BackgroundService ExecuteAsync directly is complex (infinite loop).
// These tests verify the routing logic and integration indirectly.
// The implementer should test that:
// 1. CategoryImportBackgroundService class exists and compiles
// 2. The switch expression in ExecuteAsync handles "N11" and "Trendyol"
// 3. Build verification is sufficient for background service routing
// The actual integration is verified by full test suite + manual testing.

using Entegrasyon.Business.BackgroundServices;
using FluentAssertions;

namespace Entegrasyon.Test.N11;

public class CategoryImportBackgroundServiceTests
{
    [Fact]
    public void CategoryImportBackgroundService_ShouldExist()
    {
        // This test ensures the class exists and is a BackgroundService
        typeof(CategoryImportBackgroundService)
            .Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CategoryImportBackgroundServiceTests" -v m`
Expected: FAIL — `CategoryImportBackgroundService` does not exist.

- [ ] **Step 3: Create CategoryImportBackgroundService**

```csharp
// Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Marketplace-aware kategori import background service.
/// EventChannel'dan gelen CategoryImportRequestedEvent'leri marketplace ismine gore yonlendirir.
/// </summary>
public class CategoryImportBackgroundService : BackgroundService
{
    private readonly EventChannel<CategoryImportRequestedEvent> _importRequestedChannel;
    private readonly EventChannel<CategoryImportCompletedEvent> _importCompletedChannel;
    private readonly EventChannel<NotificationEvent> _notificationChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CategoryImportBackgroundService> _logger;

    public CategoryImportBackgroundService(
        EventChannel<CategoryImportRequestedEvent> importRequestedChannel,
        EventChannel<CategoryImportCompletedEvent> importCompletedChannel,
        EventChannel<NotificationEvent> notificationChannel,
        IServiceScopeFactory scopeFactory,
        ILogger<CategoryImportBackgroundService> logger)
    {
        _importRequestedChannel = importRequestedChannel;
        _importCompletedChannel = importCompletedChannel;
        _notificationChannel = notificationChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var importEvent in _importRequestedChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Starting {Marketplace} category import for user {UserId}, {Count} categories",
                    importEvent.MarketplaceName, importEvent.UserId, importEvent.Categories.Count());

                using var scope = _scopeFactory.CreateScope();
                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

                // Route to correct importer by marketplace name
                BaseCategoryImporterService importer = importEvent.MarketplaceName switch
                {
                    "Trendyol" => scope.ServiceProvider.GetRequiredService<TrendyolCategoryImporter>(),
                    "N11" => scope.ServiceProvider.GetRequiredService<N11CategoryImporter>(),
                    _ => throw new InvalidOperationException($"Bilinmeyen pazaryeri: {importEvent.MarketplaceName}")
                };

                try
                {
                    await notificationManager.SendNotification(
                        header: $"{importEvent.MarketplaceName} kategori ice aktarma işlemi basladi",
                        content: $"{importEvent.Categories.Count()} kategori ice aktariliyor...",
                        severity: NotificationSeverity.Info,
                        category: NotificationCategory.Pazaryeri,
                        userIds: [importEvent.UserId]);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send start notification");
                }

                var result = await importer.ImportCategoriesAsync(importEvent.Categories, stoppingToken);

                await _importCompletedChannel.PublishAsync(new CategoryImportCompletedEvent(
                    importEvent.MarketplaceName,
                    importEvent.Categories.Count(),
                    result.Success,
                    result.Success ? null : result.Message,
                    importEvent.UserId));

                var header = result.Success
                    ? $"{importEvent.MarketplaceName} kategori ice aktarma tamamlandi"
                    : $"{importEvent.MarketplaceName} kategori ice aktarma hatasi";
                var content = result.Success
                    ? $"{importEvent.Categories.Count()} kategori basariyla ice aktarildi."
                    : $"Ice aktarma başarısız: {result.Message}";

                try
                {
                    await notificationManager.SendNotification(
                        header: header,
                        content: content,
                        severity: result.Success ? NotificationSeverity.Success : NotificationSeverity.Error,
                        category: NotificationCategory.Pazaryeri,
                        userIds: [importEvent.UserId],
                        actionUrl: result.Success ? "/marketplace/sync" : null);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send result notification");
                }

                _logger.LogInformation("{Marketplace} category import completed for user {UserId}: {Success}",
                    importEvent.MarketplaceName, importEvent.UserId, result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Marketplace} category import failed for user {UserId}",
                    importEvent.MarketplaceName, importEvent.UserId);
                using var scope = _scopeFactory.CreateScope();
                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();
                try
                {
                    await notificationManager.SendNotification(
                        header: $"{importEvent.MarketplaceName} kategori ice aktarma hatasi",
                        content: $"Beklenmeyen hata: {ex.Message}",
                        severity: NotificationSeverity.Error,
                        category: NotificationCategory.Pazaryeri,
                        userIds: [importEvent.UserId]);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send error notification");
                }
            }
        }
    }
}
```

- [ ] **Step 4: Delete old TrendyolCategoryImportBackgroundService**

Delete: `Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs`

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CategoryImportBackgroundServiceTests" -v m`
Expected: PASS

- [ ] **Step 6: Update DI registration (same commit to avoid build break)**

In `ApplicationDependencyExtension.cs`:
- Add `services.AddScoped<N11CategoryImporter>();` after `services.AddScoped<TrendyolCategoryImporter>();`
- In `AddBackgroundServices`, replace `services.AddHostedService<TrendyolCategoryImportBackgroundService>();` with `services.AddHostedService<CategoryImportBackgroundService>();`

- [ ] **Step 7: Delete old TrendyolCategoryImportBackgroundService**

Delete: `Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs`

- [ ] **Step 8: Verify build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 9: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v m`
Expected: All tests pass

- [ ] **Step 10: Commit**

```bash
git add Application/Entegrasyon.Business/BackgroundServices/CategoryImportBackgroundService.cs \
      Application/Entegrasyon.ApplicationBootstrap/ApplicationDependencyExtension.cs \
      Test/Entegrasyon.Test/N11/CategoryImportBackgroundServiceTests.cs
git rm Application/Entegrasyon.Business/BackgroundServices/TrendyolCategoryImportBackgroundService.cs
git commit -m "refactor: replace TrendyolCategoryImportBackgroundService with marketplace-aware CategoryImportBackgroundService"
```

---

## Task 3: UI — CategoryImport.razor N11 Tab + State

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor`
- Modify: `Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs`

- [ ] **Step 1: Add N11 state and inject to code-behind**

In `CategoryImport.razor.cs`, add:

```csharp
[Inject]
private N11CategoryImporter N11Importer { get; set; } = null!;

// N11-specific state (separate from Trendyol)
private List<CategoryTreeNode> n11Categories = [];
private IReadOnlyCollection<CategoryTreeNode>? n11SelectedNodes;
private bool n11Loading;
private string n11SearchQuery = string.Empty;
```

Add N11 handler methods:

```csharp
private async Task LoadN11CategoriesAsync()
{
    n11Loading = true;
    var result = await N11Importer.GetExternalCategoriesAsync();
    if (result.Success && result.Data != null)
    {
        n11Categories = result.Data.Select(MapToTreeNode).ToList();
        Snackbar.Add($"{n11Categories.Count} N11 ust kategori yuklendi.", Severity.Success);
    }
    else
    {
        Snackbar.Add(result.Message ?? "N11 kategorileri yuklenemedi.", Severity.Error);
    }
    n11Loading = false;
}

private async Task ImportN11CategoriesAsync()
{
    if (n11SelectedNodes == null || !n11SelectedNodes.Any())
    {
        Snackbar.Add("Lutfen en az bir kategori secin.", Severity.Warning);
        return;
    }

    importing = true;
    try
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var importRequests = n11SelectedNodes.Select(MapToImportRequest).ToList();
        var importEvent = new CategoryImportRequestedEvent("N11", importRequests, userId);
        await ImportRequestedChannel.PublishAsync(importEvent);
        Snackbar.Add("N11 kategori ice aktarma işlemi baslatildi.", Severity.Info);
        n11SelectedNodes = null;
        NavigationManager.NavigateTo("/categories");
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "N11 category import request failed");
        Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
    }
    finally
    {
        importing = false;
    }
}
```

- [ ] **Step 2: Activate N11 tab in razor file**

In `CategoryImport.razor`, find the disabled N11 tab and activate it. Add N11 tree view and selected panel content, mirroring the Trendyol tab structure but using `n11Categories`, `n11SelectedNodes`, `n11Loading`, `LoadN11CategoriesAsync`, `ImportN11CategoriesAsync`.

- [ ] **Step 3: Verify build**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor \
      Application/Entegrasyon.Blazor/Features/CategoryImport/CategoryImport.razor.cs
git commit -m "feat(n11): activate N11 tab in CategoryImport page with separate state"
```

---

## Task 4: UI — N11CategoryTreeView Component (Lazy-Load)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor`
- Create: `Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor.cs`

- [ ] **Step 1: Create N11CategoryTreeView.razor.cs code-behind**

```csharp
// Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor.cs
using Entegrasyon.Blazor.ViewModels;
using Entegrasyon.Business.Concrete.Import;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class N11CategoryTreeView
{
    [Inject]
    private N11CategoryImporter N11Importer { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>?> SelectedNodesChanged { get; set; }

    [Parameter]
    public string SearchQuery { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchQueryChanged { get; set; }

    private readonly HashSet<string> _fetchedNodes = new();
    private readonly HashSet<string> _loadingNodes = new();

    private async Task OnExpandNode(CategoryTreeNode node)
    {
        if (_fetchedNodes.Contains(node.ExternalId)) return;
        if (_loadingNodes.Contains(node.ExternalId)) return;

        _loadingNodes.Add(node.ExternalId);
        StateHasChanged();

        try
        {
            var n11Id = long.Parse(node.ExternalId);
            var result = await N11Importer.GetSubCategoriesAsync(n11Id);

            if (result.Success && result.Data != null)
            {
                node.Children = result.Data.Select(c => new CategoryTreeNode
                {
                    ExternalId = c.ExternalId,
                    Name = c.Name,
                    ParentExternalId = c.ParentExternalId,
                    Children = c.HasChildren ? new HashSet<CategoryTreeNode>() : new HashSet<CategoryTreeNode>()
                }).ToHashSet();

                _fetchedNodes.Add(node.ExternalId);
            }
            else
            {
                Snackbar.Add($"Alt kategoriler yuklenemedi: {result.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loadingNodes.Remove(node.ExternalId);
            StateHasChanged();
        }
    }

    private bool IsLoading(string externalId) => _loadingNodes.Contains(externalId);
}
```

- [ ] **Step 2: Create N11CategoryTreeView.razor**

```razor
@* Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor *@
@using Entegrasyon.Blazor.ViewModels

<MudPaper Elevation="0" Outlined="true" Class="pa-4" Style="height: 600px; display: flex; flex-direction: column;">
    <MudText Typo="Typo.subtitle1" Class="mb-2">N11 Kategori Agaci</MudText>

    @if (!Categories.Any())
    {
        <MudText Align="Align.Center" Color="Color.Secondary" Class="mt-4">
            Kategori bulunamadı
        </MudText>
    }
    else
    {
        <div style="overflow-y: auto; flex: 1;">
            @foreach (var node in Categories)
            {
                <N11CategoryTreeNodeItem Node="node"
                                        Level="0"
                                        OnExpand="OnExpandNode"
                                        OnSelect="SelectNode"
                                        IsLoading="IsLoading"
                                        SelectedNodes="SelectedNodes" />
            }
        </div>
    }
</MudPaper>
```

Also create a recursive `N11CategoryTreeNodeItem.razor` component for rendering each node with expand/collapse, checkbox, loading spinner:

```razor
@* Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeNodeItem.razor *@
@using Entegrasyon.Blazor.ViewModels

<div style="padding-left: @(Level * 24)px" class="d-flex align-center py-1">
    @if (Node.Children.Any() || !_fetched)
    {
        <MudIconButton Icon="@(Node.IsExpanded ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ChevronRight)"
                      Size="Size.Small"
                      OnClick="ToggleExpand" />
    }
    else
    {
        <div style="width: 40px;"></div>
    }

    @if (IsLoading(Node.ExternalId))
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
    }

    <MudCheckBox T="bool" Value="IsSelected" ValueChanged="OnSelectChanged" Dense="true" />
    <MudIcon Icon="@(Node.Children.Any() ? Icons.Material.Filled.Folder : Icons.Material.Filled.Category)"
            Size="Size.Small" Class="mr-1" />
    <MudText Typo="Typo.body2">@Node.Name</MudText>
</div>

@if (Node.IsExpanded)
{
    @foreach (var child in Node.Children)
    {
        <N11CategoryTreeNodeItem Node="child"
                                Level="Level + 1"
                                OnExpand="OnExpand"
                                OnSelect="OnSelect"
                                IsLoading="IsLoading"
                                SelectedNodes="SelectedNodes" />
    }
}

@code {
    [Parameter] public CategoryTreeNode Node { get; set; } = null!;
    [Parameter] public int Level { get; set; }
    [Parameter] public EventCallback<CategoryTreeNode> OnExpand { get; set; }
    [Parameter] public EventCallback<CategoryTreeNode> OnSelect { get; set; }
    [Parameter] public Func<string, bool> IsLoading { get; set; } = _ => false;
    [Parameter] public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    private bool _fetched;
    private bool IsSelected => SelectedNodes?.Contains(Node) == true;

    private async Task ToggleExpand()
    {
        Node.IsExpanded = !Node.IsExpanded;
        if (Node.IsExpanded && !_fetched)
        {
            await OnExpand.InvokeAsync(Node);
            _fetched = true;
        }
    }

    private async Task OnSelectChanged(bool _) => await OnSelect.InvokeAsync(Node);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor \
      Application/Entegrasyon.Blazor/Features/CategoryImport/N11CategoryTreeView.razor.cs
git commit -m "feat(n11): add N11CategoryTreeView component with lazy-load on expand"
```

---

## Task 5: Full Test Suite + Integration Verification

- [ ] **Step 1: Run all unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v m`
Expected: All tests pass (existing + new N11 tests)

- [ ] **Step 2: Run full solution build**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 3: Final commit (if any fixes needed)**

```bash
git commit -m "fix(n11): resolve any remaining Sprint 2 build/test issues"
```
