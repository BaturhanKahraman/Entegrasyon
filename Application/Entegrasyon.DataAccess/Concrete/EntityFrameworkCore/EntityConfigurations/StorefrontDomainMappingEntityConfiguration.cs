using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontDomainMappingEntityConfiguration : IEntityTypeConfiguration<StorefrontDomainMapping>
{
    public void Configure(EntityTypeBuilder<StorefrontDomainMapping> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.DomainName).IsUnique();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.DomainName).IsRequired().HasMaxLength(253);
    }
}
