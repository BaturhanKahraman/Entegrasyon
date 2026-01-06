using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Sales;
using MapsterMapper;
using Shared.Entity;
using Shared.Expressions;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public sealed class SaleManager : ISaleManager
{
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly IMapper _mapper;
    private readonly IFluentValidator _fluentValidator;
    private readonly OfficeStockManager _officeStockManager;
    private readonly ISaleDal _saleDal;
    public SaleManager(IApplicationLogManager applicationLogManager, IMapper mapper, IFluentValidator fluentValidator, OfficeStockManager officeStockManager, ISaleDal saleDal)
    {
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
        _fluentValidator = fluentValidator;
        _officeStockManager = officeStockManager;
        _saleDal = saleDal;
    }

    public async Task<IResult> MakeSale(MakeSaleDto dto)
    {
        await _applicationLogManager.AddLog("Satış yapma isteği geldi.", LogType.Sale, LogAction.Add, dto);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        //business logic control
        //mapper koy
        var sale = _mapper.Map<Sale>(dto);

        var decreaseStockResult = await _officeStockManager.DecreaseProductsStock(dto.SaleItems.Select(x =>
            new DecreaseStockDto(x.ProductVariantId,dto.BranchOfficeId,x.Quantity)).ToList());
        if (!decreaseStockResult.Success)
        {
            //await _applicationLogManager.AddLog("Satış ", LogType.Sale, LogAction.Add, dto);
            return decreaseStockResult;
        }
        await _saleDal.AddAsync(sale);
        await _applicationLogManager.AddLog("Satış başarı ile tamamlandı.", LogType.Sale, LogAction.Add, dto);
        return new SuccessResult(Messages.SaleSuccess);
    }

    public async Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto)
    {
        var orderBy = new List<(string, string)>(1) { new("CreatedAt", "desc") };
        var expression = new ExpressionBuilder<Sale>()
                            .AddAnd(x => x.CustomerId == dto.CustomerId, dto.CustomerId.HasValue)
                            .AddAnd(x=>x.CreatedAt>=dto.DateBetweenStart,dto.DateBetweenStart is not null)
                            .AddAnd(x=>x.CreatedAt<=dto.DateBetweenEnd,dto.DateBetweenStart is not null)
                            .AddAnd(x=>x.SalePersonId == dto.SalePersonId,dto.SalePersonId != Guid.Empty )
                            .Build();

        var result = await _saleDal.GetPaginatedTransformedEntities(dto.PageIndex, dto.PageSize, x=>new SaleListDetailDto(
            x.Id,
            x.CreatedAt,
            x.DiscountVoucherId.HasValue || x.GeneralDiscount>0 || x.SaleItems.Any(s=>s.DiscountPercent>0),
            x.GeneralDiscount,
            x.SalePerson.Name + ' ' + x.SalePerson.Surname,
            x.Customer.FullName,
            x.SaleItems.Count(),
            x.SaleItems.Sum(si=>si.Quantity),
            x.SaleItems.Sum(si=>si.UnitPrice- si.UnitPrice * ((decimal)si.DiscountPercent / 100))
            ), orderBy,expression);
        return new SuccessDataResult<Pageable<SaleListDetailDto>>(result);
    }


}
