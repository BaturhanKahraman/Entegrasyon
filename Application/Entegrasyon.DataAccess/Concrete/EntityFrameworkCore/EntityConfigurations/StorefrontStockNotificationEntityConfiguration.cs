using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontStockNotificationEntityConfiguration : IEntityTypeConfiguration<StorefrontStockNotification>
{
    public void Configure(EntityTypeBuilder<StorefrontStockNotification> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId });
        builder.HasIndex(x => new { x.TenantId, x.ProductVariantId, x.Email }).IsUnique();

        builder.Property(x => x.Email).IsRequired().HasMaxLength(300);
    }
}
