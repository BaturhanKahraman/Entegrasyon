using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Shared.User;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts.Seed;

public static class IntegrationDbContextSeed
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
       
        modelBuilder.Entity<BranchOffice>().HasData(new BranchOffice()
        {
            Id = 1,
            Name = "Merkez Ofis",
            CreatedAt=DateTimeOffset.MinValue
        });

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
        modelBuilder.Entity<Brand>().HasData(new Brand()
        {
            Id = 1,
            Name = "FirstBrand",
            CreatedAt = DateTimeOffset.MinValue
        });
        modelBuilder.Entity<Category>().HasData(new Category()
        {
            Id = 1,
            Name = "supCategory"
            ,
            CreatedAt = DateTimeOffset.MinValue
        },
        new Category()
        {
            Id = 2,
            Name = "subCategory",
            SuperCategoryId = 1,
            CreatedAt = DateTimeOffset.MinValue
        });
        modelBuilder.Entity<MainProduct>().HasData(new MainProduct()
        {
            Id = 1,
            BrandId = 1,
            Header = "Ürün Başlığı",
            Barcode = "123456798",
            CategoryId = 1,
            CreatedAt = DateTimeOffset.MinValue
        });
        modelBuilder.Entity<ProductVariant>().HasData(new List<ProductVariant>()
        {
            new()
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
                SalePrice = 54,CreatedAt = DateTimeOffset.MinValue

            },new()
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
                SalePrice = 54,CreatedAt = DateTimeOffset.MinValue
            },
        });
        //modelBuilder.Entity<AttributeKeyValue>().HasData(new AttributeKeyValue { Id = 1,CategoryAttributeKey = 3,CategoryAttributeValue = 4 },
        //    new AttributeKeyValue { Id = 2,CategoryAttributeKey = 4,CategoryAttributeValue = 66 },
        //    new AttributeKeyValue { Id = 3,CategoryAttributeKey = 1,CategoryAttributeValue = 2 },
        //    new AttributeKeyValue { Id = 4,CategoryAttributeKey = 7,CategoryAttributeValue = 9 });


    }
}