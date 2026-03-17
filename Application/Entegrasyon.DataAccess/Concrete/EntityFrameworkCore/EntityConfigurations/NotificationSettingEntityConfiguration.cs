using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class NotificationSettingEntityConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.SnackbarDurationSeconds).HasDefaultValue(5);
        builder.Property(x => x.ShowSnackbar).HasDefaultValue(true);
        builder.Property(x => x.EnableOrderNotifications).HasDefaultValue(true);
        builder.Property(x => x.EnableStockAlerts).HasDefaultValue(true);
        builder.Property(x => x.EnableMarketplaceSyncNotifications).HasDefaultValue(true);
    }
}
