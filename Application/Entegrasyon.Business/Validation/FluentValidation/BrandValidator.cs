using Entegrasyon.Entity.Brands;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class BrandValidator:AbstractValidator<Brand>
{
    public BrandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Marka adı boş geçilemez");
    }
}