using Entegrasyon.Entity.Barcode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TempBarcodeEntityConfiguration:IEntityTypeConfiguration<TempBarcode>
{
    public void Configure(EntityTypeBuilder<TempBarcode> builder)
    {
        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Barcode).IsUnique();
        
        
    }
}