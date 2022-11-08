using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BranchOfficeStockEntityConfiguration: IEntityTypeConfiguration<BranchOfficeStock>
{
    public void Configure(EntityTypeBuilder<BranchOfficeStock> builder)
    {
        builder.HasKey(x => new { x.BranchOfficeId,x.ProductVariantId });
        builder.Property(x => x.FirstTotalStock).IsRequired();
        builder.Property(x => x.CurrentStock)
            .HasComputedColumnSql(@"""FirstTotalStock""-""SoldQuantity""",stored:true);
        //.HasComputedColumnSql(@"""Name"" || ' ' || ""Surname""",stored: true);
    }
}