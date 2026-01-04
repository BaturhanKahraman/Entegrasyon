namespace Entegrasyon.Blazor.Pages;

using Entegrasyon.Entity.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

public partial class Sales
{
    [Inject]
    private IDialogService? DialogService { get; set; }

    [Inject]
    private ISnackbar? Snackbar { get; set; }

    private string _barcodeSearch = string.Empty;
    private string _paymentMethod = "Nakit";
    private bool _processing;
    private List<CartItem> _cartItems = new();
    private CustomerDto? _selectedCustomer;

    private decimal _subtotal => _cartItems.Sum(x => x.Quantity * x.UnitPrice);
    private decimal _totalDiscount => _cartItems.Sum(x => (x.Quantity * x.UnitPrice) * ((decimal)x.DiscountPercent / 100));
    private decimal _subtotalAfterDiscount => _subtotal - _totalDiscount;
    private decimal _tax => _subtotalAfterDiscount * 0.20m;
    private decimal _total => _subtotalAfterDiscount + _tax;

    private async Task HandleBarcodeSearch(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_barcodeSearch))
        {
            await SearchAndAddProduct(_barcodeSearch);
            _barcodeSearch = string.Empty;
        }
    }

    private async Task SearchAndAddProduct(string barcode)
    {
        try
        {
            // TODO: Implement actual product search by barcode
            // var result = await ProductManager.GetProductByBarcode(barcode);
            // if (result.Success && result.Data is ProductsDetailDto product)
            // {
            //     var variant = product.ProductVariants.FirstOrDefault(v => v.Barcode == barcode);
            //     if (variant != null)
            //     {
            //         AddToCart(product.Title, variant.Size, variant.ListPrice, variant.Id);
            //         Snackbar.Add($"Ürün eklendi: {product.Title}", Severity.Success);
            //     }
            // }
            // else
            // {
            //     Snackbar.Add("Barkoda ait ürün bulunamadı", Severity.Warning);
            // }

            Snackbar?.Add($"Barkod aranıyor: {barcode}", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Ürün arama hatası: {ex.Message}", Severity.Error);
        }
    }

    private void AddToCart(string productName, string size, decimal price, Guid variantId)
    {
        var existingItem = _cartItems.FirstOrDefault(x => x.ProductVariantId == variantId);

        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            _cartItems.Add(new CartItem
            {
                ProductVariantId = variantId,
                ProductName = productName,
                Size = size,
                Quantity = 1,
                UnitPrice = price,
                DiscountPercent = 0
            });
        }

        RecalculateTotal();
        Snackbar?.Add($"Sepete eklendi: {productName}", Severity.Success);
    }

    private void RemoveFromCart(CartItem item)
    {
        _cartItems.Remove(item);
        RecalculateTotal();
        Snackbar?.Add("Ürün sepetten çıkarıldı", Severity.Info);
    }

    private void RecalculateTotal()
    {
        // Total is calculated via property getter, no explicit re-render needed
    }

    private void ClearCart()
    {
        _cartItems.Clear();
        _selectedCustomer = null;
        Snackbar?.Add("Sepet temizlendi", Severity.Info);
    }

    private async Task OpenCustomerSearch()
    {
        // TODO: Implement customer search dialog
        Snackbar?.Add("Müşteri arama özelliği yakında gelecek", Severity.Info);
    }

    private void ClearCustomer()
    {
        _selectedCustomer = null;
    }

    private async Task CompleteSale()
    {
        if (!_cartItems.Any())
        {
            Snackbar?.Add("Sepet boş!", Severity.Warning);
            return;
        }

        _processing = true;
        try
        {
            // TODO: Implement actual sale completion with SaleManager
            // var saleDto = new MakeSaleDto
            // {
            //     CustomerId = _selectedCustomer?.Id,
            //     PaymentMethod = _paymentMethod,
            //     SaleItems = _cartItems.Select(item => new SaleItemDto
            //     {
            //         ProductVariantId = item.ProductVariantId,
            //         Quantity = item.Quantity,
            //         UnitPrice = item.UnitPrice,
            //         DiscountPercent = item.DiscountPercent
            //     }).ToList()
            // };

            // var result = await SaleManager.MakeSale(saleDto);

            // if (result.Success)
            // {
            //     await ShowReceiptDialog();
            //     ClearCart();
            // }

            await Task.Delay(1000); // Simulate processing

            var confirm = await DialogService!.ShowMessageBox(
                "Satış Tamamlandı",
                $"Satış başarıyla tamamlandı!\n\nToplam: ₺{_total:F2}\nÖdeme: {_paymentMethod}\n\nFiş yazdırılsın mı?",
                yesText: "Yazdır",
                cancelText: "Kapat"
            );

            if (confirm == true)
            {
                Snackbar?.Add("Fiş yazdırılıyor...", Severity.Info);
                // TODO: Implement receipt printing
            }

            Snackbar?.Add($"Satış tamamlandı! Toplam: ₺{_total:F2}", Severity.Success);
            ClearCart();
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Satış hatası: {ex.Message}", Severity.Error);
        }
        finally
        {
            _processing = false;
        }
    }

    private class CartItem
    {
        public Guid ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int DiscountPercent { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice * (1 - ((decimal)DiscountPercent / 100));
    }

    private record CustomerDto(Guid Id, string Name, string Phone);
}
