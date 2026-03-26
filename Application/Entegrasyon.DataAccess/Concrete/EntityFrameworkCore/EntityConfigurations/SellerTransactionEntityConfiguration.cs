using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SellerTransactionEntityConfiguration : IEntityTypeConfiguration<SellerTransaction>
{
    public void Configure(EntityTypeBuilder<SellerTransaction> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.SellerId, x.CreatedAt });

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);
        builder.Property(x => x.TransactionType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasOne(x => x.Seller)
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
