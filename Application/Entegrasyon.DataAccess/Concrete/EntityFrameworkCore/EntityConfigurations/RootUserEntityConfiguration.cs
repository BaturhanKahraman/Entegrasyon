using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.User;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class RootUserEntityConfiguration : IEntityTypeConfiguration<RootUser>
{
    public void Configure(EntityTypeBuilder<RootUser> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}