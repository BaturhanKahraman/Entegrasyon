using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontNewsletterEntityConfiguration : IEntityTypeConfiguration<StorefrontNewsletter>
{
    public void Configure(EntityTypeBuilder<StorefrontNewsletter> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

        builder.Property(x => x.Email).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Name).HasMaxLength(200);
    }
}
