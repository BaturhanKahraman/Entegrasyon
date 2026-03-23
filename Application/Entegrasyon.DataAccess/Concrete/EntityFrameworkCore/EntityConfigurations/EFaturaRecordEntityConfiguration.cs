using Entegrasyon.Entity.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class EFaturaRecordEntityConfiguration : IEntityTypeConfiguration<EFaturaRecord>
{
    public void Configure(EntityTypeBuilder<EFaturaRecord> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.InvoiceUuid);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
