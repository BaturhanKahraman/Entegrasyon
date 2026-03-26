using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontProductQuestionEntityConfiguration : IEntityTypeConfiguration<StorefrontProductQuestion>
{
    public void Configure(EntityTypeBuilder<StorefrontProductQuestion> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.ProductId });
        builder.HasIndex(x => new { x.TenantId, x.IsPublished });

        builder.Property(x => x.QuestionText).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.AnswerText).HasMaxLength(2000);
    }
}
