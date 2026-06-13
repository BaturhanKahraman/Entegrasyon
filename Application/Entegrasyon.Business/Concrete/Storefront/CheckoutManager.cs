using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Storefront;

public class CheckoutManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IOfficeStockManager stockManager,
    IApplicationLogManager applicationLogManager,
    ILogger<CheckoutManager> logger) : ICheckoutManager
{
    public async Task<IDataResult<Order>> CreateOrderFromCartAsync(
        Guid cartId, int customerId, int tenantId,
        CheckoutRequestDto dto, decimal freeShippingThreshold, decimal flatShippingRate)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var cart = await dbContext.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p!.Brand)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart is null)
            return new ErrorDataResult<Order>(null!, "Sepet bulunamadı.");

        if (!cart.Items.Any())
            return new ErrorDataResult<Order>(null!, "Sepet bos. Sipariş olusturulamaz.");

        // TODO: configurable storefront warehouse / multi-warehouse allocation
        // Şimdilik: her kalem için yeterli stoğu olan ilk depoyu seç.
        // Çoklu depo dağıtımı (bir kalemi birden fazla depodan karşılamak) ilerideki faz.

        // Depo seçimi — pre-check TOPLAM değil, seçilen depoya bakmalı.
        // BUG FIX: Eski kod Sum(tümDepo) >= miktar ile geçiyor,
        // sonra hardcoded depo-1'den düşüyordu → depo-1=0 olunca affected==0 sessizce geçiyordu.
        var warehouseSelections = new Dictionary<Guid, int>(); // VariantId → BranchOfficeId

        foreach (var item in cart.Items)
        {
            // Yeterli stoğu olan ilk depoyu seç (CurrentStock >= istenen miktar)
            var selectedStock = item.ProductVariant.BranchOfficeStocks
                .Where(s => s.CurrentStock >= item.Quantity)
                .OrderBy(s => s.BranchOfficeId) // deterministik sıra; ileride öncelik config buraya
                .FirstOrDefault();

            if (selectedStock is null)
            {
                var productName = item.ProductVariant.Product?.Title
                    ?? item.ProductVariantId.ToString();
                var totalAvailable = item.ProductVariant.BranchOfficeStocks
                    .Sum(s => s.CurrentStock);

                logger.LogWarning(
                    "Checkout stok yetersiz: variant={VariantId} istek={Qty} toplam_mevcut={Total}",
                    item.ProductVariantId, item.Quantity, totalAvailable);

                return new ErrorDataResult<Order>(null!,
                    $"Yetersiz stok: {productName}. " +
                    $"Istenen: {item.Quantity}, Tek depoda mevcut: {totalAvailable}");
            }

            warehouseSelections[item.ProductVariantId] = selectedStock.BranchOfficeId;
        }

        // Calculate totals
        var subTotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
        var shippingCost = subTotal >= freeShippingThreshold ? 0m : flatShippingRate;
        var grandTotal = subTotal + shippingCost;

        // Parse name parts
        var nameParts = dto.ShippingFullName.Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        // Build shipping address
        var shippingAddress = new Address
        {
            City = dto.ShippingCity,
            County = dto.ShippingDistrict,
            FullAddress = dto.ShippingAddress,
            ZipCode = dto.ShippingPostalCode,
            Country = "Turkiye"
        };

        // BillingAddress: UseSameAddressForBilling olsa bile AYRI nesne oluştur.
        // EF owned-entity: aynı Address instance'ı hem ShippingAddress hem BillingAddress
        // olarak atanırsa "property belongs to ShippingAddress#Address but used with
        // BillingAddress#Address" InvalidOperationException fırlar.
        var billingAddress = dto.UseSameAddressForBilling
            ? new Address
            {
                City = shippingAddress.City,
                County = shippingAddress.County,
                FullAddress = shippingAddress.FullAddress,
                ZipCode = shippingAddress.ZipCode,
                Country = shippingAddress.Country
            }
            : new Address
            {
                City = dto.BillingCity,
                FullAddress = dto.BillingAddress,
                Country = "Turkiye"
            };

        // Check if iyzico payment config exists — if yes, order starts as Pending
        var hasPaymentConfig = await dbContext.Set<StorefrontPaymentConfig>()
            .AnyAsync(c => c.IsActive && c.PaymentProvider == "Iyzico");

        var paymentStatus = hasPaymentConfig ? PaymentStatus.Pending : PaymentStatus.Paid;

        // Create order entity (not yet saved to DB — stock must be atomically reserved first)
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"SF-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            OrderDate = DateTimeOffset.UtcNow,
            CustomerId = customerId,
            CustomerFirstName = firstName,
            CustomerLastName = lastName,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            SubTotal = subTotal,
            ShippingCost = shippingCost,
            GrossAmount = grandTotal,
            StorefrontPaymentStatus = paymentStatus,
            StorefrontOrderStatus = OrderStatus.Received,
            OrderNote = dto.OrderNote,
            MarketPlaceId = null
        };

        var orderItems = cart.Items.Select(item => new OrderItem
        {
            OrderId = order.Id,
            ProductId = item.ProductVariantId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Barcode = item.ProductVariant.Barcode,
            BrandName = item.ProductVariant.Product?.Brand?.Name
        }).ToList();

        order.OrderItems = orderItems;

        // Atomik stok düşme — sipariş DB'ye YAZILMADAN önce stok rezerve edilmeli.
        // Başarısız olursa sipariş hiç oluşturulmaz (oversell koruması).
        // Kısmen başarılı olursa (sonraki kalem başarısız), öncekiler geri alınır.
        await applicationLogManager.AddLog(
            $"Storefront sipariş stok rezervasyonu başlıyor. Sepet: {cartId}, Müşteri: {customerId}",
            LogType.Order, LogAction.Add);

        var decreasedItems = new List<(int BranchOfficeId, Guid VariantId, int Qty)>();

        foreach (var item in cart.Items)
        {
            var branchOfficeId = warehouseSelections[item.ProductVariantId];

            logger.LogInformation(
                "Stok düşülüyor: variant={VariantId} depo={BranchId} miktar={Qty}",
                item.ProductVariantId, branchOfficeId, item.Quantity);

            var stockResult = await stockManager.DecreaseStockAtomicAsync(
                branchOfficeId: branchOfficeId,
                productVariantId: item.ProductVariantId,
                quantity: item.Quantity,
                type: StockMovementType.Sale,
                referenceType: "StorefrontOrder",
                referenceId: order.Id.ToString());

            if (!stockResult.Success)
            {
                // Atomik düşme başarısız — başka kanal pre-check ile atomik düşme
                // arasındaki kısa pencerede stoğu bitirdi (race condition).
                // Şimdiye kadar başarıyla düşülen kalemleri geri ver.
                logger.LogWarning(
                    "Atomik stok düşme başarısız: variant={VariantId} depo={BranchId} " +
                    "hata={Msg}. Önceki {Count} kalem geri alınıyor.",
                    item.ProductVariantId, branchOfficeId,
                    stockResult.Message, decreasedItems.Count);

                foreach (var (rbBranch, rbVariant, rbQty) in decreasedItems)
                {
                    await stockManager.IncreaseStockAtomicAsync(
                        branchOfficeId: rbBranch,
                        productVariantId: rbVariant,
                        quantity: rbQty,
                        type: StockMovementType.Return,
                        referenceType: "StorefrontOrderRollback",
                        referenceId: order.Id.ToString());
                }

                await applicationLogManager.AddLog(
                    $"Storefront sipariş oluşturulamadı — stok yetersiz (atomik kontrol). " +
                    $"Sepet: {cartId}, Variant: {item.ProductVariantId}",
                    LogType.Order, LogAction.None);

                return new ErrorDataResult<Order>(null!,
                    $"Stok yetersiz: ürün başka bir kanaldan satılmış olabilir. " +
                    "Lütfen sepetinizi güncelleyip tekrar deneyin.");
            }

            decreasedItems.Add((branchOfficeId, item.ProductVariantId, item.Quantity));
        }

        // Tüm stoklar başarıyla rezerve edildi — şimdi sipariş + sepet temizliği kaydedilebilir
        dbContext.Orders.Add(order);
        dbContext.CartItems.RemoveRange(cart.Items);

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Storefront sipariş oluşturuldu: {order.OrderNumber}, Müşteri: {customerId}, " +
            $"Toplam: {grandTotal:C2}",
            LogType.Order, LogAction.Add);

        logger.LogInformation(
            "Storefront sipariş oluşturuldu: orderId={OrderId} orderNumber={Number} customerId={CustomerId}",
            order.Id, order.OrderNumber, customerId);

        return new SuccessDataResult<Order>(order, "Sipariş basariyla olusturuldu.");
    }

    public async Task<IResult> CompleteOrderPaymentAsync(Guid orderId, string transactionId, decimal paidAmount)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
            return new ErrorResult("Sipariş bulunamadı.");

        order.StorefrontPaymentStatus = PaymentStatus.Paid;
        order.PaymentTransactionId = transactionId;
        order.PaymentMethodType = "IyzicoCheckoutForm";

        dbContext.Orders.Update(order);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Storefront ödeme tamamlandı: sipariş={orderId}, işlem={transactionId}",
            LogType.Order, LogAction.Update);

        logger.LogInformation(
            "Storefront ödeme tamamlandı: orderId={OrderId} txn={Txn}",
            orderId, transactionId);

        return new SuccessResult("Odeme basariyla tamamlandi.");
    }

    public async Task<IResult> FailOrderPaymentAsync(Guid orderId, string? errorMessage)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorResult("Sipariş bulunamadı.");

        order.StorefrontPaymentStatus = PaymentStatus.Failed;
        order.OrderNote = string.IsNullOrEmpty(order.OrderNote)
            ? $"Odeme hatasi: {errorMessage}"
            : $"{order.OrderNote} | Odeme hatasi: {errorMessage}";

        dbContext.Orders.Update(order);

        // Stok iadesi — hangi depodan düşüldüğünü StockMovement.BranchOfficeId'den çözmek
        // daha doğru olur; şimdilik branchOfficeId=1 bırakıldı.
        // TODO: StorefrontOrder referenceId ile StockMovement kaydını bulup gerçek depoyu kullan
        foreach (var item in order.OrderItems)
        {
            await stockManager.IncreaseStockAtomicAsync(
                branchOfficeId: 1,
                productVariantId: item.ProductId!.Value,
                quantity: item.Quantity,
                type: StockMovementType.Return,
                referenceType: "StorefrontPaymentFailed",
                referenceId: order.Id.ToString());
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Storefront ödeme başarısız: sipariş={orderId}, hata={errorMessage}",
            LogType.Order, LogAction.None);

        logger.LogWarning(
            "Storefront ödeme başarısız: orderId={OrderId} error={Error}",
            orderId, errorMessage);

        return new SuccessResult("Sipariş odeme hatasi islendi.");
    }
}
