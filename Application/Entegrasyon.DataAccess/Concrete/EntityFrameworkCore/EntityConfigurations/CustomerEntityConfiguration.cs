using Entegrasyon.Entity.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<Customer>
{

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.OwnsOne(x => x.Address);
        builder.HasDiscriminator<string>("CustomerType")
            .HasValue<RetailCustomer>("Retail")
            .HasValue<CorporateCustomer>("Corporate");
    }
}
public class RetailCustomerEntityConfiguration : IEntityTypeConfiguration<RetailCustomer>
{
    public void Configure(EntityTypeBuilder<RetailCustomer> builder)
    {
        //builder.Property(x=>x.SearchVector).IsGeneratedTsVectorColumn("turkish","PhoneNumber","Surname","Name","NationalIdentity");
        builder
          .HasGeneratedTsVectorColumn(
              p => p.SearchVector,
              "turkish",
              p => new { p.PhoneNumber, p.Surname, p.Name, p.NationalIdentity })
          .HasIndex(p => p.SearchVector)
          .HasMethod("GIN");
    }
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
             p => new { p.PhoneNumber, p.CorporateName, p.TaxNumber })
         .HasIndex(p => p.SearchVector)
         .HasMethod("GIN");
    }
}