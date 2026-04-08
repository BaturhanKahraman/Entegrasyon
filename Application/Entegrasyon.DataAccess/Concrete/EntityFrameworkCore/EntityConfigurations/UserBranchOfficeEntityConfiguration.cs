using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class UserBranchOfficeEntityConfiguration : IEntityTypeConfiguration<UserBranchOffice>
{
    public void Configure(EntityTypeBuilder<UserBranchOffice> builder)
    {
        builder.ToTable("UserBranchOffices");

        // Composite PK: junction row kimliği (UserId, BranchOfficeId) çifti
        builder.HasKey(x => new { x.UserId, x.BranchOfficeId });

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserBranchOffices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade); // kullanıcı silinirse junction satırları da gider

        builder.HasOne(x => x.BranchOffice)
            .WithMany()
            .HasForeignKey(x => x.BranchOfficeId)
            .OnDelete(DeleteBehavior.Restrict); // şube silme iş akışından geçmeli, cascade yok

        // "Bir şubeye atanmış kullanıcıları listele" sorgusu için
        builder.HasIndex(x => x.BranchOfficeId);
    }
}
