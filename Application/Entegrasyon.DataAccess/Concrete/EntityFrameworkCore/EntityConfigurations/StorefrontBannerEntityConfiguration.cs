using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontBannerEntityConfiguration : IEntityTypeConfiguration<StorefrontBanner>
{
    public void Configure(EntityTypeBuilder<StorefrontBanner> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Position, x.IsActive });

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
    }
}
