using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Templates;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Templates;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// MatchedEntityImportManager integration testleri.
/// Template paketlerin listelenmesi, cakisma tespiti ve import akislari.
/// </summary>
[Trait("Category", "Integration")]
public class MatchedEntityImportIntegrationTests : IntegrationTestBase
{
    public MatchedEntityImportIntegrationTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    #region Helpers

    private async Task<int> SeedCategoryPackageAsync(bool isPublished = true, string name = "Elektronik > Cep Telefonu")
    {
        using var dbContext = CreateDbContext();

        var package = new MatchedEntityPackage
        {
            Name = name,
            Description = "Test paketi",
            EntityType = MatchedEntityType.Category,
            IsPublished = isPublished,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MatchedEntityPackages.Add(package);
        await dbContext.SaveChangesAsync();

        // Root kategori
        var rootCategory = new TemplateCategoryData
        {
            PackageId = package.Id,
            Name = "Elektronik",
            SortOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.TemplateCategoryData.Add(rootCategory);
        await dbContext.SaveChangesAsync();

        // Child kategori
        var childCategory = new TemplateCategoryData
        {
            PackageId = package.Id,
            Name = "Cep Telefonu",
            ParentTemplateCategoryDataId = rootCategory.Id,
            SortOrder = 1,
            DefaultVatRate = 20,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.TemplateCategoryData.Add(childCategory);
        await dbContext.SaveChangesAsync();

        // Marketplace mapping'ler (Trendyol + Hepsiburada)
        dbContext.TemplateCategoryMarketplaceMappings.AddRange(
            new TemplateCategoryMarketplaceMapping
            {
                TemplateCategoryDataId = childCategory.Id,
                MarketPlaceId = 1, // Trendyol
                ExternalCategoryId = "3003",
                ExternalCategoryName = "Cep Telefonu",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new TemplateCategoryMarketplaceMapping
            {
                TemplateCategoryDataId = childCategory.Id,
                MarketPlaceId = 3, // Hepsiburada
                ExternalCategoryId = "HB-CEP-001",
                ExternalCategoryName = "Cep Telefonu",
                CreatedAt = DateTimeOffset.UtcNow
            }
        );

        // Attribute
        var attr = new TemplateCategoryAttributeData
        {
            TemplateCategoryDataId = childCategory.Id,
            AttributeKey = "Renk",
            AttributeHumanized = "Renk",
            AllowCustom = false,
            IsRequired = true,
            IsSlicer = false,
            IsVarianter = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.TemplateCategoryAttributeData.Add(attr);
        await dbContext.SaveChangesAsync();

        // Attribute marketplace mapping
        dbContext.TemplateCategoryAttrMarketplaceMappings.Add(new TemplateCategoryAttrMarketplaceMapping
        {
            TemplateCategoryAttributeDataId = attr.Id,
            MarketPlaceId = 1,
            ExternalAttributeId = 348,
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Attribute value
        var value = new TemplateCategoryAttributeValueData
        {
            TemplateCategoryAttributeDataId = attr.Id,
            ValueName = "Siyah",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.TemplateCategoryAttributeValueData.Add(value);
        await dbContext.SaveChangesAsync();

        // Value marketplace mapping
        dbContext.TemplateCategoryAttrValueMarketplaceMappings.Add(new TemplateCategoryAttrValueMarketplaceMapping
        {
            TemplateCategoryAttributeValueDataId = value.Id,
            MarketPlaceId = 1,
            ExternalValueId = 4296,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return package.Id;
    }

    #endregion

    // ─── GetAvailablePackages Tests ───────────────────────────────────

    [Fact]
    public async Task GetAvailablePackages_ShouldReturnPublishedPackages()
    {
        // Arrange
        await SeedCategoryPackageAsync(isPublished: true);
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.GetAvailablePackagesAsync(new MatchedEntityPackagePaginatedRequest());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Data.Items.Should().Contain(p => p.Name == "Elektronik > Cep Telefonu");
    }

    [Fact]
    public async Task GetAvailablePackages_ShouldNotReturnUnpublishedPackages()
    {
        // Arrange
        await SeedCategoryPackageAsync(isPublished: false, name: "Draft Paket");
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.GetAvailablePackagesAsync(new MatchedEntityPackagePaginatedRequest());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items.Should().NotContain(p => p.Name == "Draft Paket");
    }

    [Fact]
    public async Task GetAvailablePackages_ShouldFilterByEntityType()
    {
        // Arrange
        await SeedCategoryPackageAsync();
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.GetAvailablePackagesAsync(
            new MatchedEntityPackagePaginatedRequest { EntityType = MatchedEntityType.Brand });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items.Should().NotContain(p => p.EntityType == MatchedEntityType.Category);
    }

    [Fact]
    public async Task GetAvailablePackages_ShouldFilterBySearchTerm()
    {
        // Arrange
        await SeedCategoryPackageAsync(name: "Giyim > Elbise");
        await SeedCategoryPackageAsync(name: "Elektronik > Cep Telefonu");
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.GetAvailablePackagesAsync(
            new MatchedEntityPackagePaginatedRequest { SearchTerm = "Giyim" });

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Items.Should().OnlyContain(p => p.Name.Contains("Giyim"));
    }

    // ─── GetPackageDetail Tests ──────────────────────────────────────

    [Fact]
    public async Task GetPackageDetail_ShouldReturnFullHierarchy()
    {
        // Arrange
        var packageId = await SeedCategoryPackageAsync();
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.GetPackageDetailAsync(packageId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Categories.Should().HaveCount(1); // 1 root
        var root = result.Data.Categories[0];
        root.Name.Should().Be("Elektronik");
        root.Children.Should().HaveCount(1);

        var child = root.Children[0];
        child.Name.Should().Be("Cep Telefonu");
        child.MarketplaceMappings.Should().HaveCount(2); // Trendyol + HB
        child.Attributes.Should().HaveCount(1);

        var attr = child.Attributes[0];
        attr.AttributeKey.Should().Be("Renk");
        attr.IsVarianter.Should().BeTrue();
        attr.Values.Should().HaveCount(1);
        attr.Values[0].ValueName.Should().Be("Siyah");
    }

    // ─── DetectConflicts Tests ───────────────────────────────────────

    [Fact]
    public async Task DetectConflicts_ShouldReturnEmpty_WhenNoConflicts()
    {
        // Arrange
        var packageId = await SeedCategoryPackageAsync();
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.DetectConflictsAsync(packageId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectConflicts_ShouldDetectNameConflict()
    {
        // Arrange — once ayni isimde bir kategori olustur
        using (var dbContext = CreateDbContext())
        {
            dbContext.Categories.Add(new Entity.Categories.Category
            {
                Name = "Elektronik",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var packageId = await SeedCategoryPackageAsync();
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.DetectConflictsAsync(packageId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Data.Should().Contain(c =>
            c.TemplateName == "Elektronik" &&
            (c.ConflictType == ConflictType.NameMatch || c.ConflictType == ConflictType.Both));
    }

    // ─── ImportPackage Tests ─────────────────────────────────────────

    [Fact]
    public async Task ImportPackage_ShouldCreateCategoriesAndMappings_WhenNoConflicts()
    {
        // Arrange
        await SeedBasicEntitiesAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedMarketPlaceAsync(3, "Hepsiburada");

        var packageId = await SeedCategoryPackageAsync();
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        var request = new ImportPackageRequest { PackageId = packageId };

        // Act
        var result = await service.ImportPackageAsync(request);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();

        // Kategoriler olusturulmus olmali
        var rootCategory = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Name == "Elektronik" && c.ImportSource == Entity.Categories.ImportSource.MatchedEntity);
        rootCategory.Should().NotBeNull();

        var childCategory = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Name == "Cep Telefonu" && c.ImportSource == Entity.Categories.ImportSource.MatchedEntity);
        childCategory.Should().NotBeNull();
        childCategory!.SuperCategoryId.Should().Be(rootCategory!.Id);

        // Marketplace mapping'ler olusturulmus olmali
        var marketplaceMappings = await dbContext.CategoryMarketplaces
            .Where(cm => cm.CategoryId == childCategory.Id)
            .ToListAsync();
        marketplaceMappings.Should().HaveCount(2); // Trendyol + HB

        // Attribute olusturulmus olmali
        var attr = await dbContext.CategoryAttributes
            .FirstOrDefaultAsync(a => a.CategoryAttributeKey == "Renk");
        attr.Should().NotBeNull();

        // Attribute marketplace match olusturulmus olmali
        var attrMatch = await dbContext.CategoryAttributeMarketPlaceMatches
            .FirstOrDefaultAsync(m => m.ApplicationCategoryAttributeId == attr!.Id && m.MarketPlaceId == 1);
        attrMatch.Should().NotBeNull();
        attrMatch!.MarketPlaceCategoryAttributeId.Should().Be(348);

        // Attribute value olusturulmus olmali
        var attrValue = await dbContext.CategoryAttributeValues
            .FirstOrDefaultAsync(v => v.Name == "Siyah");
        attrValue.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportPackage_ShouldSetImportSourceToMatchedEntity()
    {
        // Arrange
        await SeedBasicEntitiesAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedMarketPlaceAsync(3, "Hepsiburada");

        var packageId = await SeedCategoryPackageAsync();
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        await service.ImportPackageAsync(new ImportPackageRequest { PackageId = packageId });

        // Assert
        using var dbContext = CreateDbContext();
        var categories = await dbContext.Categories
            .Where(c => c.ImportSource == Entity.Categories.ImportSource.MatchedEntity)
            .ToListAsync();
        categories.Should().HaveCount(2); // root + child
    }

    [Fact]
    public async Task ImportPackage_ShouldReturnError_WhenPackageNotFound()
    {
        // Arrange
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;

        // Act
        var result = await service.ImportPackageAsync(new ImportPackageRequest { PackageId = 99999 });

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ImportPackage_WithUseExistingStrategy_ShouldKeepExistingEntity()
    {
        // Arrange
        await SeedBasicEntitiesAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedMarketPlaceAsync(3, "Hepsiburada");

        // Mevcut kategori olustur
        int existingCategoryId;
        using (var dbContext = CreateDbContext())
        {
            var existing = new Entity.Categories.Category
            {
                Name = "Elektronik",
                ImportSource = Entity.Categories.ImportSource.Manual,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Categories.Add(existing);
            await dbContext.SaveChangesAsync();
            existingCategoryId = existing.Id;
        }

        var packageId = await SeedCategoryPackageAsync();

        // Cakismalari tespit et
        var (service, scope) = GetScopedService<IMatchedEntityImportManager>();
        using var _ = scope;
        var conflicts = await service.DetectConflictsAsync(packageId);

        var request = new ImportPackageRequest
        {
            PackageId = packageId,
            ConflictResolutions = conflicts.Data
                .Select(c => new ConflictResolution
                {
                    TemplateEntityId = c.TemplateEntityId,
                    ExistingEntityId = c.ExistingEntityId,
                    Strategy = ConflictResolutionStrategy.UseExisting
                }).ToList()
        };

        // Act
        var result = await service.ImportPackageAsync(request);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var verifyContext = CreateDbContext();
        var existingCategory = await verifyContext.Categories.FindAsync(existingCategoryId);
        existingCategory.Should().NotBeNull();
        existingCategory!.ImportSource.Should().Be(Entity.Categories.ImportSource.Manual,
            "UseExisting stratejisi mevcut entity'yi degistirmemeli");
    }
}
