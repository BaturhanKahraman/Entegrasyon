using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SaleReturnItemEntityConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.SaleItem)
            .WithMany()
            .HasForeignKey(x => x.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
