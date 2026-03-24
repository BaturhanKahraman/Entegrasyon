using Entegrasyon.Entity.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ShipmentTrackingEntityConfiguration : IEntityTypeConfiguration<ShipmentTracking>
{
    public void Configure(EntityTypeBuilder<ShipmentTracking> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.TrackingNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(200);
        builder.Property(x => x.RecipientAddress).HasMaxLength(500);

        builder.HasIndex(x => x.TrackingNumber);
        builder.HasIndex(x => x.OrderId);

        builder.HasOne(x => x.CargoCompany)
            .WithMany()
            .HasForeignKey(x => x.CargoCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.StatusHistory)
            .WithOne(x => x.ShipmentTracking)
            .HasForeignKey(x => x.ShipmentTrackingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
