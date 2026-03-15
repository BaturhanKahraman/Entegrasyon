using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductVariantMarketplaceOverrideEntityConfiguration : IEntityTypeConfiguration<ProductVariantMarketplaceOverride>
{
    public void Configure(EntityTypeBuilder<ProductVariantMarketplaceOverride> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProductMarketplaceId, x.ProductVariantId }).IsUnique();
        builder.Property(x => x.ListPriceOverride).HasColumnType("money");
        builder.Property(x => x.SalePriceOverride).HasColumnType("money");
        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
