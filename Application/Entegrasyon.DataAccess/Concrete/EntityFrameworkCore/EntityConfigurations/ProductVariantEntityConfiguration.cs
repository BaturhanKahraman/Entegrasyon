using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductVariantEntityConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Barcode).IsUnique();
        builder.Property(x => x.ProductVariantAttributes).HasColumnType("jsonb").HasConversion(
            x => JsonSerializer.Serialize(x,new JsonSerializerOptions { WriteIndented = true }),
            x => JsonSerializer.Deserialize<IEnumerable<ProductVariantAttribute>>(x,new JsonSerializerOptions { WriteIndented = true }));
        var pv = new ProductVariant()
        {
            Barcode = "0000000000001",
            Id = new Guid("32BFC865-B803-4945-9B1C-9E313A9C6398"),
            SalePrice = 100,
            ListPrice = 150,
            CostPrice = 50,
            ProductId = new Guid("84EE61DB-2275-43F4-B44E-5D54EFE4B6C5"),
            CreatedAt = DateTimeOffset.MinValue, IsDeleted = false
        };
        builder.HasData(pv);
    }
}