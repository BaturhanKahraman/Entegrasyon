using Microsoft.EntityFrameworkCore;

namespace Shared.User;

public static class UserContextSeed
{
    public static void SeedDatabase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RootClaim>().HasData(new List<RootClaim>()
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
        modelBuilder.Entity<RootRole>().HasData(new RootRole()
        {
            Id = 1,
            Name = "Admin",
            CreatedAt = DateTimeOffset.MinValue,
            
        });
        modelBuilder.Entity("RootClaimRootRole").HasData(
            new { ClaimsId=1,RolesId=1 },
            new { ClaimsId=2,RolesId=1 },
            new { ClaimsId=3,RolesId=1 },
            new { ClaimsId=4,RolesId=1 },
            new { ClaimsId=5,RolesId=1 },
            new { ClaimsId=6,RolesId=1 },
            new { ClaimsId=7,RolesId=1 },
            new { ClaimsId=8,RolesId=1 },
            new { ClaimsId=9,RolesId=1 },
            new { ClaimsId=10,RolesId=1 },
            new { ClaimsId=11,RolesId=1 },
            new { ClaimsId=12,RolesId=1 },
            new { ClaimsId=13,RolesId=1 },
            new { ClaimsId=14,RolesId=1 },
            new { ClaimsId=15,RolesId=1 },
            new { ClaimsId=16,RolesId=1 },
            new { ClaimsId=17,RolesId=1 },
            new { ClaimsId=18,RolesId=1 },
            new { ClaimsId=19,RolesId=1 },
            new { ClaimsId=20,RolesId=1 },
            new { ClaimsId=21,RolesId=1 },
            new { ClaimsId=22,RolesId=1 },
            new { ClaimsId=23,RolesId=1 },
            new { ClaimsId=24,RolesId=1 },
            new { ClaimsId=25,RolesId=1 },
            new { ClaimsId=26,RolesId=1 },
            new { ClaimsId=27,RolesId=1 },
            //new { ClaimsId=28,RolesId=1 },
            //new { ClaimsId=29,RolesId=1 },
            new { ClaimsId=30,RolesId=1 },
            new { ClaimsId=31,RolesId=1 },
            new { ClaimsId=32,RolesId=1 },
            new { ClaimsId=33,RolesId=1 },
            new { ClaimsId=34,RolesId=1 },
            new { ClaimsId=35,RolesId=1 },
            new { ClaimsId=36,RolesId=1 },
            new { ClaimsId=37,RolesId=1 },
            new { ClaimsId=38,RolesId=1 },
            new { ClaimsId=39,RolesId=1 },
            new { ClaimsId=40,RolesId=1 },
            new { ClaimsId=41,RolesId=1 },
            new { ClaimsId = 42,RolesId = 1 },
            new { ClaimsId = 43,RolesId = 1 },
            new { ClaimsId = 44,RolesId = 1 },
            new { ClaimsId = 45,RolesId = 1 },
            new { ClaimsId = 46,RolesId = 1 },
            new { ClaimsId = 47,RolesId = 1 },
            new { ClaimsId = 48,RolesId = 1 },
            new { ClaimsId = 49,RolesId = 1 }
        );
       
    }
}