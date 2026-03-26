using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontTwoFactorRecoveryCodeEntityConfiguration : IEntityTypeConfiguration<StorefrontTwoFactorRecoveryCode>
{
    public void Configure(EntityTypeBuilder<StorefrontTwoFactorRecoveryCode> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.AuthId);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(256);

        builder.HasOne(x => x.Auth)
            .WithMany(x => x.RecoveryCodes)
            .HasForeignKey(x => x.AuthId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
