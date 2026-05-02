using System.Globalization;
using Entegrasyon.Desktop.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Entegrasyon.Desktop.Components.Pages;

public partial class OfflineSales
{
    [Inject] private OfflineSaleService SaleService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private string _barcodeSearch = string.Empty;
    private string _paymentMethod = "Nakit";
    private bool _processing;
    private readonly List<CartItem> _cartItems = [];
    private DailySalesSummary _todaySummary = new(0, 0, 0, 0);

    private static readonly CultureInfo _trCulture = new("tr-TR");

    // Computed totals
    private decimal Subtotal => _cartItems.Sum(x => x.Quantity * x.UnitPrice);

    private decimal TotalDiscount => _cartItems.Sum(x =>
        x.Quantity * x.UnitPrice * ((decimal)x.DiscountPercent / 100));

    private IEnumerable<(decimal Rate, decimal Amount)> TaxBreakdown =>
        _cartItems.GroupBy(x => x.VatRate)
            .Select(g => (
                Rate: g.Key,
                Amount: g.Sum(x =>
                {
                    var lineNet = x.Quantity * x.UnitPrice * (1 - (decimal)x.DiscountPercent / 100);
                    return lineNet * (g.Key / 100);
                })))
            .Where(x => x.Amount > 0)
            .OrderBy(x => x.Rate);

    private decimal Total => Subtotal - TotalDiscount +
        _cartItems.Sum(x =>
        {
            var lineNet = x.Quantity * x.UnitPrice * (1 - (decimal)x.DiscountPercent / 100);
            return lineNet * (x.VatRate / 100);
        });

    protected override async Task OnInitializedAsync()
    {
        _todaySummary = await SaleService.GetTodaySummaryAsync();
    }

    private async Task HandleBarcodeSearch(KeyboardEventArgs e)
    {
        if (e.Key != "Enter" || string.IsNullOrWhiteSpace(_barcodeSearch))
            return;

        await SearchAndAddProduct(_barcodeSearch.Trim());
        _barcodeSearch = string.Empty;
    }

    private async Task SearchAndAddProduct(string barcode)
    {
        var product = await SaleService.FindByBarcodeAsync(barcode);

        if (product is null)
        {
            Snackbar.Add($"Barkoda ait urun bulunamadi: {barcode}", Severity.Warning);
            return;
        }

        if (product.StockQuantity <= 0)
        {
            var confirm = await DialogService.ShowMessageBox(
                "Stok Yetersiz",
                $"\"{product.Title}\" stok miktari: {product.StockQuantity}. Yine de eklemek istiyor musunuz?",
                yesText: "Ekle",
                cancelText: "Iptal");

            if (confirm != true) return;
        }

        var existing = _cartItems.FirstOrDefault(x => x.ProductId == product.ProductId);
        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            _cartItems.Add(new CartItem
            {
                ProductId = product.ProductId,
                ProductName = product.Title,
                Size = product.Size ?? "-",
                Quantity = 1,
                UnitPrice = product.SalePrice,
                DiscountPercent = 0,
                VatRate = product.VatRate,
                StockQuantity = product.StockQuantity
            });
        }

        Snackbar.Add($"Sepete eklendi: {product.Title}", Severity.Success);
    }

    private void RemoveFromCart(CartItem item)
    {
        _cartItems.Remove(item);
        Snackbar.Add("Urun sepetten cikarildi", Severity.Info);
    }

    private void ClearCart()
    {
        _cartItems.Clear();
        Snackbar.Add("Sepet temizlendi", Severity.Info);
    }

    private async Task CompleteSale()
    {
        if (!_cartItems.Any())
        {
            Snackbar.Add("Sepet bos!", Severity.Warning);
            return;
        }

        _processing = true;

        try
        {
            var items = _cartItems.Select(c => new OfflineSaleItemDto(
                c.ProductId, c.Quantity, c.UnitPrice, c.DiscountPercent, c.VatRate
            )).ToList();

            // İlk deneme — preventive warning var mı?
            var result = await SaleService.CompleteSaleAsync(items, _paymentMethod, null);

            // Stok uyarısı varsa kullanıcıya göster + override iste
            if (!result.Success && result.Warnings is { Count: > 0 } warnings)
            {
                var lines = warnings.Select(w =>
                {
                    var ageMin = (DateTimeOffset.UtcNow - w.LastSyncedAt).TotalMinutes;
                    var ageText = w.LastSyncedAt == DateTimeOffset.MinValue
                        ? "(senkron yok)"
                        : $"(son senkron: {ageMin:F0} dk önce)";
                    return $"• {w.ProductTitle} — istenen {w.RequestedQuantity}, lokal stok {w.StockBefore} {ageText}";
                });
                var msg = "Aşağıdaki ürünlerde lokal cache'e göre stok yetersiz:\n\n"
                          + string.Join("\n", lines)
                          + "\n\nYine de satışı tamamlamak istiyor musunuz? "
                          + "Senkron sırasında oversell tespit edilirse admin paneline bildirim gider.";

                var confirm = await DialogService.ShowMessageBox(
                    "Stok Uyarısı",
                    msg,
                    yesText: "Yine de Sat",
                    cancelText: "İptal");

                if (confirm != true)
                {
                    Snackbar.Add("Satış iptal edildi.", Severity.Info);
                    return;
                }

                // Override ile tekrar dene
                result = await SaleService.CompleteSaleAsync(items, _paymentMethod, null, forceOverride: true);
            }

            if (result.Success)
            {
                Snackbar.Add($"Satis tamamlandi! Toplam: {Total:C}", Severity.Success);
                _cartItems.Clear();
                _todaySummary = await SaleService.GetTodaySummaryAsync();
            }
            else
            {
                Snackbar.Add($"Satis hatasi: {result.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Satis hatasi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _processing = false;
        }
    }

    private class CartItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int DiscountPercent { get; set; }
        public decimal VatRate { get; set; } = 20;
        public int StockQuantity { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice * (1 - (decimal)DiscountPercent / 100);
    }
}
