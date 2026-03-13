using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class EditProductValidator : AbstractValidator<EditProductDto>
{
    public EditProductValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Ürün adı boş geçilemez");

        RuleFor(x => x.StockCode)
            .NotEmpty().WithMessage("Stok kodu boş geçilemez");

        RuleFor(x => x.BrandId)
            .GreaterThan(0).WithMessage("Ürün markası seçilmelidir");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Ürün kategorisi seçilmelidir");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.ListPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Liste fiyatı 0'dan küçük olamaz");
            v.RuleFor(x => x.SalePrice)
                .GreaterThanOrEqualTo(0).WithMessage("Satış fiyatı 0'dan küçük olamaz");
            v.RuleFor(x => x.CostPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Maliyet fiyatı 0'dan küçük olamaz");
        });
    }
}
