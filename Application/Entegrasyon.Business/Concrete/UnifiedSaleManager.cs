using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Mappers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Sales.Views;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class UnifiedSaleManager(IntegrationDbContext db) : IUnifiedSaleManager
{
    public async Task<IDataResult<Pageable<UnifiedSaleListItemDto>>> GetPageableAsync(UnifiedSaleFilterDto filter)
    {
        var query = BuildBaseQuery(filter, applyStatus: false);

        List<UnifiedSaleView> allRows;
        if (filter.Status.HasValue)
        {
            allRows = await query.OrderByDescending(x => x.SaleDate).ToListAsync();
            allRows = allRows
                .Where(x => UnifiedSaleStatusMapper.Map(x.RawStatusCode, x.EntityType) == filter.Status.Value)
                .ToList();

            var total = allRows.Count;
            var page = allRows
                .Skip(filter.PageIndex * filter.PageSize)
                .Take(filter.PageSize)
                .Select(MapToDto)
                .ToList();

            return new SuccessDataResult<Pageable<UnifiedSaleListItemDto>>(
                new Pageable<UnifiedSaleListItemDto>(page, filter.PageIndex, filter.PageSize, total));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.SaleDate)
            .Skip(filter.PageIndex * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtoList = items.Select(MapToDto).ToList();

        return new SuccessDataResult<Pageable<UnifiedSaleListItemDto>>(
            new Pageable<UnifiedSaleListItemDto>(dtoList, filter.PageIndex, filter.PageSize, totalCount));
    }

    public async Task<IDataResult<UnifiedSaleSummaryDto>> GetSummaryAsync(UnifiedSaleFilterDto filter)
    {
        var query = BuildBaseQuery(filter, applyStatus: false);
        var rows = await query.ToListAsync();

        if (filter.Status.HasValue)
        {
            rows = rows
                .Where(x => UnifiedSaleStatusMapper.Map(x.RawStatusCode, x.EntityType) == filter.Status.Value)
                .ToList();
        }

        var totalRevenue = rows.Sum(x => x.TotalPrice);
        var count = rows.Count;
        var avg = count == 0 ? 0m : totalRevenue / count;

        var returnCount = rows.Count(x => x.EntityType == 0 &&
            (x.RawStatusCode == (int)SaleStatus.PartialReturn ||
             x.RawStatusCode == (int)SaleStatus.FullReturn));
        var returnRate = count == 0 ? 0d : (double)returnCount / count;

        return new SuccessDataResult<UnifiedSaleSummaryDto>(
            new UnifiedSaleSummaryDto(totalRevenue, count, avg, returnRate));
    }

    public async Task<IDataResult<List<UnifiedSaleSourceCountDto>>> GetSourceCountsAsync(UnifiedSaleFilterDto filter)
    {
        var baseFilter = filter with { Source = null, Status = null };
        var query = BuildBaseQuery(baseFilter, applyStatus: false);

        var grouped = await query
            .GroupBy(x => x.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = grouped.Sum(g => g.Count);
        var list = new List<UnifiedSaleSourceCountDto>
        {
            new(null, "Tümü", total)
        };

        foreach (var g in grouped.OrderBy(x => (int)x.Source))
        {
            list.Add(new UnifiedSaleSourceCountDto(g.Source, g.Source.ToString(), g.Count));
        }

        return new SuccessDataResult<List<UnifiedSaleSourceCountDto>>(list);
    }

    private IQueryable<UnifiedSaleView> BuildBaseQuery(UnifiedSaleFilterDto filter, bool applyStatus)
    {
        var q = db.UnifiedSales.AsNoTracking();

        if (filter.StartDate.HasValue)
            q = q.Where(x => x.SaleDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            q = q.Where(x => x.SaleDate < filter.EndDate.Value);
        if (filter.Source.HasValue)
            q = q.Where(x => x.Source == filter.Source.Value);
        if (filter.CustomerId.HasValue)
            q = q.Where(x => x.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var s = filter.SearchText.Trim().ToLower();
            q = q.Where(x =>
                (x.Number ?? "").ToLower().Contains(s) ||
                (x.CustomerDisplayName ?? "").ToLower().Contains(s));
        }

        return q;
    }

    private static UnifiedSaleListItemDto MapToDto(UnifiedSaleView v)
    {
        var status = UnifiedSaleStatusMapper.Map(v.RawStatusCode, v.EntityType);
        var detailUrl = v.EntityType == 0
            ? $"/sales/sale/{v.Id}"
            : $"/sales/order/{v.Id}";

        return new UnifiedSaleListItemDto
        {
            Id = v.Id,
            EntityType = v.EntityType,
            Source = v.Source,
            Number = v.Number,
            SaleDate = v.SaleDate,
            CustomerDisplayName = v.CustomerDisplayName,
            CustomerSubLine = v.CustomerId.HasValue ? null : "Tekil",
            TotalPrice = v.TotalPrice,
            ItemCount = v.ItemCount,
            Status = status,
            StatusSubLine = v.CargoTrackingNumber,
            DetailUrl = detailUrl
        };
    }
}
