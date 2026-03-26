using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontLoginHistoryEntityConfiguration : IEntityTypeConfiguration<StorefrontLoginHistory>
{
    public void Configure(EntityTypeBuilder<StorefrontLoginHistory> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.AuthId);
        builder.HasIndex(x => x.LoginAt);
    }
}
