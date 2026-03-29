using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public sealed class SaleManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    SaleMapper mapper,
    IFluentValidator fluentValidator,
    IOfficeStockManager officeStockManager) : ISaleManager
{
    public async Task<IResult> MakeSale(MakeSaleDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();
        await applicationLogManager.AddLog("Satış yapma isteği geldi.", LogType.Sale, LogAction.Add, dto);
        await fluentValidator.ValidateAndThrowAsync(dto);
        var sale = mapper.MapToEntity(dto);

        // Atomic stok düşme — her ürün için ayrı ayrı
        foreach (var item in dto.SaleItems)
        {
            var stockResult = await officeStockManager.DecreaseStockAtomicAsync(
                dto.BranchOfficeId, item.ProductVariantId, item.Quantity,
                StockMovementType.Sale, "Sale");

            if (!stockResult.Success)
                return new ErrorResult(stockResult.Message!);
        }

        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Satış başarı ile tamamlandı.", LogType.Sale, LogAction.Add, dto);
        return new SuccessResult(Messages.SaleSuccess);
    }

    public async Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var expression = new ExpressionBuilder<Sale>()
            .AddAnd(x => x.CustomerId == dto.CustomerId, dto.CustomerId.HasValue)
            .AddAnd(x => x.CreatedAt >= dto.DateBetweenStart, dto.DateBetweenStart is not null)
            .AddAnd(x => x.CreatedAt <= dto.DateBetweenEnd, dto.DateBetweenStart is not null)
            .AddAnd(x => x.SalePersonId == dto.SalePersonId, dto.SalePersonId != Guid.Empty)
            .Build();

        var query = dbContext.Sales.AsQueryable();
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
                x.Customer!.FullName!,
                x.SaleItems.Count(),
                x.SaleItems.Sum(si => si.Quantity),
                x.SaleItems.Sum(si => si.UnitPrice - si.UnitPrice * ((decimal)si.DiscountPercent / 100))))
            .ToListAsync();

        return new SuccessDataResult<Pageable<SaleListDetailDto>>(new Pageable<SaleListDetailDto>(items, dto.PageIndex, dto.PageSize, total));
    }
}
