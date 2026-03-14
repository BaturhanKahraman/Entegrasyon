namespace Entegrasyon.PrintAgent.Contracts.Labels;

public record ShelfLabelData(
    string Barcode,
    string ProductTitle,
    decimal Price,
    string CurrencySymbol = "₺");
