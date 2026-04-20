using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class DeadLetterOutboxEntityConfiguration : IEntityTypeConfiguration<DeadLetterOutbox>
{
    public void Configure(EntityTypeBuilder<DeadLetterOutbox> b)
    {
        b.ToTable("dead_letter_outbox");
        b.HasKey(x => x.Id);
        b.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.FinalError).HasColumnType("text").IsRequired();
        b.HasIndex(x => new { x.TenantId, x.MovedAt })
            .HasDatabaseName("ix_dead_letter_outbox_tenant_moved_at");
    }
}
