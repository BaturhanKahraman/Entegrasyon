using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Desktop.Data;

/// <summary>
/// SQLite DbContext for offline data storage.
/// Uses EnsureCreated() — no migration history needed for local cache DB.
/// </summary>
public class OfflineDbContext(DbContextOptions<OfflineDbContext> options) : DbContext(options)
{
    public DbSet<OfflineProduct> Products => Set<OfflineProduct>();
    public DbSet<OfflineSale> Sales => Set<OfflineSale>();
    public DbSet<OfflineSaleItem> SaleItems => Set<OfflineSaleItem>();
    public DbSet<SyncQueue> SyncQueue => Set<SyncQueue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // OfflineProduct
        modelBuilder.Entity<OfflineProduct>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            entity.HasIndex(e => e.Barcode);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ListPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.VatRate).HasColumnType("decimal(5,2)");
        });

        // OfflineSale
        modelBuilder.Entity<OfflineSale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IsSynced);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.CustomerInfo).HasMaxLength(500);

            entity.HasMany(e => e.Items)
                  .WithOne(e => e.Sale)
                  .HasForeignKey(e => e.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OfflineSaleItem
        modelBuilder.Entity<OfflineSaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(500);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.VatRate).HasColumnType("decimal(5,2)");
        });

        // SyncQueue
        modelBuilder.Entity<SyncQueue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IsSynced, e.CreatedAt });
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
        });
    }
}
