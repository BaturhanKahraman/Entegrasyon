using Entegrasyon.Entity.POS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class POSTransactionEntityConfiguration : IEntityTypeConfiguration<POSTransaction>
{
    public void Configure(EntityTypeBuilder<POSTransaction> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.CashReceived).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ChangeGiven).HasColumnType("numeric(18,2)");

        builder.HasOne(x => x.Sale)
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
