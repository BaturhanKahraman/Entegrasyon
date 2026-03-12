using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class NotificationEntityConfiguration:IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Header).HasMaxLength(200);
        builder.Property(x => x.Content).HasMaxLength(1000);
        builder.Property(x => x.ActionUrl).HasMaxLength(500);

        builder.HasMany(n => n.Users)
               .WithMany(u => u.Notifications)
               .UsingEntity<NotificationsUsers>();
    }
}
