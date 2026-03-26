using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontGiftCardEntityConfiguration : IEntityTypeConfiguration<StorefrontGiftCard>
{
    public void Configure(EntityTypeBuilder<StorefrontGiftCard> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.Property(x => x.Code).IsRequired().HasMaxLength(16);
        builder.Property(x => x.InitialAmount).HasColumnType("money");
        builder.Property(x => x.RemainingAmount).HasColumnType("money");
        builder.Property(x => x.RecipientEmail).HasMaxLength(200);
        builder.Property(x => x.RecipientName).HasMaxLength(200);
        builder.Property(x => x.SenderMessage).HasMaxLength(500);
    }
}
