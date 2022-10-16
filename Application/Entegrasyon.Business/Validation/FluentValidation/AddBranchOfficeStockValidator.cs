using Entegrasyon.Entity.Dtos.Product;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public sealed class AddBranchOfficeStockValidator : AbstractValidator<AddBranchOfficeStockDto>
{
    public AddBranchOfficeStockValidator()
    {
        RuleFor(x => x.BranchOfficeId).NotEmpty().WithMessage("Şube boş geçilemez");
        RuleFor(x => x.FirstTotalStock)
            .NotEmpty().WithMessage("Stok miktarı boş geçilemez")
            .GreaterThan(0).WithMessage("Stok miktarı 0'dan büyük olmalıdır");
    }
}