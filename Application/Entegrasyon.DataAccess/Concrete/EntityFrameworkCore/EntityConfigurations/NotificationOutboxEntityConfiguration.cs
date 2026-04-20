using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class NotificationOutboxEntityConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> b)
    {
        b.ToTable("notification_outbox");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.LastError).HasMaxLength(4000);
        b.HasIndex(x => new { x.Status, x.NextRetryAt })
            .HasDatabaseName("ix_notification_outbox_status_next_retry_at");
        b.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .HasDatabaseName("ix_notification_outbox_tenant_created_at");
    }
}
