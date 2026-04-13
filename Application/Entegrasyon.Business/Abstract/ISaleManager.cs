using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;

namespace Entegrasyon.Business.Abstract;

public interface ISaleManager
{
    Task<IResult> MakeSale(MakeSaleDto dto);
    Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto);
    Task<IDataResult<SaleDetailDto>> GetSaleDetailAsync(Guid saleId);
    Task<IResult> CancelSaleAsync(Guid saleId, Guid cancelledByUserId);
    Task<IDataResult<SaleSummaryDto>> GetSalesSummaryAsync(SalePageableDto dto);
}
