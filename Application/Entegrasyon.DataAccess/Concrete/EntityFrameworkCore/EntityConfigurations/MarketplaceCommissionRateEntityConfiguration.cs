using Entegrasyon.Entity.Marketplace;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class MarketplaceCommissionRateEntityConfiguration : IEntityTypeConfiguration<MarketplaceCommissionRate>
{
    public void Configure(EntityTypeBuilder<MarketplaceCommissionRate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MarketPlaceId, x.CategoryId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(x => x.CommissionPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.ServiceFeePercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.TransactionFeeFixed)
            .HasPrecision(10, 2);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasOne(x => x.MarketPlace)
            .WithMany()
            .HasForeignKey(x => x.MarketPlaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
