using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SellerProductEntityConfiguration : IEntityTypeConfiguration<SellerProduct>
{
    public void Configure(EntityTypeBuilder<SellerProduct> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.SellerId, x.ProductId }).IsUnique();

        builder.Property(x => x.Price).HasPrecision(18, 2);

        builder.HasOne(x => x.Seller)
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
