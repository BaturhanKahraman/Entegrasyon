using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BranchEntityConfiguration : IEntityTypeConfiguration<BranchOffice>
{
    public void Configure(EntityTypeBuilder<BranchOffice> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.NormalizedName).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);

        builder.HasMany(x => x.Users)
            .WithOne(x => x.DefaultBranchOffice)
            .HasForeignKey(x => x.DefaultBranchOfficeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Denormalized cache: aktif Pending silme talebi var mı? O(1) sorgu için FK.
        // OnDelete SetNull çünkü talep reject edildiğinde FK null'lanmalı (cascade zararlı).
        builder.HasOne(x => x.DeletionRequest)
            .WithMany()
            .HasForeignKey(x => x.DeletionRequestId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);

        // Filtered unique index (PostgreSQL): Yalnız aktif ofisler arasında isim çakışması engellenir.
        // Silinmiş bir ofisin ismi yeniden kullanılabilir.
        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        BranchOffice[] offices = {
            new()
            {
                Id = 1,
                Name = "Merkez Ofis",
                NormalizedName = "MERKEZ OFIS",
                IsHeadquarters = true,
                CreatedAt = DateTimeOffset.MinValue
            },
        };
        builder.HasData(offices);
    }
}