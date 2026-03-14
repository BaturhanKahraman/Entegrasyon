using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StockMovementEntityConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.StockBefore).IsRequired();
        builder.Property(x => x.StockAfter).IsRequired();

        builder.Property(x => x.ReferenceType).HasMaxLength(50);
        builder.Property(x => x.ReferenceId).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.BranchOffice)
            .WithMany()
            .HasForeignKey(x => x.BranchOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProductVariantId, x.BranchOfficeId });
        builder.HasIndex(x => x.CreatedAt);
    }
}
