using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontReferralEntityConfiguration : IEntityTypeConfiguration<StorefrontReferral>
{
    public void Configure(EntityTypeBuilder<StorefrontReferral> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.ReferralCode });
        builder.HasIndex(x => new { x.TenantId, x.ReferrerCustomerId });

        builder.Property(x => x.ReferralCode).IsRequired().HasMaxLength(8);
    }
}
