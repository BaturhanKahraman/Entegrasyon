using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class ProductActivityLogger(
    IntegrationDbContext dbContext) : IProductActivityLogger
{
    public async Task LogAsync(Guid productId, ProductActivityType activityType, string message,
        ProductActivityStatus status = ProductActivityStatus.Info,
        string? detail = null, string? marketplaceName = null, string? referenceId = null)
    {
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
        var logs = await dbContext.ProductActivityLogs
            .AsNoTracking()
            .Where(l => l.ProductId == productId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return new SuccessDataResult<List<ProductActivityLog>>(logs);
    }
}
