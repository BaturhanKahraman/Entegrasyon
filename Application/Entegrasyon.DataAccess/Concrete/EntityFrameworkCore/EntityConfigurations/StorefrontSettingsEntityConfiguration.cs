using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontSettingsEntityConfiguration : IEntityTypeConfiguration<StorefrontSettings>
{
    public void Configure(EntityTypeBuilder<StorefrontSettings> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.TenantId).IsUnique();

        builder.Property(x => x.StoreName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CompanyTaxOffice).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CompanyTaxNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ContactPhone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
        builder.Property(x => x.City).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PrimaryColor).IsRequired().HasMaxLength(10);

        builder.Property(x => x.FreeShippingThreshold).HasPrecision(18, 2);
        builder.Property(x => x.FlatShippingRate).HasPrecision(18, 2);
    }
}
