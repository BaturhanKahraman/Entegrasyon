using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class MainProductEntityConfiguration:IEntityTypeConfiguration<MainProduct>
{
    public void Configure(EntityTypeBuilder<MainProduct> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasMany(x => x.ProductVariants).WithOne(x => x.ProductMain).HasForeignKey(x => x.ProductMainId);
        builder.HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId);
        builder.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId);
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        
        builder
            .HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "english",
                p => new { p.Title,p.Description,p.StockCode })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
    }
}