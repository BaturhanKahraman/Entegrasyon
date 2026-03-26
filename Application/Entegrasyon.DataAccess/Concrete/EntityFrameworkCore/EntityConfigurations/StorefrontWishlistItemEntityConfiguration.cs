using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontWishlistItemEntityConfiguration : IEntityTypeConfiguration<StorefrontWishlistItem>
{
    public void Configure(EntityTypeBuilder<StorefrontWishlistItem> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.CustomerId, x.ProductId }).IsUnique();
    }
}
