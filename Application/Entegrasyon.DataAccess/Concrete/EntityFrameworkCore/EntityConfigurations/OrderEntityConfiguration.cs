
using Entegrasyon.Entity.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
        builder.OwnsOne(x => x.ShippingAddress);
        builder.OwnsOne(x => x.BillingAddress);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Property(x => x.TotalQuantity).HasDefaultValue(0);
        builder.Property(x => x.TotalPrice).HasDefaultValue(0m);

        // Hot-path: kategori performans / dashboard / unified-sales sorguları zaman penceresini
        // Orders.OrderDate üzerinden daraltır. OrderDate nullable olduğundan ve sorgular daima
        // !IsDeleted query filter'ı uyguladığından, NULL ve soft-deleted satırları index dışında
        // tutan partial index hem küçük hem seçici kalır.
        // Filter formu HasQueryFilter(!IsDeleted)'ın ürettiği SQL ile birebir aynı
        // ("NOT \"IsDeleted\"") tutulur — böylece PostgreSQL planner'ı partial index'i
        // her sürümde predicate-implication tereddütü olmadan seçer.
        builder.HasIndex(x => x.OrderDate)
            .HasFilter("\"OrderDate\" IS NOT NULL AND NOT \"IsDeleted\"");
    }
}
