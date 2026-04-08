using Entegrasyon.Entity.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class WebhookSubscriptionEntityConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Secret).HasMaxLength(256);
        builder.Property(x => x.EventTypes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public class WebhookDeliveryLogEntityConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ResponseBody).HasMaxLength(2000);
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasOne(x => x.Subscription).WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
