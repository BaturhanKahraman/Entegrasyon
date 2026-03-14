using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ILabelService
{
    Task<IDataResult<PrintJobDto>> GenerateProductLabel(Guid variantId);
    Task<IDataResult<List<PrintJobDto>>> GenerateBulkLabels(List<Guid> variantIds);
    Task<IDataResult<PrintJobDto>> GenerateSaleReceipt(Guid saleId);
}
