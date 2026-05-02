using Entegrasyon.Entity.Printing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class PrintBatchEntityConfiguration : IEntityTypeConfiguration<PrintBatch>
{
    public void Configure(EntityTypeBuilder<PrintBatch> builder)
    {
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.PrintBatch)
            .HasForeignKey(i => i.PrintBatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PrintBatchItemEntityConfiguration : IEntityTypeConfiguration<PrintBatchItem>
{
    public void Configure(EntityTypeBuilder<PrintBatchItem> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => x.PrintBatchId);
    }
}
