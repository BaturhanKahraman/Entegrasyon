using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SaleEntityConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.SaleNumber).IsUnique();
        builder.HasIndex(x => x.ReturnCode)
               .IsUnique()
               .HasFilter("\"ReturnCode\" IS NOT NULL");
        builder.HasIndex(x => x.SaleDate);
        builder.HasIndex(x => x.SaleSource);
        builder.HasIndex(x => x.SaleStatus);

        // Sparse unique index on IdempotencyKey — sadece offline sales için doludur,
        // null'lar dahil edilmez (postgres partial index).
        builder.HasIndex(x => x.IdempotencyKey)
               .IsUnique()
               .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.Property(x => x.GeneralDiscount).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.GeneralDiscountReason)
               .WithMany()
               .HasForeignKey(x => x.GeneralDiscountReasonId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
