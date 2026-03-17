using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
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
}
