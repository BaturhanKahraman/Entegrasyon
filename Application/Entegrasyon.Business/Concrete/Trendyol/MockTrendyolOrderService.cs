using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Mock sipariş servisi — seeded fake siparişler döner.
/// </summary>
public sealed class MockTrendyolOrderService(
    ILogger<MockTrendyolOrderService> logger) : ITrendyolOrderService
{
    public Task<IDataResult<List<TrendyolShipmentPackage>>> FetchOrdersAsync(TrendyolOrderQueryParams query)
    {
        var mockOrders = new List<TrendyolShipmentPackage>
        {
            new(
                ShipmentPackageId: 100001,
                OrderNumber: "TY-MOCK-001",
                OrderDate: DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeMilliseconds().ToString(),
                Status: "Created",
                GrossAmount: 299.90m,
                TotalDiscount: 0,
                TotalPrice: 299.90m,
                Micro: false,
                FastDelivery: false,
                EstimatedDeliveryEndDate: DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeMilliseconds().ToString(),
                CargoProviderInfo: new TrendyolCargoInfo("Yurtiçi Kargo", null, null),
                CustomerInfo: new TrendyolCustomerInfo("Ali", "Yılmaz", "ali@test.com"),
                ShipmentAddress: new TrendyolAddressInfo("İstanbul", "Kadıköy", "Test Mah. Test Sk. No:1", "34000", "TR"),
                InvoiceAddress: new TrendyolAddressInfo("İstanbul", "Kadıköy", "Test Mah. Test Sk. No:1", "34000", "TR"),
                Lines: new List<TrendyolOrderLine>
                {
                    new(LineId: 1001, Quantity: 1, Price: 299.90m, Discount: 0,
                        Barcode: "MOCK-BARCODE-001", MerchantSku: "MOCK-SKU-001",
                        ProductName: "Mock Ürün 1", ProductColor: "Siyah", ProductSize: "M", MerchantId: 1)
                }),
            new(
                ShipmentPackageId: 100002,
                OrderNumber: "TY-MOCK-002",
                OrderDate: DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds().ToString(),
                Status: "Picking",
                GrossAmount: 549.00m,
                TotalDiscount: 50.00m,
                TotalPrice: 499.00m,
                Micro: true,
                FastDelivery: true,
                EstimatedDeliveryEndDate: DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeMilliseconds().ToString(),
                CargoProviderInfo: new TrendyolCargoInfo("Aras Kargo", "MOCK123456", "https://aras.com.tr/track/MOCK123456"),
                CustomerInfo: new TrendyolCustomerInfo("Ayşe", "Demir", "ayse@test.com"),
                ShipmentAddress: new TrendyolAddressInfo("Ankara", "Çankaya", "Kızılay Mah. Atatürk Blv. No:5", "06000", "TR"),
                InvoiceAddress: new TrendyolAddressInfo("Ankara", "Çankaya", "Kızılay Mah. Atatürk Blv. No:5", "06000", "TR"),
                Lines: new List<TrendyolOrderLine>
                {
                    new(LineId: 2001, Quantity: 2, Price: 199.50m, Discount: 25.00m,
                        Barcode: "MOCK-BARCODE-002", MerchantSku: "MOCK-SKU-002",
                        ProductName: "Mock Ürün 2", ProductColor: "Beyaz", ProductSize: "L", MerchantId: 1),
                    new(LineId: 2002, Quantity: 1, Price: 150.00m, Discount: 0,
                        Barcode: "MOCK-BARCODE-003", MerchantSku: "MOCK-SKU-003",
                        ProductName: "Mock Ürün 3", ProductColor: null, ProductSize: "XL", MerchantId: 1)
                })
        };

        logger.LogInformation("Mock: Returning {Count} mock orders", mockOrders.Count);

        return Task.FromResult<IDataResult<List<TrendyolShipmentPackage>>>(
            new SuccessDataResult<List<TrendyolShipmentPackage>>(mockOrders));
    }

    public Task<IResult> MarkUnsuppliedAsync(long shipmentPackageId, List<long> lineIds)
    {
        logger.LogInformation("Mock: Marked package {PackageId} lines [{Lines}] as unsupplied",
            shipmentPackageId, string.Join(", ", lineIds));
        return Task.FromResult<IResult>(new SuccessResult("Sipariş tedarik edilemez olarak işaretlendi (mock)."));
    }

    public Task<IResult> UpdateTrackingNumberAsync(long shipmentPackageId, string trackingNumber)
    {
        logger.LogInformation("Mock: Updated tracking number for package {PackageId}: {TrackingNumber}",
            shipmentPackageId, trackingNumber);
        return Task.FromResult<IResult>(new SuccessResult("Kargo takip numarası güncellendi (mock)."));
    }

    public Task<IDataResult<byte[]>> GetShippingLabelAsync(long shipmentPackageId)
    {
        logger.LogInformation("Mock: Shipping label requested for package {PackageId}", shipmentPackageId);
        // Boş PDF mock
        return Task.FromResult<IDataResult<byte[]>>(
            new SuccessDataResult<byte[]>(Array.Empty<byte>(), "Kargo etiketi hazır (mock)."));
    }
}
