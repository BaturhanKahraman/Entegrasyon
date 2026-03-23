using Entegrasyon.Entity.Invoices;
using Entegrasyon.Entity.Orders;

namespace Entegrasyon.Business.Concrete.Trendyol.EFatura;

/// <summary>
/// Order entity'sinden Trendyol e-Faturam API request body'si olusturur.
/// Tutarlar kurus cinsine cevirilir (decimal * 100 → long).
/// </summary>
public sealed class TrendyolEFaturaInvoiceBuilder
{
    /// <summary>
    /// Decimal tutari kurus'a cevirir. 114.55 → 11455
    /// </summary>
    public static long ToKurus(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

    /// <summary>
    /// KDV dahil tutardan KDV tutarini hesaplar.
    /// Ornek: taxInclusiveAmount=11455 kurus, vatRate=18 → taxAmount=1746 kurus
    /// Formula: taxAmount = taxInclusiveAmount - (taxInclusiveAmount * 100 / (100 + vatRate))
    /// </summary>
    public static long CalculateTaxAmount(long taxInclusiveAmountKurus, int vatRate)
    {
        if (vatRate <= 0) return 0;
        var taxExclusive = (long)Math.Round(
            (double)taxInclusiveAmountKurus * 100 / (100 + vatRate),
            MidpointRounding.AwayFromZero);
        return taxInclusiveAmountKurus - taxExclusive;
    }

    /// <summary>
    /// Order + OrderItems'dan e-fatura/e-arsiv request body olusturur.
    /// </summary>
    public EFaturaCreateRequest BuildFromOrder(
        Order order,
        int companyId,
        int userId,
        bool isEInvoice,
        string? targetAlias = null)
    {
        var invoiceLines = new List<EFaturaInvoiceLine>();
        long totalTaxAmount = 0;
        long totalTaxExclusive = 0;
        long totalPayable = 0;

        // KDV oranina gore grupla — subTotalTaxes icin
        var taxGroups = new Dictionary<int, (long taxableAmount, long taxAmount)>();

        foreach (var item in order.OrderItems)
        {
            var vatRate = (int)(item.VatRate ?? 0);
            var lineTotal = ToKurus(item.UnitPrice * item.Quantity);
            var discount = ToKurus(item.Discount ?? 0);
            var lineTotalAfterDiscount = lineTotal - discount;

            var lineTaxAmount = CalculateTaxAmount(lineTotalAfterDiscount, vatRate);
            var lineTaxableAmount = lineTotalAfterDiscount - lineTaxAmount;

            var lineTotalTax = new EFaturaTotalTax(
                lineTaxAmount,
                [new EFaturaSubTotalTax(lineTaxableAmount, lineTaxAmount, "KDV", vatRate)]);

            invoiceLines.Add(new EFaturaInvoiceLine
            {
                Quantity = item.Quantity,
                TotalAmount = lineTotalAfterDiscount,
                TaxAmount = lineTaxAmount,
                TaxableAmount = lineTaxableAmount,
                TaxPercent = vatRate,
                ItemName = item.Barcode ?? item.MerchantSku ?? $"Urun-{item.Id}",
                UnitPriceAmount = ToKurus(item.UnitPrice),
                TotalDiscountAmount = discount,
                TotalTax = lineTotalTax
            });

            totalTaxAmount += lineTaxAmount;
            totalTaxExclusive += lineTaxableAmount;
            totalPayable += lineTotalAfterDiscount;

            // Grup toplam
            if (!taxGroups.ContainsKey(vatRate))
                taxGroups[vatRate] = (0, 0);
            var (existingTaxable, existingTax) = taxGroups[vatRate];
            taxGroups[vatRate] = (existingTaxable + lineTaxableAmount, existingTax + lineTaxAmount);
        }

        var subTotalTaxes = taxGroups.Select(g =>
            new EFaturaSubTotalTax(g.Value.taxableAmount, g.Value.taxAmount, "KDV", g.Key)).ToList();

        var invoiceType = isEInvoice ? "TEMELFATURA" : "EARSIVFATURA";
        var orderDate = order.OrderDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        return new EFaturaCreateRequest
        {
            CompanyId = companyId,
            UserId = userId,
            LocalReferenceId = $"ORDER-{order.OrderNumber ?? order.Id.ToString()}",
            IssuedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            RecipientInfo = BuildRecipientInfo(order),
            InvoiceInfo = new EFaturaInvoiceInfo(invoiceType, "SATIS"),
            InvoiceLines = invoiceLines,
            TotalTax = new EFaturaTotalTax(totalTaxAmount, subTotalTaxes),
            InvoiceTotal = new EFaturaInvoiceTotal(
                LineExtensionAmount: totalTaxExclusive,
                TaxExclusiveAmount: totalTaxExclusive,
                TaxInclusiveAmount: totalPayable,
                PayableAmount: totalPayable,
                AllowanceTotalAmount: 0),
            PaymentInfo = new EFaturaPaymentInfo(
                PurchaseUrl: null,
                PaymentMeans: "CREDIT_CARD",
                PaymentDate: orderDate),
            DeliveryInfo = new EFaturaDeliveryInfo(
                CarrierTaxId: null,
                CarrierName: order.CargoProviderName,
                SentAt: DateTimeOffset.UtcNow.ToString("yyyy-MM-dd")),
            OrderInfo = new EFaturaOrderInfo(
                OrderId: order.OrderNumber ?? order.ShipmentPackageId?.ToString(),
                OrderDate: orderDate),
            TargetAlias = isEInvoice ? targetAlias : null
        };
    }

    private static EFaturaRecipientInfo BuildRecipientInfo(Order order)
    {
        return new EFaturaRecipientInfo(
            TaxId: null, // Fatura adresi uzerinden alinacak — TODO: Order entity'ye TaxId eklenmeli
            CountryCode: "TR",
            City: order.BillingAddress?.City,
            District: order.BillingAddress?.County,
            Address: order.BillingAddress?.FullAddress,
            PostalCode: order.BillingAddress?.ZipCode,
            Phone: null,
            Email: order.CustomerEmail,
            Name: order.CustomerFirstName,
            Surname: order.CustomerLastName,
            TaxOffice: null);
    }
}
