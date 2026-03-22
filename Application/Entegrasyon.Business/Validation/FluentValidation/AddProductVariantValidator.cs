using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductVariantValidator : AbstractValidator<AddProductVariantDto>
{
    public AddProductVariantValidator()
    {
        RuleFor(x => x.ListPrice).Must(v => v.HasValue && v.Value > 0).WithMessage("Liste fiyatı boş geçilemez");
        RuleFor(x => x.SalePrice).Must(v => v.HasValue && v.Value > 0).WithMessage("Satış Fiyatı boş geçilemez");
        RuleFor(x => x.BranchOfficeStocks).NotEmpty().WithMessage("Lütfen stok değerlerini girin.");
        RuleForEach(x => x.BranchOfficeStocks).SetValidator(new AddBranchOfficeStockValidator());

        RuleFor(x => x.VatRate)
            .Must(v => !v.HasValue || (v.Value >= 0 && v.Value <= 100))
            .WithMessage("KDV oranı 0-100 arasında olmalıdır.");

        RuleFor(x => x)
            .Must(x => !x.SalePrice.HasValue || !x.ListPrice.HasValue || x.SalePrice <= x.ListPrice)
            .WithMessage("Satış fiyatı liste fiyatından büyük olamaz.");
    }
}