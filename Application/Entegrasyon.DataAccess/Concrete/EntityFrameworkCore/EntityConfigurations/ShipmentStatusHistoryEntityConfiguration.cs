using Entegrasyon.Entity.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ShipmentStatusHistoryEntityConfiguration : IEntityTypeConfiguration<ShipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.StatusDescription).HasMaxLength(500);
        builder.Property(x => x.Location).HasMaxLength(200);

        builder.HasIndex(x => x.ShipmentTrackingId);
    }
}
