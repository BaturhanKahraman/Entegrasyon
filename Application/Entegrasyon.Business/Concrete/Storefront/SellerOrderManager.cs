using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class SellerOrderManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : ISellerOrderManager
{
    public async Task<IDataResult<List<Order>>> GetSellerOrdersAsync(int sellerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var orders = await dbContext.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderItems.Any(oi => oi.SellerId == sellerId))
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return new SuccessDataResult<List<Order>>(orders);
    }

    public async Task<IDataResult<Order>> GetSellerOrderDetailAsync(int sellerId, Guid orderId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorDataResult<Order>(null!, "Siparis bulunamadi.");

        var hasSellerItems = order.OrderItems.Any(oi => oi.SellerId == sellerId);
        if (!hasSellerItems)
            return new ErrorDataResult<Order>(null!, "Bu siparis size ait degil.");

        return new SuccessDataResult<Order>(order);
    }

    public async Task<IResult> UpdateSellerOrderStatusAsync(int sellerId, Guid orderId, string newStatus)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorResult("Siparis bulunamadi.");

        var sellerItems = order.OrderItems.Where(oi => oi.SellerId == sellerId).ToList();
        if (!sellerItems.Any())
            return new ErrorResult("Bu siparis size ait degil.");

        if (!Enum.TryParse<OrderStatus>(newStatus, true, out var status))
            return new ErrorResult("Gecersiz siparis durumu.");

        order.StorefrontOrderStatus = status;
        dbContext.Orders.Update(order);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Siparis durumu guncellendi.");
    }
}
