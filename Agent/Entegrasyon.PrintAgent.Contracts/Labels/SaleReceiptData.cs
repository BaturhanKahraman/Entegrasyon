namespace Entegrasyon.PrintAgent.Contracts.Labels;

public record SaleReceiptData(
    string StoreName,
    string StoreAddress,
    string TaxId,
    List<ReceiptLineItem> Items,
    decimal SubTotal,
    decimal Discount,
    decimal Total,
    string PaymentMethod,
    DateTimeOffset SaleDate,
    string CashierName,
    string CustomerName);

public record ReceiptLineItem(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
