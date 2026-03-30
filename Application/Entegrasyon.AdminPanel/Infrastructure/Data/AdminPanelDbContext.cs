using Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.AdminPanel.Infrastructure.Data;

public class AdminPanelDbContext(DbContextOptions<AdminPanelDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantLicense> TenantLicenses => Set<TenantLicense>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();
    public DbSet<AiCreditAccount> AiCreditAccounts => Set<AiCreditAccount>();
    public DbSet<AiCreditTransaction> AiCreditTransactions => Set<AiCreditTransaction>();
    public DbSet<FeaturePackage> FeaturePackages => Set<FeaturePackage>();
    public DbSet<FeaturePackagePermission> FeaturePackagePermissions => Set<FeaturePackagePermission>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();

    // Master Catalog
    public DbSet<MasterBrand> MasterBrands => Set<MasterBrand>();
    public DbSet<MasterBrandMarketplaceMapping> MasterBrandMarketplaceMappings => Set<MasterBrandMarketplaceMapping>();
    public DbSet<MasterCargoCompany> MasterCargoCompanies => Set<MasterCargoCompany>();
    public DbSet<MasterCargoCompanyMarketplaceMapping> MasterCargoCompanyMarketplaceMappings => Set<MasterCargoCompanyMarketplaceMapping>();
    public DbSet<MasterCategory> MasterCategories => Set<MasterCategory>();
    public DbSet<MasterAttribute> MasterAttributes => Set<MasterAttribute>();
    public DbSet<MasterAttributeValue> MasterAttributeValues => Set<MasterAttributeValue>();
    public DbSet<MasterCategoryAttribute> MasterCategoryAttributes => Set<MasterCategoryAttribute>();
    public DbSet<MasterCategoryMarketplaceMapping> MasterCategoryMarketplaceMappings => Set<MasterCategoryMarketplaceMapping>();
    public DbSet<MasterAttributeMarketplaceMapping> MasterAttributeMarketplaceMappings => Set<MasterAttributeMarketplaceMapping>();
    public DbSet<MasterValueMarketplaceMapping> MasterValueMarketplaceMappings => Set<MasterValueMarketplaceMapping>();
    public DbSet<MarketplaceReference> MarketplaceReferences => Set<MarketplaceReference>();
    public DbSet<SectorPackage> SectorPackages => Set<SectorPackage>();
    public DbSet<SectorPackageCategory> SectorPackageCategories => Set<SectorPackageCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasIndex(t => t.Subdomain).IsUnique();
            e.HasMany(t => t.Licenses).WithOne(l => l.Tenant).HasForeignKey(l => l.TenantId);
            e.HasMany(t => t.ApplicationLogs).WithOne(l => l.Tenant).HasForeignKey(l => l.TenantId);
            e.HasOne(t => t.AiCreditAccount).WithOne(a => a.Tenant).HasForeignKey<AiCreditAccount>(a => a.TenantId);
        });

        modelBuilder.Entity<AiCreditAccount>(e =>
        {
            e.HasMany(a => a.Transactions).WithOne(t => t.Account).HasForeignKey(t => t.AiCreditAccountId);
        });

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<FeaturePackage>(e =>
        {
            e.HasIndex(p => p.Name).IsUnique();
            e.HasMany(p => p.Permissions).WithOne(pp => pp.Package).HasForeignKey(pp => pp.FeaturePackageId);
            e.HasMany(p => p.Subscriptions).WithOne(s => s.Package).HasForeignKey(s => s.FeaturePackageId);
        });

        modelBuilder.Entity<TenantSubscription>(e =>
        {
            e.HasOne(s => s.Tenant).WithMany().HasForeignKey(s => s.TenantId);
        });

        // ── Master Catalog ──────────────────────────────────────────────────

        modelBuilder.Entity<MasterBrand>(e =>
        {
            e.HasIndex(b => b.Name).IsUnique();
            e.HasMany(b => b.MarketplaceMappings).WithOne(m => m.MasterBrand).HasForeignKey(m => m.MasterBrandId);
        });

        modelBuilder.Entity<MasterBrandMarketplaceMapping>(e =>
        {
            e.HasIndex(m => new { m.MasterBrandId, m.MarketplaceId }).IsUnique();
        });

        modelBuilder.Entity<MasterCargoCompany>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
            e.HasMany(c => c.MarketplaceMappings).WithOne(m => m.MasterCargoCompany).HasForeignKey(m => m.MasterCargoCompanyId);
        });

        modelBuilder.Entity<MasterCargoCompanyMarketplaceMapping>(e =>
        {
            e.HasIndex(m => new { m.MasterCargoCompanyId, m.MarketplaceId }).IsUnique();
        });

        modelBuilder.Entity<MasterCategory>(e =>
        {
            e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.CategoryAttributes).WithOne(ca => ca.MasterCategory).HasForeignKey(ca => ca.MasterCategoryId);
            e.HasMany(c => c.MarketplaceMappings).WithOne(m => m.MasterCategory).HasForeignKey(m => m.MasterCategoryId);
            e.HasMany(c => c.SectorPackageCategories).WithOne(s => s.MasterCategory).HasForeignKey(s => s.MasterCategoryId);
        });

        modelBuilder.Entity<MasterAttribute>(e =>
        {
            e.HasMany(a => a.Values).WithOne(v => v.MasterAttribute).HasForeignKey(v => v.MasterAttributeId);
            e.HasMany(a => a.CategoryLinks).WithOne(ca => ca.MasterAttribute).HasForeignKey(ca => ca.MasterAttributeId);
            e.HasMany(a => a.MarketplaceMappings).WithOne(m => m.MasterAttribute).HasForeignKey(m => m.MasterAttributeId);
        });

        modelBuilder.Entity<MasterAttributeValue>(e =>
        {
            e.HasMany(v => v.MarketplaceMappings).WithOne(m => m.MasterAttributeValue).HasForeignKey(m => m.MasterAttributeValueId);
        });

        modelBuilder.Entity<MasterCategoryAttribute>(e =>
        {
            e.HasIndex(ca => new { ca.MasterCategoryId, ca.MasterAttributeId }).IsUnique();
        });

        modelBuilder.Entity<MasterCategoryMarketplaceMapping>(e =>
        {
            e.HasIndex(m => new { m.MasterCategoryId, m.MarketplaceId }).IsUnique();
        });

        modelBuilder.Entity<MasterAttributeMarketplaceMapping>(e =>
        {
            e.HasIndex(m => new { m.MasterAttributeId, m.MarketplaceId }).IsUnique();
        });

        modelBuilder.Entity<MasterValueMarketplaceMapping>(e =>
        {
            e.HasIndex(m => new { m.MasterAttributeValueId, m.MarketplaceId }).IsUnique();
        });

        modelBuilder.Entity<MarketplaceReference>(e =>
        {
            e.HasIndex(r => new { r.MarketplaceId, r.EntityType, r.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<SectorPackage>(e =>
        {
            e.HasMany(s => s.Categories).WithOne(c => c.SectorPackage).HasForeignKey(c => c.SectorPackageId);
        });
    }
}

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Tenant : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "PostgreSQL"; // PostgreSQL, SqlServer, etc.
    public bool IsActive { get; set; } = true;
    public int UserCount { get; set; }
    public string? Notes { get; set; }

    public ICollection<TenantLicense> Licenses { get; set; } = [];
    public ICollection<ApplicationLog> ApplicationLogs { get; set; } = [];
    public AiCreditAccount? AiCreditAccount { get; set; }
}

public class TenantLicense : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LicenseType Type { get; set; } = LicenseType.Standard;
    public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
    public string? Notes { get; set; }
}

public enum LicenseType
{
    Trial,
    Standard,
    Premium,
    Enterprise
}

public class AdminUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ApplicationLog : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string Level { get; set; } = "Information"; // Information, Warning, Error, Critical
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? StackTrace { get; set; }
    public string? UserName { get; set; }
}

public class AiCreditAccount : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int ImageGenerationCredits { get; set; }
    public int ProductDescriptionCredits { get; set; }
    public ICollection<AiCreditTransaction> Transactions { get; set; } = [];
}

public class AiCreditTransaction : BaseEntity
{
    public int AiCreditAccountId { get; set; }
    public AiCreditAccount Account { get; set; } = null!;
    public AiCreditType CreditType { get; set; }
    public int Amount { get; set; } // Positive = add, Negative = consume
    public string? Description { get; set; }
}

public enum AiCreditType
{
    ImageGeneration,
    ProductDescription
}
