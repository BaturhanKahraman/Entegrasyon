namespace Entegrasyon.Entity.Dtos.Receipts;

public sealed record UpdateReceiptTemplateDto(
    string ThermalJson,
    string A4Json,
    int LogoWidthPx,
    string StoreName,
    string StoreAddress,
    string StorePhone);
