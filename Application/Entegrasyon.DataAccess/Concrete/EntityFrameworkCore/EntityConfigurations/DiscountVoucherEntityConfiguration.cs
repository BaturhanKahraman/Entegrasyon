using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations
{
    public class DiscountVoucherEntityConfiguration : IEntityTypeConfiguration<DiscountVoucher>
    {
        public void Configure(EntityTypeBuilder<DiscountVoucher> builder)
        {
            builder.Property(x => x.Code).UseCollation("CaseInsensitive");
        }
    }
}
