using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductValidator : AbstractValidator<AddProductDto>
{
    public AddProductValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Urun adi bos gecilemez");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Urun kategorisi bos gecilemez");
        RuleFor(x => x.ProductVariants).NotEmpty().WithMessage("Urun varyantlari bos gecilemez");

        RuleForEach(x => x.ProductVariants).SetValidator(new AddProductVariantValidator());
    }
}