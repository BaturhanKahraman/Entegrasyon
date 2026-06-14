using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Features.MarketplaceSync;

/// <summary>
/// Integration tests for the Trendyol Product Send workflow.
/// Tests the complete flow: preflight → overrides → sync queueing.
/// </summary>
[Trait("Category", "Integration")]
public class ProductSendIntegrationTests : IntegrationTestBase
{
    private const int TrendyolMarketPlaceId = 1;

    public ProductSendIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync(brandName: "Send Test Marka", categoryName: "Send Test Kategori");
        await SeedMarketPlaceAsync(TrendyolMarketPlaceId, "Trendyol");
    }

    /// <summary>
    /// Full send workflow: preflight passes, overrides saved, product queued for sync.
    /// </summary>
    [Fact]
    public async Task SendProductWorkflow_AllStepsPassed_ShouldQueueProductForSync()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, _) = await SeedProductWithStockAsync("SEND-001", stock: 10);

        // Setup matches
        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);
        var requiredAttrId = await SeedRequiredCategoryAttributeAsync(categoryId, "Renk");
        await SeedCategoryAttributeMarketPlaceMatchAsync(requiredAttrId, TrendyolMarketPlaceId, 5001);
        var variantId2 = await AddProductVariantAsync(productId, "SEND-001-V2", 50);

        var (syncManager, scope1) = GetScopedService<IProductSyncManager>();
        var (overrideManager, scope2) = GetScopedService<IMarketplaceOverrideManager>();
        using var __ = scope1;
        using var ___ = scope2;

        // Act 1: Verify preflight passes
        var preflightResult = await syncManager.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert 1
        preflightResult.Success.Should().BeTrue();
        preflightResult.Data!.AllPassed.Should().BeTrue();

        // Act 2: Save overrides
        var saveOverrideDto = new SaveMarketplaceOverridesDto
        {
            ProductId = productId,
            MarketPlaceId = TrendyolMarketPlaceId,
            TitleOverride = "Custom Title",
            DescriptionOverride = "Custom Description",
            VariantOverrides =
            [
                new()
                {
                    ProductVariantId = variantId2,
                    ListPriceOverride = 100m,
                    SalePriceOverride = 85m
                }
            ]
        };

        var overrideSaveResult = await overrideManager.SaveOverridesAsync(saveOverrideDto);

        // Assert 2
        overrideSaveResult.Success.Should().BeTrue();

        // Act 3: Queue product for sync
        var syncResult = await syncManager.SyncProductAsync(productId, TrendyolMarketPlaceId);

        // Assert 3
        syncResult.Success.Should().BeTrue();

        // Act 4: Verify product marketplace record created with Pending status
        using (var dbContext = CreateDbContext())
        {
            var productMarketplace = await dbContext.ProductMarketplaces
                .FirstOrDefaultAsync(pm =>
                    pm.ProductId == productId &&
                    pm.MarketPlaceId == TrendyolMarketPlaceId);

            productMarketplace.Should().NotBeNull();
            productMarketplace!.Status.Should().Be(MarketplaceProductStatus.Pending);
        }
    }

    /// <summary>
    /// Save overrides with only title (no description, no price overrides).
    /// Expected: Success = true, overrides saved without errors.
    /// </summary>
    [Fact]
    public async Task SaveOverrides_TitleOnlyOverride_ShouldSaveSuccessfully()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, variantId) = await SeedProductWithStockAsync("OVERRIDE-001", stock: 10);

        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        var (overrideManager, scope) = GetScopedService<IMarketplaceOverrideManager>();
        using var _ = scope;

        var saveDto = new SaveMarketplaceOverridesDto
        {
            ProductId = productId,
            MarketPlaceId = TrendyolMarketPlaceId,
            TitleOverride = "Only Title Override",
            DescriptionOverride = null,
            VariantOverrides = []
        };

        // Act
        var result = await overrideManager.SaveOverridesAsync(saveDto);

        // Assert
        result.Success.Should().BeTrue();

        // Verify override retrieved
        using (var dbContext = CreateDbContext())
        {
            var getResult = await overrideManager.GetOverridesAsync(productId, TrendyolMarketPlaceId);
            getResult.Success.Should().BeTrue();
            getResult.Data!.TitleOverride.Should().Be("Only Title Override");
            getResult.Data!.DescriptionOverride.Should().BeNull();
        }
    }

    /// <summary>
    /// Sync product without preflight passing (missing category match).
    /// Expected: Product is still queued (SyncProductAsync doesn't validate), but preflight would fail.
    /// </summary>
    [Fact]
    public async Task SyncProductWithoutPreflight_NoMatches_ShouldStillQueueForSync()
    {
        // Arrange
        var (productId, _) = await SeedProductWithStockAsync("NO-MATCH-001", stock: 10);
        // Intentionally NOT setting up matches

        var (syncManager, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act: Verify preflight would fail
        var preflightResult = await syncManager.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert preflight fails
        preflightResult.Data!.AllPassed.Should().BeFalse();

        // Act: Queue anyway (SyncProductAsync doesn't validate)
        var syncResult = await syncManager.SyncProductAsync(productId, TrendyolMarketPlaceId);

        // Assert: Sync still succeeds (validation is UI concern)
        syncResult.Success.Should().BeTrue();

        // Verify product is queued
        using (var dbContext = CreateDbContext())
        {
            var pm = await dbContext.ProductMarketplaces
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);
            pm!.Status.Should().Be(MarketplaceProductStatus.Pending);
        }
    }

    /// <summary>
    /// Regression: category mapping is stored by the wizard in the canonical
    /// CategoryMarketplaces table. Preflight must read THAT table.
    /// </summary>
    [Fact]
    public async Task GetSendPreflight_CategoryMappedInCanonicalTable_ShouldReportCategoryMatched()
    {
        // Arrange
        var (categoryId, _) = await GetCategoryAndBrandIdsAsync();
        var (productId, _) = await SeedProductWithStockAsync("CANON-CAT-001", stock: 5);
        // Map category exactly as the mapping wizard does (canonical CategoryMarketplaces table)
        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1011);

        var (syncManager, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var preflight = await syncManager.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        preflight.Data!.CategoryMatched.Should().BeTrue();
    }

    /// <summary>
    /// Regression guard: a mapping present ONLY in the deprecated
    /// CategoryMarketPlaceMatches table must NOT satisfy preflight — preflight
    /// reads the canonical CategoryMarketplaces table only.
    /// </summary>
    [Fact]
    public async Task GetSendPreflight_CategoryMappedOnlyInLegacyTable_ShouldReportCategoryNotMatched()
    {
        // Arrange
        var (categoryId, _) = await GetCategoryAndBrandIdsAsync();
        var (productId, _) = await SeedProductWithStockAsync("LEGACY-CAT-001", stock: 5);
        using (var db = CreateDbContext())
        {
            db.CategoryMarketPlaceMatches.Add(new CategoryMarketPlaceMatch
            {
                ApplicationCategoryId = categoryId,
                MarketPlaceId = TrendyolMarketPlaceId,
                MarketPlaceCategoryId = 1011
            });
            await db.SaveChangesAsync();
        }

        var (syncManager, scope) = GetScopedService<IProductSyncManager>();
        using var _ = scope;

        // Act
        var preflight = await syncManager.GetSendPreflightAsync(productId, TrendyolMarketPlaceId);

        // Assert
        preflight.Data!.CategoryMatched.Should().BeFalse();
    }

    /// <summary>
    /// Product already synced once, user syncs again after modifying overrides.
    /// Expected: Product status reset to Pending, existing override replaced.
    /// </summary>
    [Fact]
    public async Task ResyncProductWithNewOverrides_AlreadySynced_ShouldResetStatusAndUpdateOverrides()
    {
        // Arrange
        var (categoryId, brandId) = await GetCategoryAndBrandIdsAsync();
        var (productId, variantId) = await SeedProductWithStockAsync("RESYNC-001", stock: 10);

        await SeedCategoryMarketPlaceMatchAsync(categoryId, TrendyolMarketPlaceId, 1001);
        await SeedBrandMarketPlaceMatchAsync(brandId, TrendyolMarketPlaceId, 101);

        var (syncManager, scope1) = GetScopedService<IProductSyncManager>();
        var (overrideManager, scope2) = GetScopedService<IMarketplaceOverrideManager>();
        using var _ = scope1;
        using var __ = scope2;

        // Act 1: Initial sync
        var firstSync = await syncManager.SyncProductAsync(productId, TrendyolMarketPlaceId);
        firstSync.Success.Should().BeTrue();

        // Simulate published state
        using (var dbContext = CreateDbContext())
        {
            var pm = await dbContext.ProductMarketplaces
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);
            pm!.Status = MarketplaceProductStatus.Published;
            await dbContext.SaveChangesAsync();
        }

        // Act 2: Save new overrides
        var saveDto = new SaveMarketplaceOverridesDto
        {
            ProductId = productId,
            MarketPlaceId = TrendyolMarketPlaceId,
            TitleOverride = "Updated Title",
            DescriptionOverride = null,
            VariantOverrides = []
        };
        await overrideManager.SaveOverridesAsync(saveDto);

        // Act 3: Resync
        var secondSync = await syncManager.SyncProductAsync(productId, TrendyolMarketPlaceId);

        // Assert 3
        secondSync.Success.Should().BeTrue();

        // Verify status reset to Pending
        using (var dbContext = CreateDbContext())
        {
            var pm = await dbContext.ProductMarketplaces
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);
            pm!.Status.Should().Be(MarketplaceProductStatus.Pending);
        }
    }

    // ─── Helper Methods ───

    /// <summary>
    /// Get default category and brand IDs from seeded data.
    /// </summary>
    private async Task<(int CategoryId, int BrandId)> GetCategoryAndBrandIdsAsync()
    {
        using var dbContext = CreateDbContext();
        var category = await dbContext.Categories.FirstAsync(c => c.Name == "Send Test Kategori");
        var brand = await dbContext.Brands.FirstAsync(b => b.Name == "Send Test Marka");
        return (category.Id, brand.Id);
    }

    /// <summary>
    /// Seed a category mapping in the canonical CategoryMarketplaces table
    /// (the table the mapping wizard writes and preflight/send reads).
    /// </summary>
    private async Task SeedCategoryMarketPlaceMatchAsync(int categoryId, int marketPlaceId, int marketPlaceCategoryId)
    {
        using var dbContext = CreateDbContext();
        var exists = await dbContext.CategoryMarketplaces
            .AnyAsync(m => m.CategoryId == categoryId && m.MarketPlaceId == marketPlaceId);

        if (!exists)
        {
            dbContext.CategoryMarketplaces.Add(new CategoryMarketplace
            {
                CategoryId = categoryId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceCategoryId = marketPlaceCategoryId,
                IsActive = true
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Seed a BrandMarketPlaceMatch entry.
    /// </summary>
    private async Task SeedBrandMarketPlaceMatchAsync(int brandId, int marketPlaceId, int marketPlaceBrandId)
    {
        using var dbContext = CreateDbContext();
        var exists = await dbContext.BrandMarketPlaceMatches
            .AnyAsync(m => m.ApplicationBrandId == brandId && m.MarketPlaceId == marketPlaceId);

        if (!exists)
        {
            dbContext.BrandMarketPlaceMatches.Add(new BrandMarketPlaceMatch
            {
                ApplicationBrandId = brandId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceBrandId = marketPlaceBrandId
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Create a required CategoryAttribute for the given category.
    /// </summary>
    private async Task<int> SeedRequiredCategoryAttributeAsync(int categoryId, string attributeName)
    {
        using var dbContext = CreateDbContext();

        // Create the attribute
        var attr = new CategoryAttribute
        {
            CategoryAttributeKey = attributeName.ToLower(),
            CategoryAttributeHumanized = attributeName,
            ImportId = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.CategoryAttributes.Add(attr);
        await dbContext.SaveChangesAsync();

        var attrId = attr.Id;

        // Link it to the category as required
        var catAttrCat = new CategoryAttributeCategory
        {
            CategoryId = categoryId,
            CategoryAttributeId = attrId,
            IsRequired = true,
            IsSlicer = false,
            IsVarianter = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.CategoryAttributeCategories.Add(catAttrCat);
        await dbContext.SaveChangesAsync();

        return attrId;
    }

    /// <summary>
    /// Seed a CategoryAttributeMarketPlaceMatch entry.
    /// </summary>
    private async Task SeedCategoryAttributeMarketPlaceMatchAsync(int categoryAttributeId,
        int marketPlaceId, int marketPlaceCategoryAttributeId)
    {
        using var dbContext = CreateDbContext();
        var exists = await dbContext.CategoryAttributeMarketPlaceMatches
            .AnyAsync(m => m.ApplicationCategoryAttributeId == categoryAttributeId && m.MarketPlaceId == marketPlaceId);

        if (!exists)
        {
            dbContext.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
            {
                ApplicationCategoryAttributeId = categoryAttributeId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceCategoryAttributeId = marketPlaceCategoryAttributeId
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Add a variant to an existing product with a given barcode.
    /// </summary>
    private async Task<Guid> AddProductVariantAsync(Guid productId, string barcode, int stock)
    {
        using var dbContext = CreateDbContext();

        var variantId = Guid.NewGuid();
        var variant = new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Barcode = barcode,
            ListPrice = 200,
            SalePrice = 180,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ProductVariants.Add(variant);

        // Add stock
        dbContext.BranchOfficeStocks.Add(new BranchOfficeStock
        {
            BranchOfficeId = 1,
            ProductVariantId = variantId,
            FirstTotalStock = stock,
            SoldQuantity = 0
        });

        await dbContext.SaveChangesAsync();
        return variantId;
    }
}
