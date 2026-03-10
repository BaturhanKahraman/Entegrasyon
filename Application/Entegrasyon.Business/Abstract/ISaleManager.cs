using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;

namespace Entegrasyon.Business.Abstract;

public interface ISaleManager
{
    Task<IResult> MakeSale(MakeSaleDto dto);
    Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto);
}
