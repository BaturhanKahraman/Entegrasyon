using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BranchEntityConfiguration:IEntityTypeConfiguration<BranchOffice>
{
    public void Configure(EntityTypeBuilder<BranchOffice> builder)
    {
        builder.HasMany(x => x.Users)
            .WithOne(x => x.DefaultBranchOffice)
            .HasForeignKey(x => x.DefaultBranchOfficeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}