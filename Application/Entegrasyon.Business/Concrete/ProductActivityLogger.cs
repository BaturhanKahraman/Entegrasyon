using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class ProductActivityLogger(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IProductActivityLogger
{
    public async Task LogAsync(Guid productId, ProductActivityType activityType, string message,
        ProductActivityStatus status = ProductActivityStatus.Info,
        string? detail = null, string? marketplaceName = null, string? referenceId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        dbContext.ProductActivityLogs.Add(new ProductActivityLog
        {
            ProductId = productId,
            ActivityType = activityType,
            Message = message,
            Detail = detail,
            Status = status,
            MarketplaceName = marketplaceName,
            ReferenceId = referenceId
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(Guid productId, int limit = 50)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var logs = await dbContext.ProductActivityLogs
            .AsNoTracking()
            .Where(l => l.ProductId == productId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return new SuccessDataResult<List<ProductActivityLog>>(logs);
    }

    public async Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(
        Guid productId, ProductActivityTimelineFilter filter, int pageSize = 20)
    {
        if (pageSize <= 0) pageSize = 20;

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.ProductActivityLogs
            .AsNoTracking()
            .Where(l => l.ProductId == productId);

        // Keyset (cursor) pagination — tie-break (CreatedAt, Id) ile.
        // Tek SaveChanges'te yazılan loglar aynı CreatedAt'i paylaşabilir; salt `CreatedAt < cursor`
        // sayfa sınırına denk gelen aynı-timestamp satırları sessizce DÜŞÜRÜR. Id ile ikincil sıralama
        // bu kaybı önler.
        if (filter.Cursor is { } cursor)
        {
            if (filter.CursorId is { } cursorId)
                query = query.Where(l => l.CreatedAt < cursor || (l.CreatedAt == cursor && l.Id < cursorId));
            else
                query = query.Where(l => l.CreatedAt < cursor);
        }

        if (filter.From is { } from)
            query = query.Where(l => l.CreatedAt >= from);

        if (filter.To is { } to)
            query = query.Where(l => l.CreatedAt <= to);

        if (filter.MarketplaceNames is { Count: > 0 } marketplaces)
            query = query.Where(l => l.MarketplaceName != null && marketplaces.Contains(l.MarketplaceName));

        if (filter.ActivityTypes is { Count: > 0 } activityTypes)
            query = query.Where(l => activityTypes.Contains(l.ActivityType));

        if (filter.Statuses is { Count: > 0 } statuses)
            query = query.Where(l => statuses.Contains(l.Status));

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Take(pageSize)
            .ToListAsync();

        return new SuccessDataResult<List<ProductActivityLog>>(logs);
    }
}
