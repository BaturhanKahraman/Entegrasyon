using Entegrasyon.Entity.Sales.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class UnifiedSaleViewConfiguration : IEntityTypeConfiguration<UnifiedSaleView>
{
    public void Configure(EntityTypeBuilder<UnifiedSaleView> builder)
    {
        builder.HasNoKey();
        builder.ToView("vw_unified_sales");
    }
}
