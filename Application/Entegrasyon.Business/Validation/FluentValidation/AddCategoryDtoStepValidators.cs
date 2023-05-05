using Entegrasyon.Entity.Dtos.Category.AddStep;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddCategoryDtoStepOneValidator:AbstractValidator<AddCategoryDtoStepOne>
{
    public AddCategoryDtoStepOneValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Kategori ismi boş olamaz.")
            .MaximumLength(255).WithMessage("Karakter sayısı 255'i geçemez.")
            .MinimumLength(2).WithMessage("Karakter sayısı 2 altında olamaz.");
        RuleFor(c => c.SuperCategoryId).NotEqual(0);
    }
}
