namespace Entegrasyon.Business.Constants;

/// <summary>
/// Business katmanında claim string'lerinin tekrarlanmasını önleyen mirror sabitleri.
/// Business → ApplicationBootstrap dairesel referans olmaması için AppPermissions'ı doğrudan import edemiyoruz.
/// PermissionConstantsMirrorTests bu iki listenin senkron kaldığını doğrular.
/// </summary>
public static class PermissionConstants
{
    // Branch office delete workflow
    public const string BranchOfficeDelete = "Permissions.BranchOffices.Delete";
    public const string StockOfficeDeleteApprove = "Permissions.StockOffice.Delete.Approve";

    // Stock transfer workflow
    public const string StockTransfer = "Permissions.Stock.Transfer";
    public const string StockTransferApprove = "Permissions.Stock.Transfer.Approve";

    /// <summary>
    /// E-ticaret / pazaryeri feature gate'i. Tenant'ın aktif paketi bu izni içermiyorsa
    /// pazaryeri durum kartları ve aktivite timeline'ı çalıştırılmaz (sadece fiziksel mağaza akışı).
    /// </summary>
    public const string MarketplaceView = "Permissions.Marketplace.View";
}
