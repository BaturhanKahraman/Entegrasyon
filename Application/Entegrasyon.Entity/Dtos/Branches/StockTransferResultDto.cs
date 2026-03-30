using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Dtos.Branches;

public record StockTransferResultDto(
    int TransferredCount,
    List<StockMovement> SourceMovements,
    List<StockMovement> TargetMovements);
