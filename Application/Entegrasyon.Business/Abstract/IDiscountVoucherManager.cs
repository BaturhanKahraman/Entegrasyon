using Entegrasyon.Entity.Dtos.DiscountVouchers;
using Shared.Entity;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface IDiscountVoucherManager
{
    Task<bool> CodeExits(string code);
    Task<IDataResult<string>> CreateDiscountVoucher(CreateDiscountVoucherDto dto);
    Task<IResult> MakePassiveDiscountVouchers(IEnumerable<int> voucherIds);
    Task<IResult> MakeActiveDiscountVouchers(IEnumerable<int> voucherIds);
    Task<IDataResult<Pageable<DiscountVoucherDto>>> GetDiscountVouchers(int pageIndex, int pagesize, string code = null);
    Task<IResult> CheckVoucherValid(string code);
}
