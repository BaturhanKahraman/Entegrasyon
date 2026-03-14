using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductEntityConfiguration:IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.UseXminAsConcurrencyToken();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasMany(x => x.ProductVariants).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
        builder.HasOne(x => x.Brand)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

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
    }
}