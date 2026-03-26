using System.Linq.Expressions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts.Seed;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.DiscountVouchers;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Invoices;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Labels;
using Entegrasyon.Entity.Settings;
using Entegrasyon.Entity.Marketplace;
using Entegrasyon.Entity.Shipping;
using Entegrasyon.Entity.POS;
using Entegrasyon.Entity.Templates;
using Entegrasyon.Entity.User;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

public class IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasCollation("CaseInsensitive", locale: "en-u-ks-primary", provider: "icu", deterministic: false);

        // Materialized View: mv_category_summary — kategori listeleme agregasyonu
        modelBuilder.Entity<CategorySummaryView>().ToView("mv_category_summary");

        // Materialized View: mv_product_stock_summary — dashboard düşük stok agregasyonu
        modelBuilder.Entity<ProductStockSummaryView>().ToView("mv_product_stock_summary");

        modelBuilder.Seed();

        base.OnModelCreating(modelBuilder);
    }


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            // DateTimeOffset alanlarını UTC'ye dönüştür — EF Core metadata cache kullanır, reflection yok
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.ClrType == typeof(DateTimeOffset) ||
                    prop.Metadata.ClrType == typeof(DateTimeOffset?))
                {
                    if (prop.CurrentValue is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
                        prop.CurrentValue = new DateTimeOffset(dto.UtcDateTime, TimeSpan.Zero);
                }
            }

            // CreatedAt / UpdatedAt otomatik ayarla
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Added)
                    baseEntity.CreatedAt = DateTimeOffset.UtcNow;
                else
                    baseEntity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<CategoryAttribute> CategoryAttributes { get; set; }
    public virtual DbSet<CategoryAttributeValue> CategoryAttributeValues { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public virtual DbSet<Brand> Brands { get; set; }
    public virtual DbSet<Product> MainProducts { get; set; }
    public virtual DbSet<ProductVariant> ProductVariants { get; set; }
    public virtual DbSet<BranchOfficeStock> BranchOfficeStocks { get; set; }
    public virtual DbSet<ChangeProduct> ChangeProducts { get; set; }
    public virtual DbSet<DiscountVoucher> DiscountVouchers { get; set; }
    public virtual DbSet<ReturnProduct> ReturnProducts { get; set; }
    public virtual DbSet<Sale> Sales { get; set; }
    public virtual DbSet<SaleItem> SaleItems { get; set; }
    public virtual DbSet<BranchOffice> BranchOffices { get; set; }
    public virtual DbSet<Image> Images { get; set; }
    public virtual DbSet<MarketPlace> MarketPlaces { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<ApplicationLog> Logs { get; set; }
    public virtual DbSet<ProductActivityLog> ProductActivityLogs { get; set; }
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<RetailCustomer> RetailCustomers { get; set; }
    public virtual DbSet<CorporateCustomer> CorporateCustomers { get; set; }
    public virtual DbSet<CargoCompany> CargoCompanies { get; set; }
    public virtual DbSet<CategoryAttributeCategory> CategoryAttributeCategories { get; set; }
    public virtual DbSet<ProductMarketplace> ProductMarketplaces { get; set; }
    public virtual DbSet<CategoryMarketPlaceMatch> CategoryMarketPlaceMatches { get; set; }
    public virtual DbSet<CategoryAttributeMarketPlaceMatch> CategoryAttributeMarketPlaceMatches { get; set; }
    public virtual DbSet<CategoryAttributeValueMarketPlaceMatch> CategoryAttributeValueMarketPlaceMatches { get; set; }
    public virtual DbSet<BrandMarketPlaceMatch> BrandMarketPlaceMatches { get; set; }
    public virtual DbSet<CargoCompanyMarketPlaceMatch> CargoCompanyMarketPlaceMatches { get; set; }
    public virtual DbSet<AttributeKeyValue> AttributeKeyValues { get; set; }
    public virtual DbSet<StockMovement> StockMovements { get; set; }
    public virtual DbSet<ProductVariantMarketplaceOverride> ProductVariantMarketplaceOverrides { get; set; }
    public virtual DbSet<LabelTemplate> LabelTemplates { get; set; }
    public virtual DbSet<CategorySummaryView> CategorySummaries { get; set; }
    public virtual DbSet<ProductStockSummaryView> ProductStockSummaries { get; set; }
    public virtual DbSet<CategoryMarketplace> CategoryMarketplaces { get; set; }
    public virtual DbSet<MarketPlaceWarehouse> MarketPlaceWarehouses { get; set; }
    public virtual DbSet<ApplicationUser> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Login> Logins { get; set; }
    public virtual DbSet<ApplicationSetting> ApplicationSettings { get; set; }
    public virtual DbSet<NotificationSetting> NotificationSettings { get; set; }
    public virtual DbSet<EFaturaRecord> EFaturaRecords { get; set; }

    // E-Fatura / E-Arsiv (genel amacli)
    public virtual DbSet<EInvoice> EInvoices { get; set; }
    public virtual DbSet<EInvoiceLine> EInvoiceLines { get; set; }
    public virtual DbSet<EInvoiceIntegratorConfig> EInvoiceIntegratorConfigs { get; set; }

    // Template (MatchedEntity) DbSets
    public virtual DbSet<MatchedEntityPackage> MatchedEntityPackages { get; set; }
    public virtual DbSet<TemplateCategoryData> TemplateCategoryData { get; set; }
    public virtual DbSet<TemplateCategoryMarketplaceMapping> TemplateCategoryMarketplaceMappings { get; set; }
    public virtual DbSet<TemplateCategoryAttributeData> TemplateCategoryAttributeData { get; set; }
    public virtual DbSet<TemplateCategoryAttrMarketplaceMapping> TemplateCategoryAttrMarketplaceMappings { get; set; }
    public virtual DbSet<TemplateCategoryAttributeValueData> TemplateCategoryAttributeValueData { get; set; }
    public virtual DbSet<TemplateCategoryAttrValueMarketplaceMapping> TemplateCategoryAttrValueMarketplaceMappings { get; set; }
    public virtual DbSet<TemplateBrandData> TemplateBrandData { get; set; }
    public virtual DbSet<TemplateBrandMarketplaceMapping> TemplateBrandMarketplaceMappings { get; set; }
    public virtual DbSet<TemplateCargoCompanyData> TemplateCargoCompanyData { get; set; }
    public virtual DbSet<TemplateCargoCompanyMarketplaceMapping> TemplateCargoCompanyMarketplaceMappings { get; set; }

    // Commission
    public virtual DbSet<MarketplaceCommissionRate> MarketplaceCommissionRates { get; set; }

    // Shipping
    public virtual DbSet<ShipmentTracking> ShipmentTrackings { get; set; } = null!;
    public virtual DbSet<ShipmentStatusHistory> ShipmentStatusHistories { get; set; } = null!;

    // Bulk Operations
    public virtual DbSet<BulkOperationLog> BulkOperationLogs { get; set; } = null!;

    // POS
    public virtual DbSet<POSSession> POSSessions { get; set; }
    public virtual DbSet<POSTransaction> POSTransactions { get; set; }
    public virtual DbSet<CashMovement> CashMovements { get; set; }

    // Storefront
    public virtual DbSet<StorefrontSettings> StorefrontSettings { get; set; }
    public virtual DbSet<StorefrontDomainMapping> StorefrontDomainMappings { get; set; }
    public virtual DbSet<StorefrontBanner> StorefrontBanners { get; set; }
    public virtual DbSet<StorefrontPage> StorefrontPages { get; set; }
    public virtual DbSet<StorefrontCustomerAuth> StorefrontCustomerAuths { get; set; }
    public virtual DbSet<Cart> Carts { get; set; }
    public virtual DbSet<CartItem> CartItems { get; set; }

    //public DbSet<UsersRoles> UsersRoles { get; set; }
    //public DbSet<UsersClaims> UsersClaims { get; set; }
    //public DbSet<RolesClaims> RolesClaims { get; set; }

}
