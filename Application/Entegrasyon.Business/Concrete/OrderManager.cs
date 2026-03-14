using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Sipariş yönetim servisi — Trendyol DTO → local Order entity mapping, deduplication (ShipmentPackageId).
/// </summary>
public sealed class OrderManager(
    IntegrationDbContext dbContext,
    ILogger<OrderManager> logger) : IOrderManager
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<List<Order>>> GetOrdersAsync(int? marketPlaceId = null, int page = 0, int pageSize = 50)
    {
        var query = dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (marketPlaceId.HasValue)
            query = query.Where(o => o.MarketPlaceId == marketPlaceId);

        var orders = await query
            .OrderByDescending(o => o.OrderDate ?? o.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new SuccessDataResult<List<Order>>(orders);
    }

    public async Task<IDataResult<Order>> GetOrderByIdAsync(Guid orderId)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorDataResult<Order>(null!, "Sipariş bulunamadı.");

        return new SuccessDataResult<Order>(order);
    }

    public async Task<IResult> ImportTrendyolOrdersAsync(List<TrendyolShipmentPackage> packages)
    {
        if (packages.Count == 0)
            return new SuccessResult("İmport edilecek sipariş yok.");

        var importedCount = 0;

        foreach (var pkg in packages)
        {
            // Deduplication: ShipmentPackageId ile aynı sipariş tekrar import edilmez
            var exists = await dbContext.Orders
                .AnyAsync(o => o.ShipmentPackageId == pkg.ShipmentPackageId);

            if (exists)
            {
                // Durum güncelle
                var existingOrder = await dbContext.Orders
                    .FirstAsync(o => o.ShipmentPackageId == pkg.ShipmentPackageId);

                existingOrder.MarketplaceOrderStatus = pkg.Status;
                if (pkg.CargoProviderInfo is not null)
                {
                    existingOrder.CargoTrackingNumber = pkg.CargoProviderInfo.CargoTrackingNumber;
                    existingOrder.CargoTrackingLink = pkg.CargoProviderInfo.CargoTrackingLink;
                }
                continue;
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                MarketPlaceId = TrendyolMarketPlaceId,
                ShipmentPackageId = pkg.ShipmentPackageId,
                OrderNumber = pkg.OrderNumber,
                MarketplaceOrderStatus = pkg.Status,
                GrossAmount = pkg.GrossAmount,
                IsMicro = pkg.Micro,
                IsFastDelivery = pkg.FastDelivery,
                CargoProviderName = pkg.CargoProviderInfo?.CargoProviderName,
                CargoTrackingNumber = pkg.CargoProviderInfo?.CargoTrackingNumber,
                CargoTrackingLink = pkg.CargoProviderInfo?.CargoTrackingLink,
                CustomerFirstName = pkg.CustomerInfo?.FirstName,
                CustomerLastName = pkg.CustomerInfo?.LastName,
                CustomerEmail = pkg.CustomerInfo?.Email,
            };

            // OrderDate parse
            if (long.TryParse(pkg.OrderDate, out var orderDateMs))
                order.OrderDate = DateTimeOffset.FromUnixTimeMilliseconds(orderDateMs);

            if (long.TryParse(pkg.EstimatedDeliveryEndDate, out var deliveryMs))
                order.EstimatedDeliveryEndDate = DateTimeOffset.FromUnixTimeMilliseconds(deliveryMs);

            // Adresler
            if (pkg.ShipmentAddress is not null)
            {
                order.ShippingAddress = new Entity.Address
                {
                    City = pkg.ShipmentAddress.City ?? "",
                    County = pkg.ShipmentAddress.District ?? "",
                    Country = pkg.ShipmentAddress.CountryCode ?? "TR",
                    FullAddress = pkg.ShipmentAddress.FullAddress ?? "",
                    ZipCode = pkg.ShipmentAddress.PostalCode ?? "",
                    Street = ""
                };
            }

            if (pkg.InvoiceAddress is not null)
            {
                order.BillingAddress = new Entity.Address
                {
                    City = pkg.InvoiceAddress.City ?? "",
                    County = pkg.InvoiceAddress.District ?? "",
                    Country = pkg.InvoiceAddress.CountryCode ?? "TR",
                    FullAddress = pkg.InvoiceAddress.FullAddress ?? "",
                    ZipCode = pkg.InvoiceAddress.PostalCode ?? "",
                    Street = ""
                };
            }

            // Order items
            var orderItems = new List<OrderItem>();
            if (pkg.Lines is not null)
            {
                foreach (var line in pkg.Lines)
                {
                    // Barcode ile lokal ürün eşleştirme
                    Guid? productVariantId = null;
                    if (!string.IsNullOrEmpty(line.Barcode))
                    {
                        productVariantId = await dbContext.ProductVariants.AsNoTracking()
                            .Where(v => v.Barcode == line.Barcode)
                            .Select(v => (Guid?)v.Id)
                            .FirstOrDefaultAsync();
                    }

                    orderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = productVariantId,
                        Quantity = line.Quantity,
                        UnitPrice = line.Price,
                        LineId = line.LineId,
                        Barcode = line.Barcode,
                        MerchantSku = line.MerchantSku,
                        ProductColor = line.ProductColor,
                        ProductSize = line.ProductSize,
                        Discount = line.Discount
                    });
                }
            }

            order.OrderItems = orderItems;
            dbContext.Orders.Add(order);
            importedCount++;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} Trendyol orders", importedCount);
        return new SuccessResult($"{importedCount} sipariş import edildi.");
    }

    public async Task<IResult> UpdateOrderStatusAsync(Guid orderId, string newStatus)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
            return new ErrorResult("Sipariş bulunamadı.");

        order.MarketplaceOrderStatus = newStatus;
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Sipariş durumu güncellendi.");
    }
}
