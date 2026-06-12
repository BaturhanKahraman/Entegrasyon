using Entegrasyon.Entity.Dtos.Category;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditCategoryAttributeDtoValidator : AbstractValidator<EditCategoryAttributeDto>
{
    public EditCategoryAttributeDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçersiz özellik ID.");
        RuleFor(x => x.CategoryAttributeKey).NotEmpty().MaximumLength(255).WithMessage("Anahtar boş olamaz.");
        RuleFor(x => x.CategoryAttributeHumanized).NotEmpty().MaximumLength(255).WithMessage("Görünen ad boş olamaz.");
        RuleFor(x => x.CategoryAttributeValues)
            .NotEmpty()
            .WithMessage("En az bir değer tanımlanmalıdır.");
    }
}
