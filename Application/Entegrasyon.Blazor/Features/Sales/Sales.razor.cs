namespace Entegrasyon.Blazor.Features.Sales;

using Entegrasyon.Blazor.Features.Printing;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Label;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

public partial class Sales : IDisposable
{
    [Inject]
    private IDialogService? DialogService { get; set; }

    [Inject]
    private ISnackbar? Snackbar { get; set; }

    [Inject]
    private ILabelService? LabelService { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }

    [SupplyParameterFromQuery(Name = "barcode")]
    public string? BarcodeFromQuery { get; set; }

    private string barcodeSearch = string.Empty;
    private string paymentMethod = "Nakit";
    private bool processing;
    private readonly List<CartItem> cartItems = [];
    private CustomerDto? selectedCustomer;

    private decimal Subtotal => cartItems.Sum(x => x.Quantity * x.UnitPrice);
    private decimal TotalDiscount => cartItems.Sum(x => (x.Quantity * x.UnitPrice) * ((decimal)x.DiscountPercent / 100));
    private decimal SubtotalAfterDiscount => Subtotal - TotalDiscount;
    private decimal Tax => cartItems.Sum(x =>
    {
        var lineNet = x.Quantity * x.UnitPrice * (1 - (decimal)x.DiscountPercent / 100);
        return KdvCalculator.FromExclusive(lineNet, x.VatRate).KdvAmount;
    });

    private IEnumerable<(decimal Rate, decimal Amount)> TaxBreakdown =>
        cartItems.GroupBy(x => x.VatRate)
            .Select(g => (Rate: g.Key, Amount: g.Sum(x =>
                KdvCalculator.FromExclusive(
                    x.Quantity * x.UnitPrice * (1 - (decimal)x.DiscountPercent / 100),
                    g.Key).KdvAmount)))
            .Where(x => x.Amount > 0)
            .OrderBy(x => x.Rate);
    private decimal Total => SubtotalAfterDiscount + Tax;

    protected override async Task OnInitializedAsync()
    {
        NavigationManager!.LocationChanged += OnLocationChanged;
        await ProcessBarcodeFromQuery();
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        _ = InvokeAsync(async () =>
        {
            var uri = new Uri(e.Location);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var barcode = query["barcode"];
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                await SearchAndAddProduct(barcode);
                StateHasChanged();
            }
        });
    }

    private async Task ProcessBarcodeFromQuery()
    {
        if (!string.IsNullOrWhiteSpace(BarcodeFromQuery))
        {
            await SearchAndAddProduct(BarcodeFromQuery);
            BarcodeFromQuery = null;
        }
    }

    public void Dispose()
    {
        NavigationManager!.LocationChanged -= OnLocationChanged;
    }

    private async Task HandleBarcodeSearch(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(barcodeSearch))
        {
            await SearchAndAddProduct(barcodeSearch);
            barcodeSearch = string.Empty;
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
        var existingItem = cartItems.FirstOrDefault(x => x.ProductVariantId == variantId);

        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            cartItems.Add(new CartItem
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
        cartItems.Remove(item);
        RecalculateTotal();
        Snackbar?.Add("Ürün sepetten çıkarıldı", Severity.Info);
    }

    private void RecalculateTotal()
    {
        // Total is calculated via property getter, no explicit re-render needed
    }

    private void ClearCart()
    {
        cartItems.Clear();
        selectedCustomer = null;
        Snackbar?.Add("Sepet temizlendi", Severity.Info);
    }

    private async Task OpenCustomerSearch()
    {
        // TODO: Implement customer search dialog
        Snackbar?.Add("Müşteri arama özelliği yakında gelecek", Severity.Info);
    }

    private void ClearCustomer()
    {
        selectedCustomer = null;
    }

    private async Task CompleteSale()
    {
        if (!cartItems.Any())
        {
            Snackbar?.Add("Sepet boş!", Severity.Warning);
            return;
        }

        processing = true;
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

            // TODO: Implement actual sale via SaleManager

            var confirm = await DialogService!.ShowMessageBox(
                "Satış Tamamlandı",
                $"Satış başarıyla tamamlandı!\n\nToplam: ₺{Total:F2}\nÖdeme: {paymentMethod}\n\nFiş yazdırılsın mı?",
                yesText: "Yazdır",
                cancelText: "Kapat"
            );

            if (confirm == true)
            {
                await PrintReceipt();
            }

            Snackbar?.Add($"Satış tamamlandı! Toplam: ₺{Total:F2}", Severity.Success);
            ClearCart();
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Satış hatası: {ex.Message}", Severity.Error);
        }
        finally
        {
            processing = false;
        }
    }

    private async Task PrintReceipt()
    {
        // Satış henüz gerçek SaleManager ile kaydedilmediğinden,
        // burada fiş verisini manuel oluşturup dialog açıyoruz.
        // SaleManager entegre olduğunda saleId ile LabelService.GenerateSaleReceipt kullanılacak.
        var receiptData = new PrintAgent.Contracts.Labels.SaleReceiptData(
            StoreName: "Mağaza",
            StoreAddress: "",
            TaxId: "",
            Items: cartItems.Select(i => new PrintAgent.Contracts.Labels.ReceiptLineItem(
                i.ProductName, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            SubTotal: Subtotal,
            Discount: TotalDiscount,
            Total: Total,
            PaymentMethod: paymentMethod,
            SaleDate: DateTimeOffset.Now,
            CashierName: "Kasiyer",
            CustomerName: selectedCustomer?.Name);

        var generator = new Business.Labels.EscPosReceiptGenerator();
        var receiptBytes = generator.GenerateSaleReceipt(receiptData);

        var printJob = new PrintJobDto("", receiptBytes, "ESCPOS", "Satış Fişi");

        var parameters = new DialogParameters<PrintDialog>
        {
            { x => x.PrintJob, printJob }
        };

        await DialogService!.ShowAsync<PrintDialog>("Fiş Yazdır", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
    }

    private class CartItem
    {
        public Guid ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int DiscountPercent { get; set; }
        public decimal VatRate { get; set; } = 20;
        public decimal TotalPrice => Quantity * UnitPrice * (1 - ((decimal)DiscountPercent / 100));
    }

    private record CustomerDto(Guid Id, string Name, string Phone);
}
