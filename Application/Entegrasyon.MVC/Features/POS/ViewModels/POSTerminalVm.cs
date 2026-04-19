using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Features.POS;

namespace Entegrasyon.MVC.Features.POS.ViewModels;

public class POSTerminalVm
{
    public bool HasActiveSession { get; set; }
    public long SessionId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public string CashierName { get; set; } = "";
    public string? TerminalId { get; set; }
    public decimal OpeningCash { get; set; }
    public POSSummaryDto? Summary { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}

public class POSCartVm
{
    public List<POSCartItemVm> Items { get; set; } = [];

    // Genel (sepet) indirimi state
    public string? GeneralDiscountType { get; set; }   // "percent" | "amount" | null
    public decimal GeneralDiscountValue { get; set; }
    public int? GeneralDiscountReasonId { get; set; }
    public string? GeneralDiscountReasonName { get; set; }
    public string? GeneralDiscountReasonNote { get; set; }

    // Kalem indirimi uygulanmış alt toplam (KDV-dahil)
    public decimal SubtotalAfterLineDiscount => Items.Sum(i => i.LineTotalWithVat);

    // KDV hariç ara toplam (kalem indirimi uygulanmış) — geriye dönük
    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    // KDV toplamı (kalem indirimi uygulanmış)
    public decimal VatTotal => Items.Sum(i => i.VatAmount);

    // Genel indirim uygulanmamış grand total (kalem indirimli, KDV-dahil)
    public decimal GrandTotalBeforeGeneralDiscount => SubtotalAfterLineDiscount;

    public decimal GeneralDiscountAmount
    {
        get
        {
            if (string.IsNullOrEmpty(GeneralDiscountType) || GeneralDiscountValue <= 0m)
                return 0m;
            if (SubtotalAfterLineDiscount <= 0m) return 0m;

            return GeneralDiscountType switch
            {
                "percent" => Math.Round(
                    SubtotalAfterLineDiscount * Math.Min(GeneralDiscountValue, 100m) / 100m, 2),
                "amount"  => Math.Round(Math.Min(GeneralDiscountValue, SubtotalAfterLineDiscount), 2),
                _         => 0m
            };
        }
    }

    public decimal GrandTotal => GrandTotalBeforeGeneralDiscount - GeneralDiscountAmount;
    public int TotalItems => Items.Sum(i => i.Quantity);
    public bool HasGeneralDiscount => GeneralDiscountAmount > 0m;

    // Sepet (genel) indirimi pro-rata dağıtıldıktan sonra KDV-hariç net subtotal ve KDV toplamı.
    // Why: Subtotal/VatTotal yalnızca kalem indirimini içerir → sepet indirimi varsa KDV gösterimi
    // GrandTotal ile tutarsız olur. CompleteSale'deki dağıtımla aynı mantık burada da uygulanır.
    public (decimal NetSubtotal, decimal VatTotal) ComputeNetTotalsAfterAllDiscounts()
    {
        if (Items.Count == 0) return (0m, 0m);
        if (!HasGeneralDiscount) return (Subtotal, VatTotal);

        var lines = Items.Select(c => new CartLine(
            UnitPrice: c.UnitPrice,
            Quantity: c.Quantity,
            VatRate: c.VatRate,
            ExistingLineDiscountAmount: c.DiscountAmount
                ?? Math.Round(c.LineGross * (decimal)c.DiscountPercent / 100m, 2)
        )).ToList();

        var dist = CartDiscountDistributor.Distribute(lines, GeneralDiscountAmount);

        decimal netSubtotal = 0m, vatTotal = 0m;
        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var lineNet = item.LineTotal - dist.PerLineNetShare[i];
            if (lineNet < 0m) lineNet = 0m;
            netSubtotal += lineNet;
            vatTotal += Math.Round(lineNet * item.VatRate / 100m, 2);
        }
        return (netSubtotal, vatTotal);
    }
}

public class POSCartItemVm
{
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public string Title { get; set; } = "";
    public string Barcode { get; set; } = "";
    // Varyant display name — ör. "Sarı XL". Sepette hangi varyant seçildiğini göstermek için.
    public string DisplayName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }       // Ürün zaten indirimli mi? ListPrice>UnitPrice ise evet
    public decimal VatRate { get; set; }
    public int Quantity { get; set; } = 1;
    public int AvailableStock { get; set; }

    // POS'a özel kalem indirimi (opsiyonel)
    public double DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int? DiscountReasonId { get; set; }
    public string? DiscountReasonName { get; set; }
    public string? DiscountReasonNote { get; set; }

    public decimal LineGross => UnitPrice * Quantity;
    public decimal DiscountComputed => DiscountAmount ?? Math.Round(LineGross * (decimal)DiscountPercent / 100m, 2);
    public decimal LineTotal => LineGross - DiscountComputed;
    public decimal VatAmount => LineTotal * VatRate / 100m;
    public decimal UnitPriceWithVat => Math.Round(UnitPrice * (1 + VatRate / 100m), 2);
    public decimal LineTotalWithVat => Math.Round(LineTotal * (1 + VatRate / 100m), 2);
    public bool HasDiscount => DiscountPercent > 0 || (DiscountAmount.HasValue && DiscountAmount.Value > 0);
    public bool ProductAlreadyDiscounted => ListPrice > UnitPrice;
}

public class POSSearchResultsVm
{
    public List<POSSearchItemVm> Products { get; set; } = [];
}

public class POSSearchItemVm
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = "";
    public string StockCode { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int CurrentStock { get; set; }
}

public class POSPaymentDialogVm
{
    public long SessionId { get; set; }
    public decimal SubtotalBeforeGeneralDiscount { get; set; }  // kalem indirimli, KDV-dahil
    public decimal GeneralDiscountAmount { get; set; }
    public string? GeneralDiscountReasonName { get; set; }
    public decimal Subtotal { get; set; }        // KDV hariç (mevcut davranış)
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }      // genel indirim uygulanmış
    public int ItemCount { get; set; }
    public List<PaymentMethodDefinition> PaymentMethods { get; set; } = [];
    public string SubmitToken { get; set; } = "";
    public bool HasGeneralDiscount => GeneralDiscountAmount > 0m;
}

public class POSCloseSessionDialogVm
{
    public long SessionId { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public int TransactionCount { get; set; }
    public decimal ExpectedCash { get; set; }
}

public class POSCartDiscountDialogVm
{
    public decimal SubtotalAfterLineDiscount { get; set; }
    public string? CurrentType { get; set; }
    public decimal CurrentValue { get; set; }
    public int? CurrentReasonId { get; set; }
    public string? CurrentNote { get; set; }
    public List<Entegrasyon.Entity.Sales.DiscountReason> Reasons { get; set; } = [];
}

public class POSLineDiscountDialogVm
{
    public Guid VariantId { get; set; }
    public string ProductTitle { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineGross { get; set; }
    public bool AlreadyDiscounted { get; set; }
    public double CurrentPercent { get; set; }
    public decimal? CurrentAmount { get; set; }
    public int? CurrentReasonId { get; set; }
    public string? CurrentNote { get; set; }
    public List<Entegrasyon.Entity.Sales.DiscountReason> Reasons { get; set; } = [];
}
