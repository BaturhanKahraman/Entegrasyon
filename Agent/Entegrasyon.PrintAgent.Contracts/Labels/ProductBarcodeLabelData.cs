namespace Entegrasyon.PrintAgent.Contracts.Labels;

public record ProductBarcodeLabelData(
    string Barcode,
    string ProductTitle,
    string VariantInfo,
    decimal Price,
    string CurrencySymbol = "₺");
