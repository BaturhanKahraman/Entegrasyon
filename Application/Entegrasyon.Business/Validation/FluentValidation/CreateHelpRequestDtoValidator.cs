using Entegrasyon.Entity.Dtos.Help;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CreateHelpRequestDtoValidator : AbstractValidator<CreateHelpRequestDto>
{
    public CreateHelpRequestDtoValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Konu boş olamaz.")
            .MaximumLength(200).WithMessage("Konu en fazla 200 karakter olabilir.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mesaj boş olamaz.")
            .MaximumLength(4000).WithMessage("Mesaj en fazla 4000 karakter olabilir.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Geçersiz kategori.");
    }
}
