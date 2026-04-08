using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StockTransferRequestEntityConfiguration : IEntityTypeConfiguration<StockTransferRequest>
{
    public void Configure(EntityTypeBuilder<StockTransferRequest> builder)
    {
        builder.ToTable("StockTransferRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasIndex(x => new { x.SourceBranchOfficeId, x.Status });
        builder.HasIndex(x => new { x.TargetBranchOfficeId, x.Status });
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.SourceBranchOffice)
            .WithMany()
            .HasForeignKey(x => x.SourceBranchOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetBranchOffice)
            .WithMany()
            .HasForeignKey(x => x.TargetBranchOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.TransferRequest)
            .HasForeignKey(i => i.TransferRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class StockTransferRequestItemEntityConfiguration : IEntityTypeConfiguration<StockTransferRequestItem>
{
    public void Configure(EntityTypeBuilder<StockTransferRequestItem> builder)
    {
        builder.ToTable("StockTransferRequestItems");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TransferRequestId);
    }
}
