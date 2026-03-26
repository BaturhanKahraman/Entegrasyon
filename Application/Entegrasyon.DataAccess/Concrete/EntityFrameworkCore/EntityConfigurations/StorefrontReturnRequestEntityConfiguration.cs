using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontReturnRequestEntityConfiguration : IEntityTypeConfiguration<StorefrontReturnRequest>
{
    public void Configure(EntityTypeBuilder<StorefrontReturnRequest> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });
        builder.HasIndex(x => new { x.TenantId, x.OrderId });

        builder.Property(x => x.Reason).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.ReviewNote).HasMaxLength(2000);
        builder.Property(x => x.RefundAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
    }
}
