using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontPopularSearchEntityConfiguration : IEntityTypeConfiguration<StorefrontPopularSearch>
{
    public void Configure(EntityTypeBuilder<StorefrontPopularSearch> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Query }).IsUnique();

        builder.Property(x => x.Query).IsRequired().HasMaxLength(500);
    }
}
