using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SaleReturnEntityConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.Property(x => x.RefundAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Source).HasConversion<int>();
        builder.Property(x => x.ReturnStatus).HasConversion<int>();

        builder.HasOne(x => x.Sale)
            .WithMany(s => s.Returns)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Order)
            .WithMany(o => o.Returns)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReturnedBy)
            .WithMany()
            .HasForeignKey(x => x.ReturnedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedBy)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CompletedBy)
            .WithMany()
            .HasForeignKey(x => x.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CancelledBy)
            .WithMany()
            .HasForeignKey(x => x.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReturnReason)
            .WithMany()
            .HasForeignKey(x => x.ReturnReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RefundPaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.RefundPaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RestoreBranchOffice)
            .WithMany()
            .HasForeignKey(x => x.RestoreBranchOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.SaleReturn)
            .HasForeignKey(x => x.SaleReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.ReturnStatus);
        builder.HasIndex(x => x.CompletedAt);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SaleReturn_SaleOrOrder",
            "(\"SaleId\" IS NOT NULL AND \"OrderId\" IS NULL) OR (\"SaleId\" IS NULL AND \"OrderId\" IS NOT NULL)"));
    }
}
