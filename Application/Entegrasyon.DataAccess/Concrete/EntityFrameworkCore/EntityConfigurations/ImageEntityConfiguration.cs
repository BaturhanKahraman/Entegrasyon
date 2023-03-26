using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations
{
    public class ImageEntityConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(i=>i.ProductVariant)
                .WithMany(p=>p.Images)
                .HasForeignKey(x=>x.ProductVariantId);
            builder.HasQueryFilter(x => !x.IsDeleted);

        }
    }
}
