using Entegrasyon.Entity.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<Customer>
{

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.OwnsOne(x => x.Address);
        builder.HasDiscriminator(x => x.CustomerType)
            .HasValue<RetailCustomer>("Retail")
            .HasValue<CorporateCustomer>("Corporate");
        builder.Property(x => x.FullName)
            .HasComputedColumnSql(@"""Name"" || ' ' || ""Surname""",stored: true);
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Hot-path: Müşteri raporu "Yeni" segmenti (CreatedAt son 30 gün) ve RFM persentil
        // hesabı/dormant listesinin müşteri evrenini CreatedAt ile daraltır/sıralar. Partial
        // (NOT "IsDeleted") query filter SQL'iyle birebir → planner partial'ı seçer, soft-deleted
        // satırları index dışında bırakır.
        builder.HasIndex(x => x.CreatedAt)
            .HasFilter("NOT \"IsDeleted\"");
    }
}
public class RetailCustomerEntityConfiguration : IEntityTypeConfiguration<RetailCustomer>
{
    public void Configure(EntityTypeBuilder<RetailCustomer> builder)
    {
            builder.Property(x => x.RetailSearchVector);
        builder
            .HasGeneratedTsVectorColumn(
                p => p.RetailSearchVector,
                "english",
                p => new { p.PhoneNumber,p.Surname,p.Name,p.NationalIdentity })
            .HasIndex(p => p.RetailSearchVector)
            .HasMethod("GIN");
       
    }

    public class CorporateCustomerEntityConfiguration : IEntityTypeConfiguration<CorporateCustomer>
    {
        public void Configure(EntityTypeBuilder<CorporateCustomer> builder)
        {
            builder.Property(x => x.CorporateSearchVector);
            builder
                .HasGeneratedTsVectorColumn(
                    p => p.CorporateSearchVector,
                    "english",
                    p => new { p.PhoneNumber,p.CorporateName,p.TaxNumber,p.Name,p.Surname,})
                .HasIndex(p => p.CorporateSearchVector)
                .HasMethod("GIN");
          
        }

    }
}