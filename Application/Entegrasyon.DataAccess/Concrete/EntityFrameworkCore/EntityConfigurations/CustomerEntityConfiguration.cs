using Entegrasyon.Entity;
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
    }
}
public class RetailCustomerEntityConfiguration : IEntityTypeConfiguration<RetailCustomer>
{
    public void Configure(EntityTypeBuilder<RetailCustomer> builder)
    {
        builder
            .HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "turkish",
                p => new { p.PhoneNumber,p.Surname,p.Name,p.NationalIdentity })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
        var retailCustomers = new RetailCustomer[3];
        retailCustomers[0] = new RetailCustomer
        {
            Id = 1,
            Name = "Ali",
            Surname = "Yılmaz",
            PhoneNumber = "0532 123 45 67",
            NationalIdentity = "12345678901",
        };
        retailCustomers[1] = new RetailCustomer
        {
            Id = 2,
            Name = "Ayşe",
            Surname = "Kara",
            PhoneNumber = "0532 123 45 67",
            NationalIdentity = "12345678901",
        };
        retailCustomers[2] = new RetailCustomer
        {
            Id = 3,
            Name = "Ahmet",
            Surname = "Yılmaz",
            PhoneNumber = "0532 123 45 67",
            NationalIdentity = "12345678901",
        };
        builder.HasData(retailCustomers);
    }

    public class CorporateCustomerEntityConfiguration : IEntityTypeConfiguration<CorporateCustomer>
    {
        public void Configure(EntityTypeBuilder<CorporateCustomer> builder)
        {
            builder.Property(x => x.SearchVector);
            builder
                .HasGeneratedTsVectorColumn(
                    p => p.SearchVector,
                    "turkish",
                    p => new { p.PhoneNumber,p.CorporateName,p.TaxNumber,p.Name,p.Surname,})
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN");
            var corporateCustomers = new CorporateCustomer[3];
            corporateCustomers[0] = new CorporateCustomer
            {
                Id = 4,
                Name = "Ali",
                Surname = "Yılmaz",
                PhoneNumber = "0532 123 45 67",
                TaxNumber = "12345678901",
                CorporateName = "Entegrasyon Yazılım"

            };
            corporateCustomers[1] = new CorporateCustomer
            {
                Id = 5,
                Name = "Ayşe",
                Surname = "Kara",
                PhoneNumber = "0532 123 45 67",
                TaxNumber = "12345678901",
                CorporateName = "Entegrasyon Yazılım",
            };
            corporateCustomers[2] = new CorporateCustomer
            {
                Id = 6,
                Name = "Ayşe",
                Surname = "Kara",
                PhoneNumber = "0532 123 45 67",
                TaxNumber = "12345678901",
                CorporateName = "Entegrasyon Yazılım",
            };
            builder.HasData(corporateCustomers);
        }

    }
}