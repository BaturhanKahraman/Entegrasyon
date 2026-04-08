using Entegrasyon.Entity.Dtos.Invoicing;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class CreateEInvoiceValidator : AbstractValidator<CreateEInvoiceDto>
{
    public CreateEInvoiceValidator()
    {
        RuleFor(x => x.CustomerTaxId)
            .NotEmpty().WithMessage("Vergi kimlik numarası boş olamaz.")
            .Must(x => x.Length is 10 or 11).WithMessage("VKN 10, TCKN 11 haneli olmalıdır.");

        RuleFor(x => x.CustomerTitle)
            .NotEmpty().WithMessage("Müşteri unvanı boş olamaz.")
            .MaximumLength(300);

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("Fatura tarihi boş olamaz.");

        RuleFor(x => x.InvoiceType)
            .IsInEnum().WithMessage("Geçersiz fatura tipi.");

        RuleFor(x => x.IntegratorProvider)
            .IsInEnum().WithMessage("Geçersiz entegrator sağlayıcı.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir fatura kalemi eklenmeli.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductName)
                .NotEmpty().WithMessage("Ürün adı boş olamaz.");
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalı.");
            line.RuleFor(l => l.UnitPrice)
                .GreaterThan(0).WithMessage("Birim fiyat 0'dan büyük olmalı.");
            line.RuleFor(l => l.TaxRate)
                .GreaterThanOrEqualTo(0).WithMessage("KDV oranı negatif olamaz.");
        });
    }
}
