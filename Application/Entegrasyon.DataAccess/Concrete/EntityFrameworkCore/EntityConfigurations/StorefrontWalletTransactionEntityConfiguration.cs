using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontWalletTransactionEntityConfiguration : IEntityTypeConfiguration<StorefrontWalletTransaction>
{
    public void Configure(EntityTypeBuilder<StorefrontWalletTransaction> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.WalletId);

        builder.Property(x => x.Amount).HasColumnType("money");
        builder.Property(x => x.BalanceBefore).HasColumnType("money");
        builder.Property(x => x.BalanceAfter).HasColumnType("money");
        builder.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ReferenceId).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}
