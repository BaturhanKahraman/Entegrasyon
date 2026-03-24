using Entegrasyon.Entity.BulkOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BulkOperationLogEntityConfiguration : IEntityTypeConfiguration<BulkOperationLog>
{
    public void Configure(EntityTypeBuilder<BulkOperationLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ErrorDetails).HasColumnType("jsonb");

        builder.HasIndex(x => x.StartedByUserId);
        builder.HasIndex(x => x.Status);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
