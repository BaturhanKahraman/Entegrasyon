using MainDatabase.MainEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MainDatabase.Context;

public class HeadDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    public HeadDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.UseSqlite("Data Source=Database/MainDatabase.db");
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("Main"));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CompanyName);
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<MarketPlace> MarketPlaces { get; set; }
    public DbSet<MembershipPlan> Memberships { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<MarketPlaceSetting> MarketPlaceSettings { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<ApplicationProperty> ApplicationProperties { get; set; }
    public DbSet<ConnectionInfo> ConnectionInfos { get; set; }
}