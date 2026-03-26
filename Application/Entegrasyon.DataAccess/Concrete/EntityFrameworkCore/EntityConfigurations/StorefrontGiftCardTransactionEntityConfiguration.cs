using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontGiftCardTransactionEntityConfiguration : IEntityTypeConfiguration<StorefrontGiftCardTransaction>
{
    public void Configure(EntityTypeBuilder<StorefrontGiftCardTransaction> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.GiftCardId);
        builder.HasIndex(x => x.OrderId);

        builder.Property(x => x.Amount).HasColumnType("money");
        builder.Property(x => x.BalanceBefore).HasColumnType("money");
        builder.Property(x => x.BalanceAfter).HasColumnType("money");
        builder.Property(x => x.TransactionType).IsRequired().HasMaxLength(50);

        builder.HasOne(x => x.GiftCard)
            .WithMany()
            .HasForeignKey(x => x.GiftCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
