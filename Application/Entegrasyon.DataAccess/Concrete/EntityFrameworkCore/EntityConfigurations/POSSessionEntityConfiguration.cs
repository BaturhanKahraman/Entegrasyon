using Entegrasyon.Entity.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class POSSessionEntityConfiguration : IEntityTypeConfiguration<POSSession>
{
    public void Configure(EntityTypeBuilder<POSSession> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.OpeningCash).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ClosingCash).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => new { x.BranchOfficeId, x.Status });

        builder.HasOne(x => x.BranchOffice)
            .WithMany()
            .HasForeignKey(x => x.BranchOfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Cashier)
            .WithMany()
            .HasForeignKey(x => x.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.POSSession)
            .HasForeignKey(x => x.POSSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CashMovements)
            .WithOne(x => x.POSSession)
            .HasForeignKey(x => x.POSSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
