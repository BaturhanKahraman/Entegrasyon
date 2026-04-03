using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Business.Extensions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Requests;
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
    // TODO Multi-tenant: Lock key'lere tenantId dahil et: (tenantId * 10000) + marketplaceImportId
    // Şu an single-tenant olduğu için sabit key'ler yeterli.
    private const long AdvisoryLockKeyTrendyolImport = 2001;
    private const long AdvisoryLockKeyN11Import = 2002;
    private const long AdvisoryLockKeyPazaramaImport = 2005;

    public async Task<IDataResult<Pageable<Order>>> GetOrdersAsync(OrderPaginatedRequest request)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (request.MarketPlaceId.HasValue)
            query = query.Where(o => o.MarketPlaceId == request.MarketPlaceId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(o => o.MarketplaceOrderStatus == request.Status);

        query = query
            .ApplyGlobalSearch(request.SearchTerm,
                nameof(Order.OrderNumber),
                nameof(Order.CustomerFirstName),
                nameof(Order.CustomerLastName))
            .OrderByDescending(o => o.OrderDate ?? o.CreatedAt);

        var result = await query.ToPageableAsync(request);

        return new SuccessDataResult<Pageable<Order>>(result);
    }

    public async Task<IDataResult<Order>> GetOrderByIdAsync(Guid orderId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var order = await dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorDataResult<Order>(null!, "Sipariş bulunamadı.");

        return new SuccessDataResult<Order>(order);
    }

    public async Task<IResult> ImportTrendyolOrdersAsync(List<TrendyolShipmentPackage> packages)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
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
        await using var dbContext = await contextFactory.CreateDbContextAsync();
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

    public async Task<IResult> ImportPazaramaOrdersAsync(List<PazaramaOrderDto> orders)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        if (orders.Count == 0)
            return new SuccessResult("İmport edilecek sipariş yok.");

        // Advisory lock: Eşzamanlı import'u engelle
        var lockAcquired = await dbContext.Database
            .SqlQuery<bool>($"""SELECT pg_try_advisory_lock({AdvisoryLockKeyPazaramaImport}) AS "Value" """)
            .FirstAsync();

        if (!lockAcquired)
            return new ErrorResult("Pazarama sipariş import işlemi zaten devam ediyor.");

        try
        {
            return await ExecutePazaramaImportAsync(dbContext, orders);
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0})", AdvisoryLockKeyPazaramaImport);
        }
    }

    internal async Task<IResult> ExecutePazaramaImportAsync(IntegrationDbContext dbContext, List<PazaramaOrderDto> orders)
    {
        var importedCount = 0;

        // Marketplace'e bağlı depo ID'lerini önceden al
        var warehouseIds = await dbContext.MarketPlaceWarehouses
            .Where(w => w.MarketPlaceId == PazaramaMarketPlaceId && !w.IsDeleted)
            .Select(w => w.BranchOfficeId)
            .ToListAsync();

        // Batch deduplication: Tüm OrderNumber'ları tek sorguda kontrol et
        var allOrderNumbers = orders
            .Select(o => o.OrderNumber.ToString())
            .Distinct()
            .ToList();

        var existingOrderNumbers = (await dbContext.Orders
            .Where(o => o.MarketPlaceId == PazaramaMarketPlaceId && allOrderNumbers.Contains(o.OrderNumber!))
            .Select(o => o.OrderNumber)
            .ToListAsync()).ToHashSet();

        // Batch barcode lookup: Tüm barkodları tek sorguda çöz
        var allBarcodes = orders
            .Where(o => o.Items is not null)
            .SelectMany(o => o.Items!)
            .Select(i => i.Product?.Code)
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct()
            .ToArray();

        var barcodeMap = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => allBarcodes.Contains(v.Barcode))
            .ToDictionaryAsync(v => v.Barcode!, v => v.Id);

        foreach (var dto in orders)
        {
            var orderNumber = dto.OrderNumber.ToString();

            // Deduplication: aynı Pazarama siparişi tekrar gelmesin
            if (existingOrderNumbers.Contains(orderNumber))
                continue;

            // Müşteri adı ayrıştırma
            var nameParts = dto.CustomerName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) ?? [];
            var firstName = nameParts.Length > 0 ? nameParts[0] : null;
            var lastName = nameParts.Length > 1 ? nameParts[1] : null;

            var order = new Order
            {
                Id = Guid.NewGuid(),
                MarketPlaceId = PazaramaMarketPlaceId,
                OrderNumber = orderNumber,
                MarketplaceOrderStatus = dto.OrderStatus.ToString(),
                CustomerFirstName = firstName,
                CustomerLastName = lastName,
                CustomerEmail = dto.CustomerEmail,
                GrossAmount = dto.OrderAmount,
            };

            // Kargo adresi
            if (dto.ShipmentAddress is not null)
            {
                order.ShippingAddress = new Entegrasyon.Entity.Address
                {
                    City = dto.ShipmentAddress.CityName ?? "",
                    County = dto.ShipmentAddress.DistrictName ?? "",
                    Country = "TR",
                    FullAddress = dto.ShipmentAddress.DisplayAddressText ?? dto.ShipmentAddress.AddressDetail ?? "",
                    ZipCode = "",
                    Street = ""
                };
            }

            // Fatura adresi
            if (dto.BillingAddress is not null)
            {
                order.BillingAddress = new Entegrasyon.Entity.Address
                {
                    City = dto.BillingAddress.CityName ?? "",
                    County = dto.BillingAddress.DistrictName ?? "",
                    Country = "TR",
                    FullAddress = dto.BillingAddress.DisplayAddressText ?? dto.BillingAddress.AddressDetail ?? "",
                    ZipCode = "",
                    Street = ""
                };
            }

            // Order items + stok düşme
            var orderItems = new List<OrderItem>();
            if (dto.Items is not null)
            {
                foreach (var item in dto.Items)
                {
                    var barcode = item.Product?.Code;

                    // Barcode ile lokal ürün eşleştirme — dictionary lookup (O(1))
                    Guid? productVariantId = null;
                    if (!string.IsNullOrEmpty(barcode) && barcodeMap.TryGetValue(barcode, out var variantId))
                        productVariantId = variantId;

                    orderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = productVariantId,
                        Quantity = item.Quantity,
                        UnitPrice = item.SalePrice?.Value ?? 0,
                        Barcode = barcode,
                        VatRate = item.Product is not null ? (decimal?)item.Product.VatRate : null,
                    });

                    // Stok düşme: Pazarama marketplace satışı gerçekleşti
                    if (productVariantId.HasValue && item.Quantity > 0 && warehouseIds.Count > 0)
                    {
                        await DecreaseStockForMarketplaceOrder(
                            warehouseIds, productVariantId.Value, item.Quantity,
                            orderNumber);
                    }
                }
            }

            order.OrderItems = orderItems;
            dbContext.Orders.Add(order);
            importedCount++;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Imported {Count} Pazarama orders", importedCount);
        return new SuccessResult($"{importedCount} Pazarama siparişi import edildi.");
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
            .Where(o => o.MarketPlaceId == N11MarketPlaceId && allOrderNumbers.Contains(o.OrderNumber!))
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
            .ToDictionaryAsync(v => v.Barcode!, v => v.Id);

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
            .SelectMany(p => p.Lines!)
            .Select(l => l.Barcode)
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct()
            .ToArray();

        var barcodeMap = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => allBarcodes.Contains(v.Barcode))
            .ToDictionaryAsync(v => v.Barcode!, v => v.Id);

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
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
            return new ErrorResult("Sipariş bulunamadı.");

        order.MarketplaceOrderStatus = newStatus;
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Sipariş durumu güncellendi.");
    }

    public async Task<IResult> UpdateOrderByShipmentPackageAsync(long shipmentPackageId, string? status, string? trackingNumber)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
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

    // ── Storefront methods ──

    public async Task<IDataResult<List<Order>>> GetCustomerOrdersAsync(int customerId, int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var orders = await dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate ?? o.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<Order>>(orders);
    }

    public async Task<IDataResult<Order>> GetOrderDetailAsync(Guid orderId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var order = await dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorDataResult<Order>(null!, "Siparis bulunamadi.");

        if (order.CustomerId != customerId)
            return new ErrorDataResult<Order>(null!, "Bu siparise erisim yetkiniz yok.");

        return new SuccessDataResult<Order>(order);
    }

    public async Task<IResult> CancelOrderAsync(Guid orderId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorResult("Siparis bulunamadi.");

        if (order.CustomerId != customerId)
            return new ErrorResult("Bu siparise erisim yetkiniz yok.");

        if (order.StorefrontOrderStatus != Entity.Storefront.OrderStatus.Received)
            return new ErrorResult("Sadece 'Alindi' durumundaki siparisler iptal edilebilir.");

        order.StorefrontOrderStatus = Entity.Storefront.OrderStatus.Cancelled;
        order.StorefrontPaymentStatus = Entity.Storefront.PaymentStatus.Refunded;

        // Restore stock for each order item (decrease SoldQuantity to increase CurrentStock)
        foreach (var item in order.OrderItems)
        {
            if (item.ProductId.HasValue && item.Quantity > 0)
            {
                var stock = await dbContext.BranchOfficeStocks.AsTracking()
                    .FirstOrDefaultAsync(s => s.BranchOfficeId == 1 && s.ProductVariantId == item.ProductId);
                if (stock is not null)
                    stock.SoldQuantity -= item.Quantity;
            }
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Storefront order cancelled: {OrderId} by customer {CustomerId}", orderId, customerId);
        return new SuccessResult("Siparis basariyla iptal edildi.");
    }

    public async Task<IDataResult<Order>> GetOrderByNumberAsync(string orderNumber)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var order = await dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (order is null)
            return new ErrorDataResult<Order>(null!, "Siparis bulunamadi.");

        return new SuccessDataResult<Order>(order);
    }

    public async Task<IDataResult<List<Entity.Dtos.Storefront.StorefrontProductCardDto>>> GetPreviouslyPurchasedProductsAsync(int customerId, int count = 24)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Get distinct product variant IDs from this customer's orders, most recent first
        var variantIds = await dbContext.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.CustomerId == customerId && oi.ProductId.HasValue)
            .OrderByDescending(oi => oi.Order.OrderDate ?? oi.Order.CreatedAt)
            .Select(oi => oi.ProductId!.Value)
            .Distinct()
            .Take(count)
            .ToListAsync();

        if (variantIds.Count == 0)
            return new SuccessDataResult<List<Entity.Dtos.Storefront.StorefrontProductCardDto>>([]);

        // Load product variants with their products
        var variants = await dbContext.ProductVariants.AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.Images)
            .Include(v => v.BranchOfficeStocks)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        // Group by Product to get distinct products
        var cards = variants
            .Where(v => v.Product is not null)
            .GroupBy(v => v.Product!.Id)
            .Select(g =>
            {
                var product = g.First().Product!;
                var allVariants = g.ToList();
                var minPrice = allVariants.Min(v => v.SalePrice);
                var maxPrice = allVariants.Max(v => v.SalePrice);
                var totalStock = allVariants.Sum(v => v.BranchOfficeStocks.Sum(s => s.CurrentStock));
                var mainImage = allVariants
                    .SelectMany(v => v.Images)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefault();

                var maxListPrice = allVariants.Max(v => v.ListPrice);
                return new Entity.Dtos.Storefront.StorefrontProductCardDto(
                    product.Id, product.Title ?? "", product.SeoSlug,
                    mainImage?.StorageKey, minPrice, maxPrice,
                    maxListPrice > minPrice ? maxListPrice : null,
                    totalStock, product.Brand?.Name, product.Category?.Name ?? "",
                    product.CreatedAt > DateTimeOffset.UtcNow.AddDays(-30));
            })
            .Take(count)
            .ToList();

        return new SuccessDataResult<List<Entity.Dtos.Storefront.StorefrontProductCardDto>>(cards);
    }

    private async Task DecreaseStockForMarketplaceOrder(
        List<int> warehouseIds, Guid productVariantId, int quantity, string referenceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
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
