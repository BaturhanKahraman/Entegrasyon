using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.ViewModels.ProductVariant;

public record ProductVariantDetailViewModel(
[property: Display(Name = "Varyant Kimlik Numarası")] Guid Id,
[property: Display(Name = "Barkod")] string Barcode,
[property: Display(Name = "Ağırlık/Boyut")] decimal DeminsionalWeight,
[property: Display(Name = "Para Birimi")] string CurrencyType,
[property: Display(Name = "Liste Fiyatı")] decimal ListPrice,
[property: Display(Name = "Satış Fiyatı")] decimal SalePrice,
[property: Display(Name = "Maliyet")] decimal CostPrice,
[property: Display(Name = "KDV Oranı")] decimal VatRate,
string[] ImageLinks,
IEnumerable<VariantStockDetailViewModel> StockDetails
    );
