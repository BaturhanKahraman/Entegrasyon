using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SellerBalanceEntityConfiguration : IEntityTypeConfiguration<SellerBalance>
{
    public void Configure(EntityTypeBuilder<SellerBalance> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.SellerId).IsUnique();

        builder.Property(x => x.TotalEarned).HasPrecision(18, 2);
        builder.Property(x => x.TotalPaidOut).HasPrecision(18, 2);
        builder.Property(x => x.PendingAmount).HasPrecision(18, 2);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);

        builder.HasOne(x => x.Seller)
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
