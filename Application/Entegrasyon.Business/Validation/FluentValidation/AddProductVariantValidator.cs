using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductVariantValidator : AbstractValidator<AddProductVariantDto>
{
    public AddProductVariantValidator()
    {
        RuleFor(x => x.ListPrice).Must(v => v.HasValue && v.Value > 0).WithMessage("Liste fiyatı boş geçilemez");
        RuleFor(x => x.SalePrice).Must(v => v.HasValue && v.Value > 0).WithMessage("Satış Fiyatı boş geçilemez");
        //RuleFor(x => x.AttributeKeyValues).NotEmpty().WithMessage("Özellik değerleri boş geçilemez");
        RuleFor(x => x.BranchOfficeStocks).NotEmpty().WithMessage("Lütfen stok değerlerini girin.");
        RuleForEach(x => x.BranchOfficeStocks).SetValidator(new AddBranchOfficeStockValidator());


    }
}