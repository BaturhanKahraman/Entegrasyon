using Entegrasyon.Entity.Dtos.Brand;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditBrandDtoValidator : AbstractValidator<EditBrandDto>
{
    public EditBrandDtoValidator()
    {
        RuleFor(b => b.Id)
            .GreaterThan(0).WithMessage("Geçerli bir marka seçilmelidir.");

        RuleFor(b => b.Name)
            .NotEmpty().WithMessage("Marka adı boş geçilemez.")
            .MaximumLength(55).WithMessage("Marka adı en fazla 55 karakter olabilir.");

        RuleFor(b => b.SeoSlug)
            .MaximumLength(100).WithMessage("SEO slug en fazla 100 karakter olabilir.")
            .When(b => !string.IsNullOrEmpty(b.SeoSlug));
    }
}
