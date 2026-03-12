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
        RuleFor(x => x.AllowCustom)
            .Equal(true)
            .When(x => x.CategoryAttributeValues == null || x.CategoryAttributeValues.Count == 0)
            .WithMessage("Önceden tanımlı değer yoksa özel değere izin verilmelidir.");
        RuleFor(x => x.CategoryAttributeValues)
            .NotEmpty()
            .When(x => x.AllowCustom == false)
            .WithMessage("Özel değere izin verilmiyorsa en az bir değer tanımlanmalıdır.");
    }
}
