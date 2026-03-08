using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductMarketplaceEntityConfiguration : IEntityTypeConfiguration<ProductMarketplace>
{
    public void Configure(EntityTypeBuilder<ProductMarketplace> builder)
    {
        builder.HasIndex(x => new { x.ProductId, x.MarketPlaceId }).IsUnique();
        builder.Property(x => x.BatchRequestId).HasMaxLength(200);
        builder.Property(x => x.ExternalProductId).HasMaxLength(200);
        builder.Property(x => x.StatusMessage).HasMaxLength(1000);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
