using Entegrasyon.Entity.Dtos.Product.Discount;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class ApplyDiscountValidator : AbstractValidator<ApplyDiscountDto>
{
    public ApplyDiscountValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ürün ID boş olamaz.");

        RuleFor(x => x.DiscountPercentage)
            .GreaterThan(0).WithMessage("İndirim yüzdesi 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(100).WithMessage("İndirim yüzdesi 100'den büyük olamaz.");
    }
}
