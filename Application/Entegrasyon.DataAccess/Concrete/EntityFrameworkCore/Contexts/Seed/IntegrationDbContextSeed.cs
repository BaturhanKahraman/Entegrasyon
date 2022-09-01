using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Users;
using Microsoft.EntityFrameworkCore;
using Shared.User;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts.Seed;

public static class IntegrationDbContextSeed
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationClaim>().HasData(new List<ApplicationClaim>()
        {
            new ApplicationClaim()
            {
                Id = 1,
                Name = "Product.Add",
                Description = "Ürün ekleme yetkisi."
            },
            new ApplicationClaim()
            {
                Id = 2,

                Name = "Product.Delete",
                Description = "Ürün silme yetkisi."
            },
            new ApplicationClaim()
            {
                Id = 3,

                Name = "Product.Update",
                Description = "Ürün güncelleme yetkisi."
            },
            new ApplicationClaim()
            {Id = 4,
                Name = "Product.List",
                Description = "Ürün listeleme yetkisi."
            },
            new ApplicationClaim()
            {Id = 5,
                Name = "Sale.Add",
                Description = "Satış yapma yetkisi."
            },
            new ApplicationClaim()
            {Id = 6,
                Name = "Sale.Update",
                Description = "Satış düzenleme yetkisi."
            },
            new ApplicationClaim()
            {Id = 7,
                Name = "BranchOffice.Add",
                Description = "Şube ekleme yetkisi."
            },
            new ApplicationClaim()
            {   Id = 8,
                Name = "BranchOffice.Update",
                Description = "Şube düzenleme yetkisi."
            },
            new ApplicationClaim()
            {
                Id = 9,
                Name = "BranchOffice.Delete",
                Description = "Şube silme yetkisi."
            }
        });
        modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole()
        {
            Id=1,
            Name = "Admin"
        });
        modelBuilder.Entity<BranchOffice>().HasData(new BranchOffice()
        {
            Id=1,
            Name = "Merkez Ofis"
        });
        modelBuilder.Entity<ApplicationUser>().HasData(new ApplicationUser()
        {
            Id = new Guid("DFDA5D4A-F807-408C-9B4D-908830AD5724"),
            Name = "Admin",
            Surname = "Admin",
            Email = "admin@entegrasyon.com",
            NeedsTakeNewPassword = true,
            TemporaryPassword = "Admin",
            DefaultBranchOfficeId = 1
        });
        modelBuilder.Entity<Brand>().HasData(new Brand()
        {
            Id = 1,
            Name = "FirstBrand"
        });
        modelBuilder.Entity<Category>().HasData(new Category()
        {
            Id = 1,
            Name = "supCategory"
        },
            new Category()
        {
            Id = 2,
            Name = "subCategory"
        });
        modelBuilder.Entity<MainProduct>().HasData(new MainProduct()
        {
            Id=1,
            BrandId = 1,
            Header = "Ürün Başlığı",
            Barcode = "123456798",
            CategoryId = 1,
        });
        modelBuilder.Entity<ProductVariant>().HasData(new List<ProductVariant>()
        {
            new ProductVariant()
            {Id = 1,
                Description = "Açıklama",
                BranchOfficeId = 1,
                CurrencyType = "₺",
                CurrentStockQuantity = 50,
                ListPrice = 50,
                VatRate = 8,
                StockCode = "22qwe123456",
                Title = "Başlık",
                ProductMainId = 1,
                SalePrice = 54,
                
            },new ProductVariant()
            {Id = 2,
                Description = "Açıklama 2",
                BranchOfficeId = 1,
                CurrencyType = "₺",
                CurrentStockQuantity = 30,
                ListPrice = 50,
                VatRate = 8,
                StockCode = "22qwe123456",
                Title = "Başlık 2",
                ProductMainId = 1,
                SalePrice = 54
            },
        });
        modelBuilder.Entity<AttributeKeyValue>().HasData(new()
                {Id=1, CategoryAttributeKey = 3, CategoryAttributeValue = 4 },
            new() { Id = 2,CategoryAttributeKey = 4, CategoryAttributeValue = 66 },
            new() { Id = 3,CategoryAttributeKey = 1, CategoryAttributeValue = 2 },
            new() { Id = 4,CategoryAttributeKey = 7, CategoryAttributeValue = 9 });


    }
}