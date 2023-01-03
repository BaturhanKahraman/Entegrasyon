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
        BranchOffice[] offices = new BranchOffice[]
        {
            new BranchOffice { Id = 1,Name = "Merkez Ofis",CreatedAt = DateTimeOffset.MinValue },
            new BranchOffice { Id = 2,Name = "İstanbul",CreatedAt = DateTimeOffset.MinValue},
            new BranchOffice { Id = 3,Name = "İzmir",CreatedAt = DateTimeOffset.MinValue },
            new BranchOffice { Id = 4,Name = "Ankara",CreatedAt = DateTimeOffset.MinValue }
    };
        builder.HasData(offices);
    }
}