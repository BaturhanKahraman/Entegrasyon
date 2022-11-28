using MainDatabase.MainEntities;
using Microsoft.EntityFrameworkCore;

namespace MainDatabase.Context;

public class HeadDbContext : DbContext
{
    public HeadDbContext(DbContextOptions<HeadDbContext> opt):base(opt)
    {
        
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