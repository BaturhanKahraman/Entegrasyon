using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class PaymentMethodDefinitionEntityConfiguration : IEntityTypeConfiguration<PaymentMethodDefinition>
{
    public void Configure(EntityTypeBuilder<PaymentMethodDefinition> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Property(x => x.CommissionRate).HasColumnType("numeric(5,2)");
        builder.HasIndex(x => new { x.TenantId, x.SystemCode }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
