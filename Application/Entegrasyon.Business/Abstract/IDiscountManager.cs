using Entegrasyon.Entity.Dtos.Product.Discount;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IDiscountManager
{
    Task<IDataResult<DiscountPreviewDto>> GetDiscountPreviewAsync(Guid productId);
    Task<IDataResult<DiscountResultDto>> ApplyDiscountAsync(ApplyDiscountDto dto);
}
