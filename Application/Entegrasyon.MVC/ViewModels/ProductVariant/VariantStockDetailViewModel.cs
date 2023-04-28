using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.ViewModels.ProductVariant;

public record VariantStockDetailViewModel(
    [Display(Name ="Depo/Ofis")] string OfficeName,
    [Display(Name = "Geçerli Stok")] int CurrentStock,
    [Display(Name = "Satılmış Ürün Sayısı")] int SoldQuantity,
    [Display(Name = "İlk Stok")] int FirstTotalStock
    );

