namespace Entegrasyon.MVC.Features.Reports.ViewModels;

/// <summary>
/// Stok uyarı sayfasından toplu transfer talebi oluşturma formu.
/// Seçili her satır kendi kaynak şubesini (alert satırından) taşır; hedef şube tek (formdan).
/// Kaynak şube başına gruplanıp ayrı StockTransferRequest açılır
/// (her talep tek source/target alır).
/// </summary>
public sealed class StockAlertBulkTransferVm
{
    /// <summary>Tüm seçili satırların aktarılacağı tek hedef şube.</summary>
    public int TargetBranchOfficeId { get; set; }

    /// <summary>Seçili alert satırları (varyant + kaynak şube + miktar).</summary>
    public List<StockAlertBulkTransferLine> Lines { get; set; } = [];
}

public sealed class StockAlertBulkTransferLine
{
    public Guid ProductVariantId { get; set; }

    /// <summary>Alert satırının ait olduğu kaynak şube (transfer kaynağı).</summary>
    public int SourceBranchOfficeId { get; set; }

    /// <summary>Aktarılacak miktar (varsayılan: SuggestedOrderQuantity).</summary>
    public int Quantity { get; set; }
}
