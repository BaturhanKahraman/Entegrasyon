using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest;

/// <summary>
/// EF Core migration testleri — tum migration'lar sorunsuz uygulanabiliyor mu?
/// Seed verileri dogru mu?
/// </summary>
[Trait("Category", "Integration")]
public class MigrationTests : IntegrationTestBase
{
    public MigrationTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    [Fact]
    public async Task AllMigrations_ShouldBeApplied_WithoutErrors()
    {
        // Arrange & Act — migration'lar InitializeAsync'te uygulanir
        using var dbContext = CreateDbContext();

        // Assert — pending migration olmamali
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty("All migrations should already be applied during test initialization");
    }

    [Fact]
    public async Task AppliedMigrations_ShouldNotBeEmpty()
    {
        using var dbContext = CreateDbContext();

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().NotBeEmpty("At least one migration should exist");
    }

    [Fact]
    public async Task SeedData_ShouldContainAdminRole()
    {
        using var dbContext = CreateDbContext();

        var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        adminRole.Should().NotBeNull("Admin role should be seeded");
        adminRole!.Id.Should().Be(1);
    }

    [Fact]
    public async Task SeedData_ShouldContainAdminUser()
    {
        using var dbContext = CreateDbContext();

        var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "Admin");
        adminUser.Should().NotBeNull("Admin user should be seeded");
        adminUser!.Email.Should().Be("admin@admin.com");
    }

    [Fact]
    public async Task SeedData_ShouldContainApplicationSettings()
    {
        using var dbContext = CreateDbContext();

        var settings = await dbContext.ApplicationSettings.ToListAsync();
        settings.Should().NotBeEmpty("Application settings should be seeded");
        settings.Should().Contain(s => s.Key == "CompanyName");
        settings.Should().Contain(s => s.Key == "Currency");
        settings.Should().Contain(s => s.Key == "TaxRate");
    }

    [Fact]
    public async Task AllDbSets_ShouldBeQueryable()
    {
        using var dbContext = CreateDbContext();

        // Verify that all DbSet'ler sorgulanabilir (table/view mevcut)
        // Her biri icin basit bir Count sorgusu yapariz
        var categoryCount = await dbContext.Categories.CountAsync();
        categoryCount.Should().BeGreaterThanOrEqualTo(0);

        var brandCount = await dbContext.Brands.CountAsync();
        brandCount.Should().BeGreaterThanOrEqualTo(0);

        var productCount = await dbContext.MainProducts.CountAsync();
        productCount.Should().BeGreaterThanOrEqualTo(0);

        var orderCount = await dbContext.Orders.CountAsync();
        orderCount.Should().BeGreaterThanOrEqualTo(0);

        var marketplaceCount = await dbContext.MarketPlaces.CountAsync();
        marketplaceCount.Should().BeGreaterThanOrEqualTo(0);
    }
}
