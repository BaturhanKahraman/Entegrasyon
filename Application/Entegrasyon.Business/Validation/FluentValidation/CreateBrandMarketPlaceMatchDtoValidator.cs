using Entegrasyon.Entity.Dtos.Brand;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CreateBrandMarketPlaceMatchDtoValidator : AbstractValidator<CreateBrandMarketPlaceMatchDto>
{
    public CreateBrandMarketPlaceMatchDtoValidator()
    {
        RuleFor(x => x.ApplicationBrandId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir marka seçilmelidir.");

        RuleFor(x => x.MarketPlaceId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir e-ticaret sitesi seçilmelidir.");

        RuleFor(x => x.MarketPlaceBrandId)
            .GreaterThan(0)
            .WithMessage("Geçerli bir e-ticaret sitesi marka ID'si sağlanmalıdır.");
    }
}
