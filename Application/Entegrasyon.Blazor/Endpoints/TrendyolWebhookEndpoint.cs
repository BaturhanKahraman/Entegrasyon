using System.Text.Json;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Blazor.Endpoints;

/// <summary>
/// Trendyol webhook receiver -- Minimal API endpoint.
/// POST /api/trendyol/webhook
/// Siparis durumu degisikliklerini alir ve local DB'yi gunceller.
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
        IOrderManager orderManager)
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

            // Siparis durumu guncelleme
            if (payload.ShipmentPackageId > 0)
            {
                var result = await orderManager.UpdateOrderByShipmentPackageAsync(
                    payload.ShipmentPackageId, payload.Status, payload.TrackingNumber);

                if (result.Success)
                    logger.LogInformation("Order with ShipmentPackageId {PackageId} updated via webhook to {Status}",
                        payload.ShipmentPackageId, payload.Status);
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
