using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SellerCommissionEntityConfiguration : IEntityTypeConfiguration<SellerCommission>
{
    public void Configure(EntityTypeBuilder<SellerCommission> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.SellerId, x.CategoryId }).IsUnique();

        builder.Property(x => x.CommissionRate).HasPrecision(5, 2);

        builder.HasOne(x => x.Seller)
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
