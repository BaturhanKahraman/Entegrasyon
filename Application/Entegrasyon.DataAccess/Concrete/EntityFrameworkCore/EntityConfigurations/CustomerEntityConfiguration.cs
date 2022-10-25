using Entegrasyon.Entity.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<Customer>
{

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.OwnsOne(x => x.Address);
    }
}