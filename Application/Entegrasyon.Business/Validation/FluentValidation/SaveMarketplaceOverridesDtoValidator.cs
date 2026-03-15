using Entegrasyon.Entity.Dtos.Product.Marketplace;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class SaveMarketplaceOverridesDtoValidator : AbstractValidator<SaveMarketplaceOverridesDto>
{
    public SaveMarketplaceOverridesDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Ürün ID boş olamaz.");
        RuleFor(x => x.MarketPlaceId).GreaterThan(0).WithMessage("Pazaryeri seçilmeli.");
        RuleFor(x => x.TitleOverride).MaximumLength(200)
            .WithMessage("Başlık override en fazla 200 karakter olabilir.")
            .When(x => x.TitleOverride is not null);
        RuleFor(x => x.DescriptionOverride).MaximumLength(30000)
            .WithMessage("Açıklama override en fazla 30000 karakter olabilir.")
            .When(x => x.DescriptionOverride is not null);

        RuleForEach(x => x.VariantOverrides).ChildRules(vo =>
        {
            vo.RuleFor(x => x.ListPriceOverride).GreaterThan(0)
                .WithMessage("Liste fiyatı 0'dan büyük olmalı.")
                .When(x => x.ListPriceOverride.HasValue);
            vo.RuleFor(x => x.SalePriceOverride).GreaterThan(0)
                .WithMessage("Satış fiyatı 0'dan büyük olmalı.")
                .When(x => x.SalePriceOverride.HasValue);
        });
    }
}
