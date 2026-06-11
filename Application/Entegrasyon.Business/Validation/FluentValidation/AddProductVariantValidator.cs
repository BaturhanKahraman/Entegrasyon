using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class AddProductVariantValidator : AbstractValidator<AddProductVariantDto>
{
    public AddProductVariantValidator()
    {
        RuleFor(x => x.ListPrice)
            .Must(v => !v.HasValue || v.Value >= 0)
            .WithMessage("Liste fiyatı negatif olamaz.");

        RuleFor(x => x.SalePrice).Must(v => v.HasValue && v.Value > 0).WithMessage("Satış Fiyatı boş geçilemez");
        // Bug #3: Stok zorunluluğu kaldırıldı — esnaf stoksuz ürün ekleyebilmeli
        // (ön sipariş / yolda / henüz gelmemiş / tükenmiş). BranchOfficeStocks boş olabilir;
        // girilen stok satırları (varsa) yine de kendi içinde doğrulanır.
        RuleForEach(x => x.BranchOfficeStocks).SetValidator(new AddBranchOfficeStockValidator());

        RuleFor(x => x.VatRate)
            .Must(v => !v.HasValue || (v.Value >= 0 && v.Value <= 100))
            .WithMessage("KDV oranı 0-100 arasında olmalıdır.");

        RuleFor(x => x)
            .Must(x => !x.SalePrice.HasValue || !x.ListPrice.HasValue || x.ListPrice.Value == 0 || x.SalePrice <= x.ListPrice)
            .WithMessage("Satış fiyatı liste fiyatından büyük olamaz.");
    }
}
