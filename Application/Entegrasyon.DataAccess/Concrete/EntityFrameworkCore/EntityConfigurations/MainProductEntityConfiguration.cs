using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class MainProductEntityConfiguration:IEntityTypeConfiguration<MainProduct>
{
    public void Configure(EntityTypeBuilder<MainProduct> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}