using Entegrasyon.Entity.Dtos.Category;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class BulkCategoryMatchDtoValidator : AbstractValidator<BulkCategoryMatchDto>
{
    public BulkCategoryMatchDtoValidator()
    {
        RuleFor(x => x.MarketPlaceId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir marketplace seçilmelidir.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("En az bir eşleştirme öğesi gereklidir.")
            .Must(items => items.Count <= 500)
            .WithMessage("En fazla 500 öğe gönderilebilir.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ApplicationCategoryId)
                .GreaterThan(0)
                .WithMessage("Geçerli bir kategori ID'si gereklidir.");

            item.RuleFor(i => i.MarketPlaceCategoryId)
                .GreaterThan(0)
                .WithMessage("Geçerli bir marketplace kategori ID'si gereklidir.");
        });
    }
}
