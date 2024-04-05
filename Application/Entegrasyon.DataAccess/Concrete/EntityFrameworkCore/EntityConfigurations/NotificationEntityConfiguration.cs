using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class NotificationEntityConfiguration:IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Header).HasMaxLength(50);
        builder.Property(x => x.Content).HasMaxLength(400);
        builder.HasKey(x => x.Id);

        builder.HasMany(n => n.Users).WithMany(u => u.Notifications).UsingEntity<NotificationsUsers>();
        builder.HasMany(n => n.Claims).WithMany(c => c.Notifications).UsingEntity<NotificationsClaims>();
    }
}