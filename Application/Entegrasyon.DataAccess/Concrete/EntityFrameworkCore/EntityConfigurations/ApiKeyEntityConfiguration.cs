using Entegrasyon.Entity.ApiKeys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ApiKeyEntityConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(12).IsRequired();
        builder.Property(x => x.Scopes).HasMaxLength(1000);

        builder.HasIndex(x => x.KeyHash).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
