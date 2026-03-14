using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Blazor.Endpoints;

/// <summary>
/// Trendyol webhook receiver — Minimal API endpoint.
/// POST /api/trendyol/webhook
/// Sipariş durumu değişikliklerini alır ve local DB'yi günceller.
/// </summary>
public static class TrendyolWebhookEndpoint
{
    public static void MapTrendyolWebhooks(this WebApplication app)
    {
        app.MapPost("/api/trendyol/webhook", HandleWebhookAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleWebhookAsync(
        HttpContext httpContext,
        IOrderManager orderManager,
        IntegrationDbContext dbContext)
    {
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("TrendyolWebhook");

        try
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            logger.LogInformation("Trendyol webhook received: {Body}", body);

            var payload = JsonSerializer.Deserialize<TrendyolWebhookPayload>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload is null)
                return Results.BadRequest("Invalid payload");

            // Sipariş durumu güncelleme
            if (payload.ShipmentPackageId > 0)
            {
                var order = await dbContext.Orders
                    .FirstOrDefaultAsync(o => o.ShipmentPackageId == payload.ShipmentPackageId);

                if (order is not null)
                {
                    order.MarketplaceOrderStatus = payload.Status;
                    if (!string.IsNullOrEmpty(payload.TrackingNumber))
                        order.CargoTrackingNumber = payload.TrackingNumber;
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Order {OrderId} status updated via webhook to {Status}",
                        order.Id, payload.Status);
                }
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol webhook processing error");
            return Results.StatusCode(500);
        }
    }
}

public sealed record TrendyolWebhookPayload(
    long ShipmentPackageId,
    string? Status,
    string? TrackingNumber,
    string? OrderNumber);
