using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.AdminPanel.Infrastructure.Data;

/// <summary>
/// Admin Panel'in PostgreSQL'e bağlanan ikinci DbContext'i.
/// Sadece template (MatchedEntity) tablolarını map eder.
/// Ana uygulama ile aynı PostgreSQL veritabanını paylaşır.
/// </summary>
public class TemplateDbContext(DbContextOptions<TemplateDbContext> options) : DbContext(options)
{
    public DbSet<MatchedEntityPackage> MatchedEntityPackages => Set<MatchedEntityPackage>();
    public DbSet<TemplateCategoryData> TemplateCategoryData => Set<TemplateCategoryData>();
    public DbSet<TemplateCategoryMarketplaceMapping> TemplateCategoryMarketplaceMappings => Set<TemplateCategoryMarketplaceMapping>();
    public DbSet<TemplateCategoryAttributeData> TemplateCategoryAttributeData => Set<TemplateCategoryAttributeData>();
    public DbSet<TemplateCategoryAttrMarketplaceMapping> TemplateCategoryAttrMarketplaceMappings => Set<TemplateCategoryAttrMarketplaceMapping>();
    public DbSet<TemplateCategoryAttributeValueData> TemplateCategoryAttributeValueData => Set<TemplateCategoryAttributeValueData>();
    public DbSet<TemplateCategoryAttrValueMarketplaceMapping> TemplateCategoryAttrValueMarketplaceMappings => Set<TemplateCategoryAttrValueMarketplaceMapping>();
    public DbSet<TemplateBrandData> TemplateBrandData => Set<TemplateBrandData>();
    public DbSet<TemplateBrandMarketplaceMapping> TemplateBrandMarketplaceMappings => Set<TemplateBrandMarketplaceMapping>();
    public DbSet<TemplateCargoCompanyData> TemplateCargoCompanyData => Set<TemplateCargoCompanyData>();
    public DbSet<TemplateCargoCompanyMarketplaceMapping> TemplateCargoCompanyMarketplaceMappings => Set<TemplateCargoCompanyMarketplaceMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MatchedEntityPackage
        modelBuilder.Entity<MatchedEntityPackage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Version).HasDefaultValue(1);
            e.HasMany(x => x.Categories).WithOne(x => x.Package).HasForeignKey(x => x.PackageId);
            e.HasMany(x => x.Brands).WithOne(x => x.Package).HasForeignKey(x => x.PackageId);
            e.HasMany(x => x.CargoCompanies).WithOne(x => x.Package).HasForeignKey(x => x.PackageId);
        });

        // TemplateCategoryData
        modelBuilder.Entity<TemplateCategoryData>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(500);
            e.Property(x => x.DefaultVatRate).HasPrecision(5, 2);
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentTemplateCategoryDataId);
            e.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCategoryData).HasForeignKey(x => x.TemplateCategoryDataId);
            e.HasMany(x => x.Attributes).WithOne(x => x.TemplateCategoryData).HasForeignKey(x => x.TemplateCategoryDataId);
        });

        // TemplateCategoryMarketplaceMapping
        modelBuilder.Entity<TemplateCategoryMarketplaceMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalCategoryId).IsRequired().HasMaxLength(200);
            e.Property(x => x.ExternalCategoryName).HasMaxLength(500);
        });

        // TemplateCategoryAttributeData
        modelBuilder.Entity<TemplateCategoryAttributeData>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AttributeKey).IsRequired().HasMaxLength(500);
            e.Property(x => x.AttributeHumanized).HasMaxLength(500);
            e.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCategoryAttributeData).HasForeignKey(x => x.TemplateCategoryAttributeDataId);
            e.HasMany(x => x.Values).WithOne(x => x.TemplateCategoryAttributeData).HasForeignKey(x => x.TemplateCategoryAttributeDataId);
        });

        // TemplateCategoryAttrMarketplaceMapping
        modelBuilder.Entity<TemplateCategoryAttrMarketplaceMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalAttributeExternalId).HasMaxLength(200);
        });

        // TemplateCategoryAttributeValueData
        modelBuilder.Entity<TemplateCategoryAttributeValueData>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ValueName).IsRequired().HasMaxLength(500);
            e.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCategoryAttributeValueData).HasForeignKey(x => x.TemplateCategoryAttributeValueDataId);
        });

        // TemplateCategoryAttrValueMarketplaceMapping
        modelBuilder.Entity<TemplateCategoryAttrValueMarketplaceMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalValueExternalId).HasMaxLength(200);
        });

        // TemplateBrandData
        modelBuilder.Entity<TemplateBrandData>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(500);
            e.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateBrandData).HasForeignKey(x => x.TemplateBrandDataId);
        });

        // TemplateBrandMarketplaceMapping
        modelBuilder.Entity<TemplateBrandMarketplaceMapping>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalBrandExternalId).HasMaxLength(200);
        });

        // TemplateCargoCompanyData
        modelBuilder.Entity<TemplateCargoCompanyData>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(500);
            e.Property(x => x.Code).HasMaxLength(100);
            e.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCargoCompanyData).HasForeignKey(x => x.TemplateCargoCompanyDataId);
        });

        // TemplateCargoCompanyMarketplaceMapping
        modelBuilder.Entity<TemplateCargoCompanyMarketplaceMapping>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }
}
