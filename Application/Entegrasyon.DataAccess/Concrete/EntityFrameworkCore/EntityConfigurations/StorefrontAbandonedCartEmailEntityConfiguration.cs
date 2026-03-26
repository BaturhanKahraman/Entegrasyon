using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontAbandonedCartEmailEntityConfiguration : IEntityTypeConfiguration<StorefrontAbandonedCartEmail>
{
    public void Configure(EntityTypeBuilder<StorefrontAbandonedCartEmail> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.CartId, x.EmailStep }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.CouponCode).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    }
}
