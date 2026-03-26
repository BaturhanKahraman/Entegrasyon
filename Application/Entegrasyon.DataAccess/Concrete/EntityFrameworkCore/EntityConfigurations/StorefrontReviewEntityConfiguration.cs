using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontReviewEntityConfiguration : IEntityTypeConfiguration<StorefrontReview>
{
    public void Configure(EntityTypeBuilder<StorefrontReview> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.ProductId });
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });

        builder.Property(x => x.Comment).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.ReplyText).HasMaxLength(2000);
    }
}
