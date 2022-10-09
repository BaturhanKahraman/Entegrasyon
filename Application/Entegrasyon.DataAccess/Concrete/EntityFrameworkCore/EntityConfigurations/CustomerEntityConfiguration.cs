using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<ApplicationCustomer>
{

    public void Configure(EntityTypeBuilder<ApplicationCustomer> builder)
    {
        builder
            .HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "english",  
                p => new { p.NationalIdentity,p.Name,p.Surname })  
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
    }
}