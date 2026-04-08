using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BranchOfficeDeletionRequestEntityConfiguration : IEntityTypeConfiguration<BranchOfficeDeletionRequest>
{
    public void Configure(EntityTypeBuilder<BranchOfficeDeletionRequest> builder)
    {
        builder.ToTable("BranchOfficeDeletionRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        // Pending talepleri hızlı bulmak için
        builder.HasIndex(x => new { x.BranchOfficeId, x.Status });
        builder.HasIndex(x => x.Status);

        // Source şube — Restrict (soft-delete iş akışı üzerinden geçilir)
        builder.HasOne(x => x.BranchOffice)
            .WithMany()
            .HasForeignKey(x => x.BranchOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transfer target — opsiyonel, Restrict (hedef ofis manual silinemez, iş akışından geçer)
        builder.HasOne(x => x.TransferTargetBranchOffice)
            .WithMany()
            .HasForeignKey(x => x.TransferTargetBranchOfficeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Requester / Approver — Restrict (kullanıcı silinemez eğer talebi varsa)
        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Items (snapshot) — cascade delete; request silinince item'lar da silinir
        builder.HasMany(x => x.Items)
            .WithOne(i => i.DeletionRequest)
            .HasForeignKey(i => i.DeletionRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // BaseEntity soft-delete query filter
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class BranchOfficeDeletionRequestItemEntityConfiguration : IEntityTypeConfiguration<BranchOfficeDeletionRequestItem>
{
    public void Configure(EntityTypeBuilder<BranchOfficeDeletionRequestItem> builder)
    {
        builder.ToTable("BranchOfficeDeletionRequestItems");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.DeletionRequestId);
    }
}
