using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductVariantEntityConfiguration:IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(x => x.CurrentStockQuantity)
            .HasComputedColumnSql("(\"Quantity\")-(\"SoldQuantity\")",true);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}