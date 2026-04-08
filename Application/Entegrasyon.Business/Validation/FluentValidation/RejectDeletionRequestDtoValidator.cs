using Entegrasyon.Entity.Dtos.Branches;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class RejectDeletionRequestDtoValidator : AbstractValidator<RejectDeletionRequestDto>
{
    public RejectDeletionRequestDtoValidator()
    {
        RuleFor(x => x.RequestId)
            .GreaterThan(0).WithMessage("Talep kimliği geçersiz.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Red gerekçesi boş bırakılamaz.")
            .MaximumLength(500).WithMessage("Red gerekçesi en fazla 500 karakter olabilir.");
    }
}
