using System.Text.Json;
using Entegrasyon.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Handles offline sale operations against local SQLite.
/// Creates sale records + sync queue entries for later push.
/// </summary>
public class OfflineSaleService(
    OfflineDbContext db,
    ILogger<OfflineSaleService> logger)
{
    /// <summary>
    /// Search products by barcode in local SQLite.
    /// </summary>
    public async Task<OfflineProduct?> FindByBarcodeAsync(string barcode)
    {
        return await db.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    /// <summary>
    /// Search products by name (partial match) in local SQLite.
    /// </summary>
    public async Task<List<OfflineProduct>> SearchByNameAsync(string query, int maxResults = 20)
    {
        return await db.Products
            .Where(p => EF.Functions.Like(p.Title, $"%{query}%"))
            .Take(maxResults)
            .ToListAsync();
    }

    /// <summary>
    /// Complete an offline sale: save to SQLite, decrease local stock, add to sync queue.
    /// </summary>
    public async Task<OfflineSaleResult> CompleteSaleAsync(List<OfflineSaleItemDto> items, string paymentMethod, string? customerInfo)
    {
        if (items.Count == 0)
            return new OfflineSaleResult(false, "Sepet bos");

        var sale = new OfflineSale
        {
            Id = Guid.NewGuid(),
            SaleDate = DateTimeOffset.UtcNow,
            PaymentMethod = paymentMethod,
            CustomerInfo = customerInfo,
            IsSynced = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        decimal totalAmount = 0;
        decimal totalTax = 0;

        foreach (var item in items)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            if (product is null)
            {
                logger.LogWarning("Product not found in local DB: {ProductId}", item.ProductId);
                continue;
            }

            var lineNet = item.Quantity * item.UnitPrice * (1 - (decimal)item.DiscountPercent / 100);
            var lineTax = lineNet * (item.VatRate / 100);

            sale.Items.Add(new OfflineSaleItem
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                ProductId = item.ProductId,
                Barcode = product.Barcode,
                ProductName = product.Title,
                Size = product.Size,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                VatRate = item.VatRate
            });

            totalAmount += lineNet + lineTax;
            totalTax += lineTax;

            // Decrease local stock
            product.StockQuantity -= item.Quantity;
        }

        sale.TotalAmount = totalAmount;
        sale.TaxAmount = totalTax;

        db.Sales.Add(sale);

        // Add to sync queue (outbox pattern)
        var payload = JsonSerializer.Serialize(new
        {
            sale.Id,
            sale.SaleDate,
            sale.TotalAmount,
            sale.TaxAmount,
            sale.PaymentMethod,
            sale.CustomerInfo,
            Items = sale.Items.Select(i => new
            {
                i.ProductId,
                i.Barcode,
                i.Quantity,
                i.UnitPrice,
                i.DiscountPercent,
                i.VatRate
            })
        });

        db.SyncQueue.Add(new SyncQueue
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(OfflineSale),
            EntityId = sale.Id,
            Action = SyncAction.Create,
            Payload = payload,
            CreatedAt = DateTimeOffset.UtcNow,
            IsSynced = false
        });

        await db.SaveChangesAsync();

        logger.LogInformation("Offline sale completed: {SaleId}, Total: {Total}, Items: {Count}",
            sale.Id, sale.TotalAmount, sale.Items.Count);

        return new OfflineSaleResult(true, "Satis tamamlandi", sale.Id);
    }

    /// <summary>
    /// Get today's offline sales summary.
    /// </summary>
    public async Task<DailySalesSummary> GetTodaySummaryAsync()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var sales = await db.Sales
            .Where(s => s.SaleDate >= today)
            .ToListAsync();

        return new DailySalesSummary(
            Count: sales.Count,
            TotalRevenue: sales.Sum(s => s.TotalAmount),
            SyncedCount: sales.Count(s => s.IsSynced),
            PendingCount: sales.Count(s => !s.IsSynced));
    }

    /// <summary>
    /// Get total product count in local DB.
    /// </summary>
    public async Task<int> GetProductCountAsync()
    {
        return await db.Products.CountAsync();
    }
}

public record OfflineSaleItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    int DiscountPercent,
    decimal VatRate);

public record OfflineSaleResult(bool Success, string Message, Guid? SaleId = null);

public record DailySalesSummary(int Count, decimal TotalRevenue, int SyncedCount, int PendingCount);
