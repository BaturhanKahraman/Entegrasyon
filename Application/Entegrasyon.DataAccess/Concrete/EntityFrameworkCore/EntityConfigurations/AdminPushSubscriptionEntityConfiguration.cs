using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class AdminPushSubscriptionEntityConfiguration : IEntityTypeConfiguration<AdminPushSubscription>
{
    public void Configure(EntityTypeBuilder<AdminPushSubscription> b)
    {
        b.ToTable("admin_push_subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Endpoint).HasMaxLength(2000).IsRequired();
        b.Property(x => x.P256dhKey).HasMaxLength(500).IsRequired();
        b.Property(x => x.AuthKey).HasMaxLength(500).IsRequired();
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.HasIndex(x => new { x.UserId, x.Endpoint }).IsUnique()
            .HasDatabaseName("ux_admin_push_subscriptions_user_endpoint");
    }
}
