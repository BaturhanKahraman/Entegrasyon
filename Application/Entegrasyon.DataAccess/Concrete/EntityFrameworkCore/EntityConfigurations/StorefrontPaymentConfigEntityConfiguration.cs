using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontPaymentConfigEntityConfiguration : IEntityTypeConfiguration<StorefrontPaymentConfig>
{
    public void Configure(EntityTypeBuilder<StorefrontPaymentConfig> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.PaymentProvider }).IsUnique();

        builder.Property(x => x.PaymentProvider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ApiKey).HasMaxLength(200);
        builder.Property(x => x.SecretKey).HasMaxLength(200);
        builder.Property(x => x.BaseUrl).HasMaxLength(500);
        builder.Property(x => x.MinInstallmentAmount).HasPrecision(18, 2);
    }
}
