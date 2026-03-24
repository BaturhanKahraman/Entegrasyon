using Entegrasyon.Entity.Dtos.Marketplace;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class SaveCommissionRateDtoValidator : AbstractValidator<SaveCommissionRateDto>
{
    public SaveCommissionRateDtoValidator()
    {
        RuleFor(x => x.MarketPlaceId)
            .GreaterThan(0)
            .WithMessage("Pazaryeri seçilmeli.");

        RuleFor(x => x.CommissionPercent)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Komisyon oranı 0-100 arasında olmalı.");

        RuleFor(x => x.ServiceFeePercent)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Hizmet bedeli oranı 0-100 arasında olmalı.")
            .When(x => x.ServiceFeePercent.HasValue);

        RuleFor(x => x.TransactionFeeFixed)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Sabit işlem ücreti negatif olamaz.")
            .When(x => x.TransactionFeeFixed.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Açıklama en fazla 500 karakter olabilir.")
            .When(x => x.Description is not null);

        RuleFor(x => x.CategoryId)
            .Null()
            .WithMessage("Default oran için kategori belirtilmemeli.")
            .When(x => x.IsDefault);
    }
}
