using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Sales;
using Shared.Entity;
using Shared.Helpers;
using Shared.Results;
using System.Linq.Expressions;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.DiscountVouchers;
using Entegrasyon.Entity.Logs;

namespace Entegrasyon.Business.Concrete;

public class DiscountVoucherManager
{
    private readonly IDiscountVoucherDal _discountVoucherDal;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly FluentValidator _fluentValidator;
    private const int CodeLength = 6;

    public DiscountVoucherManager(ApplicationLogManager applicationLogManager, IDiscountVoucherDal discountVoucherDal,
        IRandomGenerator randomGenerator, FluentValidator fluentValidator)
    {
        _applicationLogManager = applicationLogManager;
        _discountVoucherDal = discountVoucherDal;
        _randomGenerator = randomGenerator;
        _fluentValidator = fluentValidator;
    }

    public async Task<bool> CodeExits(string code) =>
        await _discountVoucherDal.Exists(x => string.Equals(x.Code, code));


    public async Task<IDataResult<string>> CreateDiscountVoucher(CreateDiscountVoucherDto dto)
    {
        await _applicationLogManager.AddLog("İndirim kodu oluşturulma isteği geldi.", LogType.DiscountVoucher,
            LogAction.Add,
            dto);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        //bl 
        
        var discountVoucher = new DiscountVoucher
            { Amount = dto.Amount, ExpiringDate = dto.ExpiringDay, CustomerId = dto.CustomerId };
        string code = _randomGenerator.GetRandomCode(CodeLength, true, true, false);
        while (await CodeExits(code))
            code = _randomGenerator.GetRandomCode(CodeLength, true, true, false);
        discountVoucher.Code = code;
        discountVoucher.CustomerId = dto.CustomerId;
        await _discountVoucherDal.AddAsync(discountVoucher);
        await _applicationLogManager.AddLog($"İndirim kodu başarı ile oluşturuldu. {code}", LogType.DiscountVoucher,
            LogAction.Add);
        return new SuccessDataResult<string>(discountVoucher.Code);
    }

    public async Task<IResult> MakePassiveDiscountVouchers(IEnumerable<int> voucherIds)
    {
        await _applicationLogManager.AddLog("İndirim kodları pasife çekiliyor.", LogType.DiscountVoucher,
            LogAction.Update);
        await _discountVoucherDal.ChangeStatus(voucherIds);
        await _applicationLogManager.AddLog("İndirim kodları pasife çekildi.", LogType.DiscountVoucher,
            LogAction.Update);
        return new SuccessResult("Tüm indirim kuponları pasif hale getirildi.");
    }

    public async Task<IResult> MakeActiveDiscountVouchers(IEnumerable<int> voucherIds)
    {
        await _applicationLogManager.AddLog("İndirim kodları aktife çekiliyor.", LogType.DiscountVoucher,
            LogAction.Update);
        await _discountVoucherDal.ChangeStatus(voucherIds, true);
        await _applicationLogManager.AddLog("İndirim kodları pasife çekildi.", LogType.DiscountVoucher,
            LogAction.Update);
        return new SuccessResult("Tüm indirim kuponları aktif hale getirildi.");
    }

    public async Task<IDataResult<Pageable<DiscountVoucherDto>>> GetDiscountVouchers(int pageIndex, int pagesize,
        string searchParam = null)
    {
        var orderTuples = new List<(string, string)>
        {
            new("CreatedAt", "desc")
        };
        Expression<Func<DiscountVoucher, bool>> expression = !string.IsNullOrEmpty(searchParam)
            ? x => 
                string.Equals(x.Code, searchParam) || x.Customer.FullName.Contains(searchParam)
            : null;

        var result = await _discountVoucherDal.GetPaginatedTransformedEntities(pageIndex, pagesize, d =>
            new DiscountVoucherDto(d.Id, d.Code, d.Percentage, d.Amount, d.ExpiringDate.Value, d.Customer.FullName,
                d.Customer.PhoneNumber,
                d.Customer.Discriminator,
                (d.Customer as RetailCustomer).NationalIdentity,
                (d.Customer as CorporateCustomer).TaxNumber
            ), orderTuples, expression);
        return new SuccessDataResult<Pageable<DiscountVoucherDto>>(result);
    }

    public async Task<IResult> CheckVoucherValid(string code)
    {
        var discountVoucher = await _discountVoucherDal.GetAsync(x => string.Equals(x.Code, code));
        if (discountVoucher == null)
            return new ErrorResult($"{code} kodunda herhangi bir kupon bulunamamıştır.");
        if (discountVoucher.ExpiringDate.HasValue && discountVoucher.ExpiringDate.Value.Day < DateTimeOffset.Now.Day)
            return new ErrorResult(
                $"{code} kodundaki kuponun tarihi geçmiştir. Son tarih: {discountVoucher.ExpiringDate.Value:dd-mm-yyyy}");
        return new SuccessResult();
    }
}