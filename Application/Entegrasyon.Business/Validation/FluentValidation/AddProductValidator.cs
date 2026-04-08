using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductValidator : AbstractValidator<AddProductDto>
{
    public AddProductValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Ürün adı boş geçilemez");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Ürün kategorisi boş geçilemez");
        RuleFor(x => x.ProductVariants).NotEmpty().WithMessage("Ürün varyantları boş geçilemez");

        RuleForEach(x => x.ProductVariants).SetValidator(new AddProductVariantValidator());
    }
}
