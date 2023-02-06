using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.IdentityModel.Tokens;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductEntityConfiguration:IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasMany(x => x.ProductVariants).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
        builder.HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId);
        builder.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId);
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Season).HasMaxLength(55);
        builder.Property(x => x.Year).HasMaxLength(55);


        builder
            .HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "english",
                p => new { p.Title,p.Description,p.StockCode })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
        Product[] products = new Product[1];
        products[0] = new Product
        {
            Id = new Guid("84EE61DB-2275-43F4-B44E-5D54EFE4B6C5"),
            Title = "Product 1",
            Description = "Product 1 Description",
            StockCode = "Product 1 Stock Code",
            CreatedAt = DateTimeOffset.MinValue,
            IsDeleted = false,
            CategoryId = 1,
            
        };
        builder.HasData(products);
    }
}