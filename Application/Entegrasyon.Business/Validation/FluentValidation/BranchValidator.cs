using Entegrasyon.Entity;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class BranchValidator:AbstractValidator<BranchOffice>
{
    public BranchValidator()
    {
        RuleFor(b=>b.Name).NotEmpty().WithMessage("Lütfen isim alanını boş bırakmayın!");
    }
}