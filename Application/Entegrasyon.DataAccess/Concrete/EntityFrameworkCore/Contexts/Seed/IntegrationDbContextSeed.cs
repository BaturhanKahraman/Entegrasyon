using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Shared.User;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts.Seed;

public static class IntegrationDbContextSeed
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
       UserContextSeed.SeedDatabase(modelBuilder);
        modelBuilder.Entity<ApplicationUser>().HasData(new ApplicationUser()
        {
            Id = new Guid("DFDA5D4A-F807-408C-9B4D-908830AD5724"),
            Name = "Admin",
            Surname = "Admin",
            UserName = "Admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@admin.com",
            NeedsTakeNewPassword = true,
            TemporaryPassword = "Admin",
            DefaultBranchOfficeId = 1,
            CreatedAt = DateTimeOffset.MinValue,
            RoleId = 1
        });
        //modelBuilder.Entity<AttributeKeyValue>().HasData(new AttributeKeyValue { Id = 1,CategoryAttributeKey = 3,CategoryAttributeValue = 4 },
        //    new AttributeKeyValue { Id = 2,CategoryAttributeKey = 4,CategoryAttributeValue = 66 },
        //    new AttributeKeyValue { Id = 3,CategoryAttributeKey = 1,CategoryAttributeValue = 2 },
        //    new AttributeKeyValue { Id = 4,CategoryAttributeKey = 7,CategoryAttributeValue = 9 });


    }
}