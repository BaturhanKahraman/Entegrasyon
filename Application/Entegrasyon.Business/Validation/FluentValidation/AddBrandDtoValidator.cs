using Entegrasyon.Entity.Dtos.Brand;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddBrandDtoValidator:AbstractValidator<AddBrandDto>
{
    public AddBrandDtoValidator()
    {
        RuleFor(b => b.Name)
            .NotEmpty();
    }
}
