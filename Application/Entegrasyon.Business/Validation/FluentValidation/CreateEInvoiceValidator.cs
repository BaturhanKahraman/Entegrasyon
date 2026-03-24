using Entegrasyon.Entity.Dtos.Invoicing;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CreateEInvoiceValidator : AbstractValidator<CreateEInvoiceDto>
{
    public CreateEInvoiceValidator()
    {
        RuleFor(x => x.CustomerTaxId)
            .NotEmpty().WithMessage("Vergi kimlik numarasi bos olamaz.")
            .Must(x => x.Length is 10 or 11).WithMessage("VKN 10, TCKN 11 haneli olmalidir.");

        RuleFor(x => x.CustomerTitle)
            .NotEmpty().WithMessage("Musteri unvani bos olamaz.")
            .MaximumLength(300);

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("Fatura tarihi bos olamaz.");

        RuleFor(x => x.InvoiceType)
            .IsInEnum().WithMessage("Gecersiz fatura tipi.");

        RuleFor(x => x.IntegratorProvider)
            .IsInEnum().WithMessage("Gecersiz entegrator saglayici.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir fatura kalemi eklenmeli.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductName)
                .NotEmpty().WithMessage("Urun adi bos olamaz.");
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Miktar 0'dan buyuk olmali.");
            line.RuleFor(l => l.UnitPrice)
                .GreaterThan(0).WithMessage("Birim fiyat 0'dan buyuk olmali.");
            line.RuleFor(l => l.TaxRate)
                .GreaterThanOrEqualTo(0).WithMessage("KDV orani negatif olamaz.");
        });
    }
}
