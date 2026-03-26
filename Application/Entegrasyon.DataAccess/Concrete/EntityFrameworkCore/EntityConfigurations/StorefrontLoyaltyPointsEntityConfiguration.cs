using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontLoyaltyPointsEntityConfiguration : IEntityTypeConfiguration<StorefrontLoyaltyPoints>
{
    public void Configure(EntityTypeBuilder<StorefrontLoyaltyPoints> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.CustomerId }).IsUnique();
    }
}
