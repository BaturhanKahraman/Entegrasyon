using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCategoryDataEntityConfiguration : IEntityTypeConfiguration<TemplateCategoryData>
{
    public void Configure(EntityTypeBuilder<TemplateCategoryData> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DefaultVatRate).HasPrecision(5, 2);

        builder.HasOne(x => x.Package).WithMany(x => x.Categories).HasForeignKey(x => x.PackageId);
        builder.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentTemplateCategoryDataId);
        builder.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCategoryData).HasForeignKey(x => x.TemplateCategoryDataId);
        builder.HasMany(x => x.Attributes).WithOne(x => x.TemplateCategoryData).HasForeignKey(x => x.TemplateCategoryDataId);
    }
}
