using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Entegrasyon.UnitTest")]

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Sipariş yönetim servisi — Trendyol DTO → local Order entity mapping, deduplication (ShipmentPackageId).
/// Sipariş import edildiğinde stok atomik olarak düşülür.
/// Advisory lock ile eşzamanlı import korunur.
/// </summary>
public class OrderManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IOfficeStockManager officeStockManager,
    INotificationManager notificationManager,
    ILogger<OrderManager> logger) : IOrderManager
{
    private const long AdvisoryLockKeyTrendyolImport = 2001;
    private const long AdvisoryLockKeyN11Import = 2002;

    public async Task<IDataResult<List<Order>>> GetOrdersAsync(int? marketPlaceId = null, int page = 0, int pageSize = 50)
    {
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
        var order = await dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorDataResult<Order>(null!, "Sipariş bulunamadı.");

        return new SuccessDataResult<Order>(order);
    }

    public async Task<IResult> ImportTrendyolOrdersAsync(List<TrendyolShipmentPackage> packages)
    {
        using var dbContext = contextFactory.CreateDbContext();
        if (packages.Count == 0)
            return new SuccessResult("İmport edilecek sipariş yok.");

        // Advisory lock: Eşzamanlı import'u engelle
        var lockAcquired = await dbContext.Database
            .SqlQuery<bool>($"""SELECT pg_try_advisory_lock({AdvisoryLockKeyTrendyolImport}) AS "Value" """)
            .FirstAsync();

        if (!lockAcquired)
            return new ErrorResult("Sipariş import işlemi zaten devam ediyor.");

        try
        {
            return await ExecuteImportAsync(dbContext, packages);
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0})", AdvisoryLockKeyTrendyolImport);
        }
    }

    public async Task<IResult> ImportN11OrdersAsync(List<N11OrderDto> orders)
    {
        using var dbContext = contextFactory.CreateDbContext();
        if (orders.Count == 0)
            return new SuccessResult("İmport edilecek sipariş yok.");

        // Advisory lock: Eşzamanlı import'u engelle
        var lockAcquired = await dbContext.Database
            .SqlQuery<bool>($"""SELECT pg_try_advisory_lock({AdvisoryLockKeyN11Import}) AS "Value" """)
            .FirstAsync();

        if (!lockAcquired)
            return new ErrorResult("N11 sipariş import işlemi zaten devam ediyor.");

        try
        {
            return await ExecuteN11ImportAsync(dbContext, orders);
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0})", AdvisoryLockKeyN11Import);
        }
    }

    internal async Task<IResult> ExecuteN11ImportAsync(IntegrationDbContext dbContext, List<N11OrderDto> orders)
    {
        var importedCount = 0;

        // Marketplace'e bağlı depo ID'lerini önceden al
        var warehouseIds = await dbContext.MarketPlaceWarehouses
            .Where(w => w.MarketPlaceId == N11MarketPlaceId && !w.IsDeleted)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        // Batch deduplication: Tüm OrderNumber'ları tek sorguda kontrol et
        var allOrderNumbers = orders
            .Select(o => o.OrderNumber)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        var existingOrderNumbers = (await dbContext.Orders
            .Where(o => o.MarketPlaceId == N11MarketPlaceId && allOrderNumbers.Contains(o.OrderNumber))
            .Select(o => o.OrderNumber)
            .ToListAsync()).ToHashSet();

        // Batch barcode lookup: Tüm barkodları tek sorguda çöz
        var allBarcodes = orders
            .Where(o => o.OrderItems is not null)
            .SelectMany(o => o.OrderItems)
            .Select(i => i.ProductSellerCode)
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct()
            .ToArray();

        var barcodeMap = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => allBarcodes.Contains(v.Barcode))
            .ToDictionaryAsync(v => v.Barcode, v => v.Id);

        foreach (var dto in orders)
        {
            // Deduplication: aynı N11 siparişi tekrar gelmesin
            if (existingOrderNumbers.Contains(dto.OrderNumber))
                continue;

            var order = new Order
            {
                Id = Guid.NewGuid(),
                MarketPlaceId = N11MarketPlaceId,
                OrderNumber = dto.OrderNumber,
                MarketplaceOrderStatus = dto.Status,
                GrossAmount = dto.TotalAmount,
                OrderDate = dto.CreateDate,
                CustomerFirstName = dto.Buyer?.FirstName,
                CustomerLastName = dto.Buyer?.LastName,
                CustomerEmail = dto.Buyer?.Email,
            };

            // Adresler
            if (dto.BillingAddress is not null)
            {
                order.BillingAddress = new Entegrasyon.Entity.Address
                {
                    City = dto.BillingAddress.City ?? "",
                    County = dto.BillingAddress.District ?? "",
                    Country = "TR",
                    FullAddress = dto.BillingAddress.FullAddress ?? "",
                    ZipCode = dto.BillingAddress.PostalCode ?? "",
                    Street = ""
                };
            }

            if (dto.ShippingAddress is not null)
            {
                order.ShippingAddress = new Entegrasyon.Entity.Address
                {
                    City = dto.ShippingAddress.City ?? "",
                    County = dto.ShippingAddress.District ?? "",
                    Country = "TR",
                    FullAddress = dto.ShippingAddress.FullAddress ?? "",
                    ZipCode = dto.ShippingAddress.PostalCode ?? "",
                    Street = ""
                };
            }

            // Order items + stok düşme
            var orderItems = new List<OrderItem>();
            foreach (var item in dto.OrderItems)
            {
                // Barcode ile lokal ürün eşleştirme — dictionary lookup (O(1))
                Guid? productVariantId = null;
                if (!string.IsNullOrEmpty(item.ProductSellerCode) &&
                    barcodeMap.TryGetValue(item.ProductSellerCode, out var variantId))
                    productVariantId = variantId;

                orderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = productVariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    Barcode = item.ProductSellerCode,
                    MerchantSku = item.ProductSellerCode,
                });

                // Stok düşme: N11 marketplace satışı gerçekleşti
                if (productVariantId.HasValue && item.Quantity > 0 && warehouseIds.Count > 0)
                {
                    await DecreaseStockForMarketplaceOrder(
                        warehouseIds, productVariantId.Value, item.Quantity,
                        dto.OrderNumber);
                }
            }

            order.OrderItems = orderItems;
            dbContext.Orders.Add(order);
            importedCount++;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} N11 orders", importedCount);
        return new SuccessResult($"{importedCount} N11 siparişi import edildi.");
    }

    private async Task<IResult> ExecuteImportAsync(IntegrationDbContext dbContext, List<TrendyolShipmentPackage> packages)
    {
        var importedCount = 0;

        // Marketplace'e bağlı depo ID'lerini önceden al (her sipariş için tekrar sorgulamayalım)
        var warehouseIds = await dbContext.MarketPlaceWarehouses
            .Where(w => w.MarketPlaceId == TrendyolMarketPlaceId && !w.IsDeleted)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        // Batch deduplication: Tüm ShipmentPackageId'leri tek sorguda kontrol et
        var allPackageIds = packages
            .Select(p => (long?)p.ShipmentPackageId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var existingOrders = await dbContext.Orders
            .Where(o => allPackageIds.Contains(o.ShipmentPackageId))
            .ToDictionaryAsync(o => o.ShipmentPackageId!.Value);

        // Batch barcode lookup: Tüm barkodları tek sorguda çöz
        var allBarcodes = packages
            .Where(p => p.Lines is not null)
            .SelectMany(p => p.Lines)
            .Select(l => l.Barcode)
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct()
            .ToArray();

        var barcodeMap = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => allBarcodes.Contains(v.Barcode))
            .ToDictionaryAsync(v => v.Barcode, v => v.Id);

        foreach (var pkg in packages)
        {
            // Deduplication: Önceden çekilen dictionary'den kontrol et
            if (existingOrders.TryGetValue(pkg.ShipmentPackageId, out var existingOrder))
            {
                // Durum güncelle
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

            // Order items + stok düşme
            var orderItems = new List<OrderItem>();
            if (pkg.Lines is not null)
            {
                foreach (var line in pkg.Lines)
                {
                    // Barcode ile lokal ürün eşleştirme — dictionary lookup (O(1))
                    Guid? productVariantId = null;
                    if (!string.IsNullOrEmpty(line.Barcode) && barcodeMap.TryGetValue(line.Barcode, out var variantId))
                        productVariantId = variantId;

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
                        Discount = line.Discount,
                        VatRate = pkg.Micro ? 0 : line.VatRate
                    });

                    // Stok düşme: marketplace satışı gerçekleşti
                    if (productVariantId.HasValue && line.Quantity > 0 && warehouseIds.Count > 0)
                    {
                        await DecreaseStockForMarketplaceOrder(
                            warehouseIds, productVariantId.Value, line.Quantity,
                            pkg.ShipmentPackageId.ToString());
                    }
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
        using var dbContext = contextFactory.CreateDbContext();
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
            return new ErrorResult("Sipariş bulunamadı.");

        order.MarketplaceOrderStatus = newStatus;
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Sipariş durumu güncellendi.");
    }

    public async Task<IResult> UpdateOrderByShipmentPackageAsync(long shipmentPackageId, string? status, string? trackingNumber)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var order = await dbContext.Orders
            .FirstOrDefaultAsync(o => o.ShipmentPackageId == shipmentPackageId);

        if (order is null)
            return new ErrorResult("Sipariş bulunamadı.");

        if (!string.IsNullOrEmpty(status))
            order.MarketplaceOrderStatus = status;
        if (!string.IsNullOrEmpty(trackingNumber))
            order.CargoTrackingNumber = trackingNumber;

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Sipariş güncellendi.");
    }

    private async Task DecreaseStockForMarketplaceOrder(
        List<int> warehouseIds, Guid productVariantId, int quantity, string referenceId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        foreach (var warehouseId in warehouseIds)
        {
            var stockResult = await officeStockManager.DecreaseStockAtomicAsync(
                warehouseId, productVariantId, quantity,
                StockMovementType.MarketplaceSale,
                "TrendyolOrder", referenceId);

            if (stockResult.Success)
                return; // Başarılı — ilk uygun depodan düşüldü

            // Stok yetersiz ama marketplace zaten sattı — zorla düş
            logger.LogWarning(
                "Stok yetersiz, zorla düşülüyor. VariantId={VariantId}, Warehouse={WarehouseId}, Qty={Qty}",
                productVariantId, warehouseId, quantity);

            await officeStockManager.ForceDecreaseStockAsync(
                warehouseId, productVariantId, quantity,
                StockMovementType.MarketplaceSale,
                "TrendyolOrder", referenceId);

            // Acil bildirim
            var adminUserIds = await dbContext.Users.AsNoTracking()
                .Select(u => u.Id)
                .ToListAsync();

            await notificationManager.SendNotification(
                "Marketplace Stok Uyarısı",
                $"Trendyol siparişi için stok yetersizdi ama satış zaten gerçekleşti. Sipariş: {referenceId}",
                NotificationSeverity.Error,
                NotificationCategory.Stok,
                adminUserIds);
            return;
        }

        logger.LogWarning(
            "Marketplace siparişi için depo bulunamadı. VariantId={VariantId}, ShipmentPkg={PkgId}",
            productVariantId, referenceId);
    }
}
