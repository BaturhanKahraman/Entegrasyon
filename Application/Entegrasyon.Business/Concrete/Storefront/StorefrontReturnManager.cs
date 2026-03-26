using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontReturnManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontReturnManager
{
    public async Task<IResult> CreateReturnRequestAsync(
        int tenantId, Guid orderId, int customerId, string reason, string? description)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return new ErrorResult("Iade nedeni bos birakilamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);

        if (order is null)
            return new ErrorResult("Siparis bulunamadi.");

        if (order.StorefrontOrderStatus != OrderStatus.Delivered)
            return new ErrorResult("Sadece teslim edilmis siparisler icin iade talebi olusturulabilir.");

        var alreadyRequested = await dbContext.StorefrontReturnRequests
            .AnyAsync(r => r.TenantId == tenantId && r.OrderId == orderId && r.CustomerId == customerId);

        if (alreadyRequested)
            return new ErrorResult("Bu siparis icin zaten bir iade talebi bulunmaktadir.");

        var returnRequest = new StorefrontReturnRequest
        {
            TenantId = tenantId,
            OrderId = orderId,
            CustomerId = customerId,
            Status = ReturnStatus.Pending,
            Reason = reason,
            Description = description
        };

        dbContext.StorefrontReturnRequests.Add(returnRequest);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Iade talebiniz basariyla olusturuldu.");
    }

    public async Task<IDataResult<List<StorefrontReturnRequest>>> GetCustomerReturnsAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var returns = await dbContext.StorefrontReturnRequests
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontReturnRequest>>(returns);
    }

    public async Task<IDataResult<List<StorefrontReturnRequest>>> GetAllReturnsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var returns = await dbContext.StorefrontReturnRequests
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontReturnRequest>>(returns);
    }

    public async Task<IResult> UpdateReturnStatusAsync(
        int id, ReturnStatus status, string? reviewNote, decimal? refundAmount)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var returnRequest = await dbContext.StorefrontReturnRequests.FindAsync(id);
        if (returnRequest is null)
            return new ErrorResult("Iade talebi bulunamadi.");

        returnRequest.Status = status;
        returnRequest.ReviewNote = reviewNote;
        returnRequest.RefundAmount = refundAmount;
        returnRequest.ReviewedAt = DateTimeOffset.UtcNow;

        dbContext.StorefrontReturnRequests.Update(returnRequest);
        await dbContext.SaveChangesAsync();

        var statusText = status switch
        {
            ReturnStatus.Approved => "onaylandi",
            ReturnStatus.Rejected => "reddedildi",
            ReturnStatus.Completed => "tamamlandi",
            _ => "guncellendi"
        };

        return new SuccessResult($"Iade talebi {statusText}.");
    }
}
