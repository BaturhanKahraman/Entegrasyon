using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BranchOfficeStockEntityConfiguration: IEntityTypeConfiguration<BranchOfficeStock>
{
    public void Configure(EntityTypeBuilder<BranchOfficeStock> builder)
    {
        builder.HasKey(x => new { x.BranchOfficeId, ProductKindId = x.ProductVariantId });
        builder.Property(x => x.FirstTotalStock).IsRequired();
        builder.Property(x => x.CurrentStock)
            .HasComputedColumnSql(@"""FirstTotalStock""-""SoldQuantity""",stored:true);
        //.HasComputedColumnSql(@"""Name"" || ' ' || ""Surname""",stored: true);
        var bof = new List<BranchOfficeStock>()
        {
            new()
            {

                FirstTotalStock = 5,
                SoldQuantity = 0,
                BranchOfficeId = 2,
                ProductVariantId = new Guid("32BFC865-B803-4945-9B1C-9E313A9C6398")
            }
        };
        builder.HasData(bof);
    }
}