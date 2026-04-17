using Entegrasyon.Entity.Dtos.Sale;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CompleteSaleReturnValidator : AbstractValidator<CompleteSaleReturnDto>
{
    public CompleteSaleReturnValidator()
    {
        RuleFor(x => x.ReturnId).GreaterThan(0);
        RuleFor(x => x.CompletedByUserId).NotEmpty();
        RuleFor(x => x.BranchOfficeId).GreaterThan(0).WithMessage("Stok ekleme için şube seçimi zorunludur.");
        RuleFor(x => x.ItemIdsToRestore).NotNull();
    }
}
