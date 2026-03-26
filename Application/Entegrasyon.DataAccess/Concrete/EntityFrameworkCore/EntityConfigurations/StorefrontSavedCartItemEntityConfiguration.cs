using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontSavedCartItemEntityConfiguration : IEntityTypeConfiguration<StorefrontSavedCartItem>
{
    public void Configure(EntityTypeBuilder<StorefrontSavedCartItem> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.CustomerId, x.ProductVariantId }).IsUnique();
    }
}
