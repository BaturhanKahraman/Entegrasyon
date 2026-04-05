using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditProductVariantValidator : AbstractValidator<EditProductVariantDto>
{
    public EditProductVariantValidator()
    {
        RuleFor(x => x.ListPrice).GreaterThan(0).WithMessage("Liste fiyatı 0'dan büyük olmalıdır.");
        RuleFor(x => x.SalePrice).GreaterThan(0).WithMessage("Satış fiyatı 0'dan büyük olmalıdır.");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100)
            .WithMessage("KDV oranı 0-100 arasında olmalıdır.");

        RuleFor(x => x.SalePrice)
            .LessThanOrEqualTo(x => x.ListPrice)
            .WithMessage("Satış fiyatı liste fiyatından büyük olamaz.");

        RuleFor(x => x.DimensionalWeight)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Desi 0 veya daha büyük olmalıdır.");
    }
}
