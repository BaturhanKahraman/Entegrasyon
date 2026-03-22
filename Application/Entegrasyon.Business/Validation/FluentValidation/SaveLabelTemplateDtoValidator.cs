using Entegrasyon.Entity.Dtos.Label;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class SaveLabelTemplateDtoValidator : AbstractValidator<SaveLabelTemplateDto>
{
    public SaveLabelTemplateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Şablon adı boş olamaz")
            .MaximumLength(100).WithMessage("Şablon adı en fazla 100 karakter olabilir");

        RuleFor(x => x.WidthMm)
            .GreaterThan(0).WithMessage("Genişlik 0'dan büyük olmalıdır");

        RuleFor(x => x.HeightMm)
            .GreaterThan(0).WithMessage("Yükseklik 0'dan büyük olmalıdır");

        RuleFor(x => x.Elements)
            .NotNull().WithMessage("Eleman listesi boş olamaz");
    }
}
