using Entegrasyon.AdminPanel.Infrastructure.Auth;
using Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;
using Microsoft.Extensions.Logging.Abstractions;

namespace Entegrasyon.AdminPanel.Infrastructure.Data;

public static class SeedData
{
    public static void Initialize(AdminPanelDbContext context, string? mainConnectionString = null,
        string? catalogSnapshotPath = null)
    {
        SeedAdminUser(context);
        SeedFeaturePackages(context);
        SeedDevelopmentTenant(context, mainConnectionString);

        // Master catalog seed (async — sync wrapper kullanıyoruz startup sırasında)
        var catalogSeedService = new MasterCatalogSeedService(context, NullLogger<MasterCatalogSeedService>.Instance);
        catalogSeedService.SeedAsync(catalogSnapshotPath).GetAwaiter().GetResult();
    }

    private static void SeedAdminUser(AdminPanelDbContext context)
    {
        if (context.AdminUsers.Any()) return;

        context.AdminUsers.Add(new AdminUser
        {
            Username = "admin",
            PasswordHash = PasswordHasher.Hash("admin123"),
            DisplayName = "Sistem Yöneticisi",
            IsActive = true
        });

        context.SaveChanges();
    }

    private static void SeedFeaturePackages(AdminPanelDbContext context)
    {
        if (context.FeaturePackages.Any()) return;

        var starterPermissions = new List<string>
        {
            "Permissions.Products.View", "Permissions.Products.Create", "Permissions.Products.Edit", "Permissions.Products.Delete",
            "Permissions.Categories.View", "Permissions.Categories.Create", "Permissions.Categories.Edit", "Permissions.Categories.Delete",
            "Permissions.Brands.View", "Permissions.Brands.Create", "Permissions.Brands.Edit", "Permissions.Brands.Delete",
            "Permissions.Customers.View", "Permissions.Customers.Create", "Permissions.Customers.Edit", "Permissions.Customers.Delete",
            "Permissions.BranchOffices.View", "Permissions.BranchOffices.Create", "Permissions.BranchOffices.Edit", "Permissions.BranchOffices.Delete",
            "Permissions.Sales.View", "Permissions.Sales.Create", "Permissions.Sales.Edit", "Permissions.Sales.Delete",
            "Permissions.Orders.View",
            "Permissions.Settings.View", "Permissions.Settings.Edit",
            "Permissions.Users.View", "Permissions.Users.Create", "Permissions.Users.Edit", "Permissions.Users.Delete",
            "Permissions.Roles.View", "Permissions.Roles.Create", "Permissions.Roles.Edit", "Permissions.Roles.Delete",
            "Permissions.Notifications.View",
            "Permissions.Logs.View",
        };

        var proExtraPermissions = new List<string>
        {
            "Permissions.Orders.Create", "Permissions.Orders.Edit", "Permissions.Orders.Delete",
            "Permissions.Cargo.View", "Permissions.Cargo.Create", "Permissions.Cargo.Edit", "Permissions.Cargo.Delete",
            "Permissions.Reports.View", "Permissions.Reports.Create", "Permissions.Reports.Edit", "Permissions.Reports.Delete",
            "Permissions.Integrations.View", "Permissions.Integrations.Create", "Permissions.Integrations.Edit", "Permissions.Integrations.Delete",
            "Permissions.Marketplace.View", "Permissions.Marketplace.Create", "Permissions.Marketplace.Edit", "Permissions.Marketplace.Delete",
        };

        var allPermissions = starterPermissions.Concat(proExtraPermissions).ToList();

        var starter = new FeaturePackage
        {
            Name = "Starter",
            Description = "Temel urun, kategori, musteri ve satis yonetimi",
            MonthlyPrice = 499,
            Permissions = starterPermissions.Select(p => new FeaturePackagePermission { PermissionKey = p }).ToList()
        };

        var pro = new FeaturePackage
        {
            Name = "Pro",
            Description = "Tum marketplace entegrasyonlari, raporlar ve kargo yonetimi",
            MonthlyPrice = 999,
            Permissions = allPermissions.Select(p => new FeaturePackagePermission { PermissionKey = p }).ToList()
        };

        var enterprise = new FeaturePackage
        {
            Name = "Enterprise",
            Description = "Tam ozellik paketi: storefront, API erisimi ve premium destek",
            MonthlyPrice = 1999,
            Permissions = allPermissions.Select(p => new FeaturePackagePermission { PermissionKey = p }).ToList()
        };

        context.FeaturePackages.AddRange(starter, pro, enterprise);
        context.SaveChanges();
    }

    private static void SeedDevelopmentTenant(AdminPanelDbContext context, string? mainConnectionString)
    {
        if (context.Tenants.Any(t => t.Subdomain == "dev")) return;

        var devTenant = new Tenant
        {
            CompanyName = "Development Tenant",
            Subdomain = "dev",
            ContactEmail = "dev@entegrasyon.local",
            ConnectionString = mainConnectionString
                ?? "Host=localhost;Port=5432;Database=IntegrationDb;Username=postgres;Password=postgres",
            DatabaseType = "PostgreSQL",
            IsActive = true,
            UserCount = 1,
            Notes = "Auto-created development tenant"
        };
        context.Tenants.Add(devTenant);
        context.SaveChanges();

        var proPackage = context.FeaturePackages.FirstOrDefault(p => p.Name == "Pro");
        if (proPackage is not null)
        {
            context.TenantSubscriptions.Add(new TenantSubscription
            {
                TenantId = devTenant.Id,
                FeaturePackageId = proPackage.Id,
                StartDate = DateTime.UtcNow,
                EndDate = null,
                IsActive = true
            });
            context.SaveChanges();
        }
    }
}
