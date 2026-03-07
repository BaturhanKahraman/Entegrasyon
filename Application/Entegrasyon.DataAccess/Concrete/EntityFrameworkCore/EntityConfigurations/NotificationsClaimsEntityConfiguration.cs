using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class NotificationsClaimsEntityConfiguration : IEntityTypeConfiguration<NotificationsClaims>
{
    public void Configure(EntityTypeBuilder<NotificationsClaims> builder)
    {
        builder.ToTable("NotificationsClaims");
        builder.HasKey(x => new { x.NotificationId, x.ClaimId });

        builder.HasOne(x => x.Notification)
            .WithMany(n => n.NotificationClaims)
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
