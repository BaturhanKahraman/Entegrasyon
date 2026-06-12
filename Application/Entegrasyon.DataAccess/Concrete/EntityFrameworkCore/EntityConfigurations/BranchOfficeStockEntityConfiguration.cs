using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BranchOfficeStockEntityConfiguration: IEntityTypeConfiguration<BranchOfficeStock>
{
    public void Configure(EntityTypeBuilder<BranchOfficeStock> builder)
    {
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();
        builder.HasKey(x => new { x.BranchOfficeId, ProductKindId = x.ProductVariantId });
        builder.Property(x => x.FirstTotalStock).IsRequired();
        builder.Property(x => x.CurrentStock)
            .HasComputedColumnSql(@"""FirstTotalStock""-""SoldQuantity""",stored:true);

        // Stok-alert hot-path: WHERE CurrentStock <= threshold (+ opsiyonel BranchOfficeId)
        // ORDER BY CurrentStock + Skip/Take pagination. CurrentStock STORED computed → indexlenebilir.
        // Branch-filtreli sorguda (çok-şubeli esnaf ana senaryo) equality(BranchOfficeId)+range(CurrentStock)
        // tek index ile karşılanır; indexli OrderBy pagination'ı seq-scan/sort'tan kurtarır.
        builder.HasIndex(x => new { x.BranchOfficeId, x.CurrentStock })
            .HasDatabaseName("IX_BranchOfficeStocks_BranchOfficeId_CurrentStock");
        //.HasComputedColumnSql(@"""Name"" || ' ' || ""Surname""",stored: true);
        //var bof = new List<BranchOfficeStock>()
        //{
        //    new()
        //    {

        //        FirstTotalStock = 5,
        //        SoldQuantity = 0,
        //        BranchOfficeId = 1,
        //        ProductVariantId = new Guid("32BFC865-B803-4945-9B1C-9E313A9C6398")
        //    }
        //};
        //builder.HasData(bof);
    }
}