using Entegrasyon.Entity.Dtos.Branches;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class RejectStockTransferRequestDtoValidator : AbstractValidator<RejectStockTransferRequestDto>
{
    public RejectStockTransferRequestDtoValidator()
    {
        RuleFor(x => x.RequestId)
            .GreaterThan(0).WithMessage("Talep kimliği geçersiz.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Red gerekçesi boş bırakılamaz.")
            .MaximumLength(500).WithMessage("Red gerekçesi en fazla 500 karakter olabilir.");
    }
}
