using Entegrasyon.Entity.Dtos.Branches;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CreateStockTransferRequestDtoValidator : AbstractValidator<CreateStockTransferRequestDto>
{
    public CreateStockTransferRequestDtoValidator()
    {
        RuleFor(x => x.SourceBranchOfficeId)
            .GreaterThan(0).WithMessage("Kaynak şube geçersiz.");

        RuleFor(x => x.TargetBranchOfficeId)
            .GreaterThan(0).WithMessage("Hedef şube geçersiz.");

        RuleFor(x => x)
            .Must(x => x.SourceBranchOfficeId != x.TargetBranchOfficeId)
            .WithMessage("Kaynak ve hedef şube aynı olamaz.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("En az bir ürün seçmelisiniz.")
            .Must(items => items != null && items.Count > 0)
            .WithMessage("En az bir ürün seçmelisiniz.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Transfer miktarı 0'dan büyük olmalıdır.");
            item.RuleFor(i => i.ProductVariantId)
                .NotEqual(Guid.Empty).WithMessage("Ürün kimliği geçersiz.");
        });
    }
}
