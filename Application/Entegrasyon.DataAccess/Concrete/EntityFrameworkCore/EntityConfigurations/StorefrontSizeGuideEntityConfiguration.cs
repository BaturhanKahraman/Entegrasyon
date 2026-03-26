using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontSizeGuideEntityConfiguration : IEntityTypeConfiguration<StorefrontSizeGuide>
{
    public void Configure(EntityTypeBuilder<StorefrontSizeGuide> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SizeData).IsRequired();
        builder.Property(x => x.CategoryIds).HasMaxLength(2000);
        builder.Property(x => x.MeasurementImageUrl).HasMaxLength(1000);
        builder.Property(x => x.MeasurementInstructions).HasMaxLength(5000);
    }
}
