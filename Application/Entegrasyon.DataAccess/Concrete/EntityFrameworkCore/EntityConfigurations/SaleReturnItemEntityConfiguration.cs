using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SaleReturnItemEntityConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.SaleItem)
            .WithMany()
            .HasForeignKey(x => x.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrderItem)
            .WithMany()
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RestoredBy)
            .WithMany()
            .HasForeignKey(x => x.RestoredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SaleItemId);
        builder.HasIndex(x => x.OrderItemId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SaleReturnItem_SaleOrOrder",
            "(\"SaleItemId\" IS NOT NULL AND \"OrderItemId\" IS NULL) OR (\"SaleItemId\" IS NULL AND \"OrderItemId\" IS NOT NULL)"));
    }
}
