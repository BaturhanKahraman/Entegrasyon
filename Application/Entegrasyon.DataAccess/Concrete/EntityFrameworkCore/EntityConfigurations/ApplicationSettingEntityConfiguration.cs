using Entegrasyon.Entity.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ApplicationSettingEntityConfiguration : IEntityTypeConfiguration<ApplicationSetting>
{
    public void Configure(EntityTypeBuilder<ApplicationSetting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Value).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Group).HasMaxLength(100);
        builder.Property(x => x.ValueType).HasConversion<string>().HasMaxLength(20);

        builder.HasData(
            new ApplicationSetting { Id = 19, Key = "SmtpHost", Value = "", Description = "SMTP sunucu adresi (ör: smtp.gmail.com)", Group = "E-posta Ayarları", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 20, Key = "SmtpPort", Value = "587", Description = "SMTP port numarası", Group = "E-posta Ayarları", ValueType = SettingValueType.Integer, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 21, Key = "SmtpUsername", Value = "", Description = "SMTP kullanıcı adı", Group = "E-posta Ayarları", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 22, Key = "SmtpPassword", Value = "", Description = "SMTP şifresi", Group = "E-posta Ayarları", ValueType = SettingValueType.Password, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 23, Key = "SmtpFromAddress", Value = "", Description = "Gönderen e-posta adresi", Group = "E-posta Ayarları", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 24, Key = "SmtpFromDisplayName", Value = "", Description = "Gönderen görünen adı", Group = "E-posta Ayarları", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 25, Key = "SmtpEnableSsl", Value = "true", Description = "SSL/TLS bağlantısı kullan", Group = "E-posta Ayarları", ValueType = SettingValueType.Boolean, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 26, Key = "SmtpInvoiceEmailEnabled", Value = "false", Description = "Fatura e-postaları gönderilsin mi?", Group = "E-posta Ayarları", ValueType = SettingValueType.Boolean, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 27, Key = "SmtpNotificationEmailEnabled", Value = "false", Description = "Bildirim e-postaları gönderilsin mi?", Group = "E-posta Ayarları", ValueType = SettingValueType.Boolean, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 28, Key = "ThemeMode", Value = "system", Description = "Tema modu (system, light, dark)", Group = "Görünüm", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue }
        );
    }
}
