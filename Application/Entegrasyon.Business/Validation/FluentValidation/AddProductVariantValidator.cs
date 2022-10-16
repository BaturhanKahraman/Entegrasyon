using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;
using FluentValidation.Validators;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductVariantValidator : AbstractValidator<AddProductVariantDto>
{
    public AddProductVariantValidator()
    {
        RuleFor(x => x.ListPrice).NotEmpty().WithMessage("Liste fiyatı boş geçilemez");
        RuleFor(x => x.SalePrice).NotEmpty().WithMessage("Satış Fiyatı boş geçilemez");
        //RuleFor(x => x.AttributeKeyValues).NotEmpty().WithMessage("Özellik değerleri boş geçilemez");
        RuleFor(x => x.BranchOfficeStocks).NotEmpty().WithMessage("Lütfen stok değerlerini girin.");
        RuleForEach(x => x.BranchOfficeStocks).SetValidator(new AddBranchOfficeStockValidator());


    }
}