using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontLoyaltyTransactionEntityConfiguration : IEntityTypeConfiguration<StorefrontLoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<StorefrontLoyaltyTransaction> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });

        builder.Property(x => x.TransactionType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ReferenceId).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}
