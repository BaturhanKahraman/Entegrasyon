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
}
