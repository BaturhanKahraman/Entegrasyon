using Entegrasyon.Entity.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class EInvoiceEntityConfiguration : IEntityTypeConfiguration<EInvoice>
{
    public void Configure(EntityTypeBuilder<EInvoice> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.GibUuid).IsUnique().HasFilter("\"GibUuid\" IS NOT NULL");
        builder.HasIndex(x => x.InvoiceNumber);
        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Sale)
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.EInvoice)
            .HasForeignKey(x => x.EInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EInvoiceLineEntityConfiguration : IEntityTypeConfiguration<EInvoiceLine>
{
    public void Configure(EntityTypeBuilder<EInvoiceLine> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class EInvoiceIntegratorConfigEntityConfiguration : IEntityTypeConfiguration<EInvoiceIntegratorConfig>
{
    public void Configure(EntityTypeBuilder<EInvoiceIntegratorConfig> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.IntegratorProvider);
    }
}
