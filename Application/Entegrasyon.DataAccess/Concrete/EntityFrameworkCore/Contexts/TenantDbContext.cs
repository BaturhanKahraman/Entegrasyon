using Entegrasyon.Entity.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;

public class TenantDbContext:DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().OwnsOne(x => x.ConnectionInfo);
        modelBuilder.Entity<Tenant>().HasIndex(x=>x.ConnectionString);
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<MainCustomer> MainCustomers { get; set; }
}