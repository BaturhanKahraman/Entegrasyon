using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontPushSubscriptionEntityConfiguration : IEntityTypeConfiguration<StorefrontPushSubscription>
{
    public void Configure(EntityTypeBuilder<StorefrontPushSubscription> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Endpoint).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });

        builder.Property(x => x.Endpoint).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.P256dhKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.AuthKey).IsRequired().HasMaxLength(500);
    }
}
