using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Sales;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public sealed class SaleManager : ISaleManager
{
    private readonly IntegrationDbContext _dbContext;
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly IMapper _mapper;
    private readonly IFluentValidator _fluentValidator;
    private readonly IOfficeStockManager _officeStockManager;

    public SaleManager(IntegrationDbContext dbContext, IApplicationLogManager applicationLogManager, IMapper mapper, IFluentValidator fluentValidator, IOfficeStockManager officeStockManager)
    {
        _dbContext = dbContext;
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
        _fluentValidator = fluentValidator;
        _officeStockManager = officeStockManager;
    }

    public async Task<IResult> MakeSale(MakeSaleDto dto)
    {
        await _applicationLogManager.AddLog("Satış yapma isteği geldi.", LogType.Sale, LogAction.Add, dto);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var sale = _mapper.Map<Sale>(dto);
        var decreaseStockResult = await _officeStockManager.DecreaseProductsStock(dto.SaleItems.Select(x =>
            new DecreaseStockDto(x.ProductVariantId, dto.BranchOfficeId, x.Quantity)).ToList());
        if (!decreaseStockResult.Success)
            return decreaseStockResult;
        _dbContext.Sales.Add(sale);
        await _dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("Satış başarı ile tamamlandı.", LogType.Sale, LogAction.Add, dto);
        return new SuccessResult(Messages.SaleSuccess);
    }

    public async Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto)
    {
        var expression = new ExpressionBuilder<Sale>()
            .AddAnd(x => x.CustomerId == dto.CustomerId, dto.CustomerId.HasValue)
            .AddAnd(x => x.CreatedAt >= dto.DateBetweenStart, dto.DateBetweenStart is not null)
            .AddAnd(x => x.CreatedAt <= dto.DateBetweenEnd, dto.DateBetweenStart is not null)
            .AddAnd(x => x.SalePersonId == dto.SalePersonId, dto.SalePersonId != Guid.Empty)
            .Build();

        var query = _dbContext.Sales.AsQueryable();
        if (expression != null)
            query = query.Where(expression);

        int total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(dto.PageIndex * dto.PageSize)
            .Take(dto.PageSize)
            .Select(x => new SaleListDetailDto(
                x.Id, x.CreatedAt,
                x.DiscountVoucherId.HasValue || x.GeneralDiscount > 0 || x.SaleItems.Any(s => s.DiscountPercent > 0),
                x.GeneralDiscount,
                x.SalePerson.Name + ' ' + x.SalePerson.Surname,
                x.Customer.FullName,
                x.SaleItems.Count(),
                x.SaleItems.Sum(si => si.Quantity),
                x.SaleItems.Sum(si => si.UnitPrice - si.UnitPrice * ((decimal)si.DiscountPercent / 100))))
            .ToListAsync();

        return new SuccessDataResult<Pageable<SaleListDetailDto>>(new Pageable<SaleListDetailDto>(items, dto.PageIndex, dto.PageSize, total));
    }
}
