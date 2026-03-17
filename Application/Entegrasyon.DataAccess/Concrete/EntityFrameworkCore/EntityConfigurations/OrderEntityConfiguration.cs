
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
    }
}
