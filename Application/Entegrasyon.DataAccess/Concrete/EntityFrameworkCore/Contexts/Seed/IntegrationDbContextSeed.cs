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
    }
}