using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class CheckoutManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IOfficeStockManager stockManager) : ICheckoutManager
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
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart is null)
            return new ErrorDataResult<Order>(null!, "Sepet bulunamadı.");

        if (!cart.Items.Any())
            return new ErrorDataResult<Order>(null!, "Sepet bos. Sipariş olusturulamaz.");

        // Stock verification
        foreach (var item in cart.Items)
        {
            var availableStock = item.ProductVariant.BranchOfficeStocks.Sum(s => s.CurrentStock);
            if (item.Quantity > availableStock)
                return new ErrorDataResult<Order>(null!,
                    $"Yetersiz stok: {item.ProductVariant.Product?.Title ?? item.ProductVariantId.ToString()}. " +
                    $"Istenen: {item.Quantity}, Mevcut: {availableStock}");
        }

        // Calculate totals
        var subTotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
        var shippingCost = subTotal >= freeShippingThreshold ? 0m : flatShippingRate;
        var grandTotal = subTotal + shippingCost;

        // Parse name parts
        var nameParts = dto.ShippingFullName.Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        // Build addresses
        var shippingAddress = new Address
        {
            City = dto.ShippingCity,
            County = dto.ShippingDistrict,
            FullAddress = dto.ShippingAddress,
            ZipCode = dto.ShippingPostalCode,
            Country = "Turkiye"
        };

        var billingAddress = dto.UseSameAddressForBilling
            ? shippingAddress
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
        var orderStatus = hasPaymentConfig ? OrderStatus.Received : OrderStatus.Received;

        // Create order
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
            StorefrontOrderStatus = orderStatus,
            OrderNote = dto.OrderNote,
            MarketPlaceId = null
        };

        var orderItems = cart.Items.Select(item => new OrderItem
        {
            OrderId = order.Id,
            ProductId = item.ProductVariantId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Barcode = item.ProductVariant.Barcode
        }).ToList();

        order.OrderItems = orderItems;

        dbContext.Orders.Add(order);

        // Decrease stock for each item (reserve)
        foreach (var item in cart.Items)
        {
            await stockManager.DecreaseStockAtomicAsync(
                branchOfficeId: 1,
                productVariantId: item.ProductVariantId,
                quantity: item.Quantity,
                type: StockMovementType.Sale,
                referenceType: "StorefrontOrder",
                referenceId: order.Id.ToString());
        }

        // Clear cart
        dbContext.CartItems.RemoveRange(cart.Items);

        await dbContext.SaveChangesAsync();

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

        // Restore stock for each item
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

        return new SuccessResult("Sipariş odeme hatasi islendi.");
    }
}
