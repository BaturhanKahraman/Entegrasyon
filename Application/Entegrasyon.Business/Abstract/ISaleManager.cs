using Entegrasyon.Entity.Dtos.Sale;
using Shared.DTO;
using Shared.Entity;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface ISaleManager
{
    Task<IResult> MakeSale(MakeSaleDto dto);
    Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto);
}
