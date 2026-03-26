using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontContactMessageEntityConfiguration : IEntityTypeConfiguration<StorefrontContactMessage>
{
    public void Configure(EntityTypeBuilder<StorefrontContactMessage> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Subject).HasMaxLength(300);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(5000);
    }
}
