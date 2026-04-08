using Entegrasyon.Entity.Dtos.Branches;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class RequestDeleteDtoValidator : AbstractValidator<RequestDeleteDto>
{
    public RequestDeleteDtoValidator()
    {
        RuleFor(x => x.BranchOfficeId)
            .GreaterThan(0).WithMessage("Şube kimliği geçersiz.");

        RuleFor(x => x.TransferTargetBranchOfficeId)
            .GreaterThan(0).WithMessage("Hedef şube kimliği geçersiz.")
            .When(x => x.TransferTargetBranchOfficeId.HasValue);

        RuleFor(x => x)
            .Must(x => x.TransferTargetBranchOfficeId != x.BranchOfficeId)
            .WithMessage("Hedef şube, silinen şubeyle aynı olamaz.")
            .When(x => x.TransferTargetBranchOfficeId.HasValue);
    }
}
