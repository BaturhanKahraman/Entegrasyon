using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class DiscountReasonEntityConfiguration : IEntityTypeConfiguration<DiscountReason>
{
    public void Configure(EntityTypeBuilder<DiscountReason> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
