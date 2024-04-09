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
using Shared.User;
using System.Reflection;
using Entegrasyon.Entity.Barcode;
using Entegrasyon.Entity.Brands;
using Shared.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.DiscountVouchers;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

public class IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasCollation("CaseInsensitive", locale: "en-u-ks-primary", provider: "icu", deterministic: false);
        modelBuilder.Seed();
        //modelBuilder.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
    
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        foreach(var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch(entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    break;
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
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<RetailCustomer> RetailCustomers { get; set; }
    public virtual DbSet<CorporateCustomer> CorporateCustomers { get; set; }
    public virtual DbSet<CargoCompany> CargoCompanies { get; set; }
    public virtual DbSet<CategoryAttributeCategory> CategoryAttributeCategories { get; set; }
    public virtual DbSet<TempBarcode> TempBarcodes { get; set; }
    public virtual DbSet<CategoryMarketPlaceMatch> CategoryMarketPlaceMatches { get; set; }
    public virtual DbSet<CategoryAttributeMarketPlaceMatch> CategoryAttributeMarketPlaceMatches { get; set; }
    public virtual DbSet<CategoryAttributeValueMarketPlaceMatch> CategoryAttributeValueMarketPlaceMatches { get; set; }
    public virtual DbSet<BrandMarketPlaceMatch> BrandMarketPlaceMatches { get; set; }
    public virtual DbSet<CargoCompanyMarketPlaceMatch> CargoCompanyMarketPlaceMatches { get; set; }
    public virtual DbSet<AttributeKeyValue> AttributeKeyValues { get; set; }
    public virtual DbSet<ApplicationUser> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<ApplicationClaim> Claims { get; set; }
    public virtual DbSet<Login> Logins { get; set; }

    //public DbSet<UsersRoles> UsersRoles { get; set; }
    //public DbSet<UsersClaims> UsersClaims { get; set; }
    //public DbSet<RolesClaims> RolesClaims { get; set; }

}