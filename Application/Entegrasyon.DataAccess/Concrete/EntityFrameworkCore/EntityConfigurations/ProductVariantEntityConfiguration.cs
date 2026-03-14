
using System.Text.Json;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductVariantEntityConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.UseXminAsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Barcode).IsUnique();
        //builder.OwnsMany(x => x.ProductVariantAttributes, navBuilder =>
        //{
        //    navBuilder.ToJson();
        //});
        //builder.Property(x => x.ProductVariantAttributes).HasColumnType("jsonb").HasConversion(
        //    x => JsonSerializer.Serialize(x,new JsonSerializerOptions { WriteIndented = true }),
        //    x => JsonSerializer.Deserialize<List<ProductVariantAttribute>>(x,new JsonSerializerOptions { WriteIndented = true }));
        builder.OwnsMany(x => x.ProductVariantAttributes, ownedNavigationBuilder =>
        {
            ownedNavigationBuilder.ToTable("ProductVariantAttributes");
        });
    }
}