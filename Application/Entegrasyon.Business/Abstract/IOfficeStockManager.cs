using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IOfficeStockManager
{
    Task<IResult> AddOfficeStocks(IEnumerable<BranchOfficeStock> stocks);
    Task<IResult> CheckIfOfficeExists(IEnumerable<int> officesIds);
    Task UpdateStock(int branchOfficeId, Guid productVariantId, int stock);
    Task<IResult> DecreaseProductStock(Guid id, int stockNumber, int branchId, bool overrideStockStatus = false);
    Task<IResult> DecreaseProductsStock(List<DecreaseStockDto> dtos, bool overrideStockStatus = false);

    /// <summary>
    /// Atomic stok düşme — stok yetersizse hata döner. POS satış vb. için.
    /// ExecuteUpdateAsync ile tek SQL, race condition imkansız.
    /// </summary>
    Task<IDataResult<StockMovement>> DecreaseStockAtomicAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null);

    /// <summary>
    /// Zorla stok düşme — stok yetersiz olsa bile düşer, negatife izin verir.
    /// Marketplace satışları için (Trendyol zaten sattıysa geri alınamaz).
    /// </summary>
    Task<IDataResult<StockMovement>> ForceDecreaseStockAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null);

    /// <summary>
    /// Atomic stok artirma — SoldQuantity azaltilir, stok geri verilir.
    /// Odeme başarısız oldugunda veya iptal/iade senaryolarinda kullanilir.
    /// </summary>
    Task<IDataResult<StockMovement>> IncreaseStockAtomicAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null);

    /// <summary>
    /// Depolar arasi stok transfer. Tek transaction icinde kaynak azaltilir, hedef arttirilir.
    /// StockMovement kayitlari her iki taraf icin olusturulur.
    /// </summary>
    Task<IDataResult<StockTransferResultDto>> TransferStockAsync(
        int sourceBranchId, int targetBranchId, List<TransferItemDto> items);

    /// <summary>
    /// Tüm stok hareketlerini sayfalanmış olarak listeler. Branch ve tip filtrelemesi desteklenir.
    /// </summary>
    Task<IDataResult<Pageable<StockMovementViewDto>>> GetStockMovementsAsync(
        int pageIndex = 0, int pageSize = 50,
        int? branchOfficeId = null, StockMovementType? type = null);

    /// <summary>
    /// Belirli bir şube ve varyant için mevcut stoku döner. Kayıt yoksa 0.
    /// POS stok aşım kontrolü için kullanılır.
    /// </summary>
    Task<int> GetAvailableStockAsync(int branchOfficeId, Guid productVariantId);

    /// <summary>
    /// Stok değişikliği event'ini marketplace sync kanalına yayınlar.
    /// SaleManager gibi inline stok değişikliği yapan manager'lar bu metodu çağırır.
    /// </summary>
    Task PublishStockChangedEventAsync(Guid productVariantId, Guid productId);
}
