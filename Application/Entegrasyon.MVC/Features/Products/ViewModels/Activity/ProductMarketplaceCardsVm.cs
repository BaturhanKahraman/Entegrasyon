using Entegrasyon.Entity.Dtos.Product.Activity;

namespace Entegrasyon.MVC.Features.Products.ViewModels.Activity;

/// <summary>
/// Ürün 360° pazaryeri durum kartları partial'ının modeli (_ProductMarketplaceCards.cshtml).
/// E-ticaret kapalıysa <see cref="EcommerceEnabled"/> false ve <see cref="Cards"/> boştur.
/// </summary>
public sealed class ProductMarketplaceCardsVm
{
    public required Guid ProductId { get; init; }
    public bool EcommerceEnabled { get; init; }
    public IReadOnlyList<ProductMarketplaceStatusDto> Cards { get; init; } = [];
}
