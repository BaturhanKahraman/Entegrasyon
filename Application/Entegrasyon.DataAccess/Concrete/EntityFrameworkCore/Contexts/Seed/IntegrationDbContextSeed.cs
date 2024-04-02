using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts.Seed;

public static class IntegrationDbContextSeed
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationClaim>().HasData(new List<ApplicationClaim>
        {
            new()
            {
                Id = 1,
                Name = "product.Add",
                Description = "Ürün ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 2,

                Name = "product.Delete",
                Description = "Ürün silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 3,

                Name = "product.Update",
                Description = "Ürün güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {Id = 4,
                Name = "product.List",
                Description = "Ürün listeleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {Id = 5,
                Name = "sale.Add",
                Description = "Satış yapma yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {Id = 6,
                Name = "sale.Update",
                Description = "Satış düzenleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {Id = 7,
                Name = "branchOffice.Add",
                Description = "Şube ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {   Id = 8,
                Name = "branchOffice.Update",
                Description = "Şube düzenleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 9,
                Name = "branchOffice.Delete",
                Description = "Şube silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 10,
                Name = "category.Add",
                Description = "Kategori silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 11,
                Name = "category.Update",
                Description = "Kategori güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 12,
                Name = "category.List",
                Description = "Kategori görebilme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 13,
                Name = "category.Delete",
                Description = "Kategori silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 14,
                Name = "branchOffice.List",
                Description = "Ofis görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 15,
                Name = "sale.List",
                Description = "Satış görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 16,
                Name = "sale.Delete",
                Description = "Satış silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },new()
            {
                Id = 17,
                Name = "customer.Add",
                Description = "Müşteri ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 18,
                Name = "customer.Update",
                Description = "Müşteri güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 19,
                Name = "customer.List",
                Description = "Müşteri görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 20,
                Name = "customer.Delete",
                Description = "Müşteri silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            }, new()
            {
                Id = 21,
                Name = "order.Add",
                Description = "Satış ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 22,
                Name = "order.Update",
                Description = "Satış güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 23,
                Name = "order.List",
                Description = "Satış görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 24,
                Name = "order.Delete",
                Description = "Satış silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            }, new()
            {
                Id = 25,
                Name = "report.Add",
                Description = "Rapor ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 26,
                Name = "report.Update",
                Description = "Rapor güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 27,
                Name = "report.List",
                Description = "Rapor görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 30,
                Name = "report.Delete",
                Description = "Rapor silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            }, new()
            {
                Id = 31,
                Name = "cargo.Add",
                Description = "Kargo ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 32,
                Name = "cargo.Update",
                Description = "Kargo güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 33,
                Name = "cargo.List",
                Description = "Kargo görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 34,
                Name = "cargo.Delete",
                Description = "Kargo silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
             new()
            {
                Id = 35,
                Name = "integration.Add",
                Description = "Entegrasyon ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 36,
                Name = "integration.Update",
                Description = "Entegrasyon güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 37,
                Name = "integration.List",
                Description = "Entegrasyon görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 38,
                Name = "integration.Delete",
                Description = "Entegrasyon silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },new()
            {
                Id = 39,
                Name = "user.Add",
                Description = "Kullanıcı ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 40,
                Name = "user.Update",
                Description = "Kullanıcı güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 41,
                Name = "user.List",
                Description = "Kullanıcı görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 42,
                Name = "user.Delete",
                Description = "Kullanıcı silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 43,
                Name = "log.List",
                Description = "Sistem kaydı görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 44,
                Name = "setting.Update",
                Description = "Ayar güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 45,
                Name = "setting.List",
                Description = "Ayar görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },new()
            {
                Id = 46,
                Name = "brand.Add",
                Description = "Marka ekleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 47,
                Name = "brand.Update",
                Description = "Marka güncelleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 48,
                Name = "brand.List",
                Description = "Marka görüntüleme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },
            new()
            {
                Id = 49,
                Name = "brand.Delete",
                Description = "Marka silme yetkisi.",CreatedAt = DateTimeOffset.MinValue
            },

        });
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            CreatedAt = DateTimeOffset.MinValue,
        };
        modelBuilder.Entity<Role>().HasData(role);
        modelBuilder.Entity<RolesClaims>()
            .HasData(
                new RolesClaims { ApplicationClaimId= 1,RoleId= 1 },
            new RolesClaims { ApplicationClaimId= 2, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 3, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 4, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 5, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 6, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 7, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 8, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 9, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 10, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 11, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 12, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 13, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 14, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 15, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 16, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 17, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 18, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 19, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 20, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 21, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 22, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 23, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 24, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 25, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 26, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 27, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 30, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 31, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 32, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 33, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 34, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 35, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 36, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 37, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 38, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 39, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 40, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 41, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 42, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 43, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 44, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 45, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 46, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 47, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 48, RoleId = 1 },
            new RolesClaims { ApplicationClaimId= 49, RoleId = 1 }
        );

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