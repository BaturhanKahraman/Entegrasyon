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

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.Seed();
        
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<CategoryAttribute> CategoryAttributes { get; set; }
    public DbSet<CategoryAttributeValue> CategoryAttributeValues { get; set; }
    public DbSet<CategoryMarketPlaceMatch> CategoryMarketPlaceMatches { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<MainProduct> MainProducts { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<ChangeProduct> ChangeProducts { get; set; }
    public DbSet<DiscountVoucher> DiscountVouchers { get; set; }
    public DbSet<ReturnProduct> ReturnProducts { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<BranchOffice> BranchOffices { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<MarketPlace> MarketPlaces { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ApplicationLog> Logs { get; set; }
    public DbSet<ApplicationCustomer> ApplicationCustomers { get; set; }

    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<RootRole> Roles { get; set; }
    public DbSet<RootClaim> Claims { get; set; }
    public DbSet<RootLogin> Logins { get; set; }




}