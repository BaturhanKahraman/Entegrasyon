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
                Name = "Product.Add",
                Description = "Ürün ekleme yetkisi."
            },
            new()
            {
                Id = 2,

                Name = "Product.Delete",
                Description = "Ürün silme yetkisi."
            },
            new()
            {
                Id = 3,

                Name = "Product.Update",
                Description = "Ürün güncelleme yetkisi."
            },
            new()
            {Id = 4,
                Name = "Product.List",
                Description = "Ürün listeleme yetkisi."
            },
            new()
            {Id = 5,
                Name = "Sale.Add",
                Description = "Satış yapma yetkisi."
            },
            new()
            {Id = 6,
                Name = "Sale.Update",
                Description = "Satış düzenleme yetkisi."
            },
            new()
            {Id = 7,
                Name = "BranchOffice.Add",
                Description = "Şube ekleme yetkisi."
            },
            new()
            {   Id = 8,
                Name = "BranchOffice.Update",
                Description = "Şube düzenleme yetkisi."
            },
            new()
            {
                Id = 9,
                Name = "BranchOffice.Delete",
                Description = "Şube silme yetkisi."
            }
        });
        modelBuilder.Entity<RootRole>().HasData(new RootRole()
        {
            Id = 1,
            Name = "Admin"
        });
       
    }
}