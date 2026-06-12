using Entegrasyon.Entity.Dtos.Reports;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class NotifySupplierHighReturnDtoValidator : AbstractValidator<NotifySupplierHighReturnDto>
{
    public NotifySupplierHighReturnDtoValidator()
    {
        RuleFor(x => x.ProductVariantId)
            .NotEmpty().WithMessage("Geçerli bir ürün seçilmedi.");

        RuleFor(x => x.ReturnRatePercent)
            .InclusiveBetween(0, 100).WithMessage("İade oranı 0 ile 100 arasında olmalıdır.");
    }
}
