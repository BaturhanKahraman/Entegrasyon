using Entegrasyon.Entity.Settings;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts.Seed;

public static class IntegrationDbContextSeed
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            NormalizedName = "ADMIN",
            CreatedAt = DateTimeOffset.MinValue,
        };
        modelBuilder.Entity<Role>().HasData(role);

        modelBuilder.Entity<ApplicationUser>().Property(x => x.FullName)
           .HasComputedColumnSql(@"""Name"" || ' ' || ""Surname""", stored: true);
        modelBuilder.Entity<ApplicationUser>().HasData(new ApplicationUser
        {
            Id = new Guid("DFDA5D4A-F807-408C-9B4D-908830AD5724"),
            Name = "Admin",
            Surname = "Admin",
            UserName = "Admin",
            NormalizedUserName = "Admin".ToUpperInvariant(),
            Email = "admin@admin.com",
            NormalizedEmail = "ADMIN@ADMIN.COM",
            NeedsTakeNewPassword = true,
            TemporaryPassword = "Admin",
            DefaultBranchOfficeId = 1,
            CreatedAt = DateTimeOffset.MinValue,
        });
        modelBuilder.Entity<UsersRoles>().HasData([
            new UsersRoles
            {
                ApplicationUserId = new Guid("DFDA5D4A-F807-408C-9B4D-908830AD5724"),
                RoleId = 1,
            }
        ]);

        modelBuilder.Entity<ApplicationSetting>().HasData(
            new ApplicationSetting { Id = 1, Key = "CompanyName", Value = "", Description = "Firma adı", Group = "Firma Bilgileri", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 2, Key = "CompanyEmail", Value = "", Description = "Firma e-posta adresi", Group = "Firma Bilgileri", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 3, Key = "CompanyPhone", Value = "", Description = "Firma telefon numarası", Group = "Firma Bilgileri", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 4, Key = "CompanyAddress", Value = "", Description = "Firma adresi", Group = "Firma Bilgileri", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 5, Key = "TaxNumber", Value = "", Description = "Vergi numarası", Group = "Firma Bilgileri", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 6, Key = "Currency", Value = "TRY", Description = "Para birimi", Group = "Sistem", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 7, Key = "TaxRate", Value = "20", Description = "Varsayılan vergi oranı (%)", Group = "Sistem", ValueType = SettingValueType.Decimal, CreatedAt = DateTimeOffset.MinValue },
            new ApplicationSetting { Id = 8, Key = "DefaultLanguage", Value = "tr", Description = "Varsayılan dil", Group = "Sistem", ValueType = SettingValueType.String, CreatedAt = DateTimeOffset.MinValue }
        );
    }
}