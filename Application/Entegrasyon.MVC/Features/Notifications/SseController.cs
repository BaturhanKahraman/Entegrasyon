using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Channels;
using Entegrasyon.Business.Notifications.Sse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Notifications;

[Authorize]
public sealed class SseController(ISseConnectionRegistry registry, ILogger<SseController> logger) : Controller
{
    [HttpGet("/events/notifications")]
    public ServerSentEventsResult<SseNotificationPayload> StreamNotifications()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            // Return an empty stream — authentication failure will be caught by [Authorize]
            // but as a safety net return an empty sequence
            return TypedResults.ServerSentEvents(EmptyStream());
        }

        // NOTE: Use the controller's built-in HttpContext property — DO NOT add an
        // `HttpContext httpContext` parameter; MVC tries to model-bind that and crashes.
        return TypedResults.ServerSentEvents(Stream(userId, HttpContext.RequestAborted));
    }

    private static async IAsyncEnumerable<SseItem<SseNotificationPayload>> EmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private async IAsyncEnumerable<SseItem<SseNotificationPayload>> Stream(
        Guid userId, [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateBounded<SseNotificationPayload>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        var connId = registry.Register(userId, channel);
        logger.LogInformation("SSE connection opened for user {UserId} (connId {ConnId})", userId, connId);

        using var heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(25));
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = RunHeartbeatAsync(heartbeatTimer, channel, heartbeatCts.Token);

        try
        {
            // Client disconnect cancels `ct`; reading must end gracefully (break) instead of
            // letting OperationCanceledException propagate out of the iterator — that surfaces
            // as a logged HTTP 500 in SseFormatter on every normal disconnect (log pollution).
            while (true)
            {
                SseNotificationPayload msg;
                try
                {
                    if (!await channel.Reader.WaitToReadAsync(ct))
                        break;
                    if (!channel.Reader.TryRead(out msg!))
                        continue;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                yield return new SseItem<SseNotificationPayload>(msg, msg.EventType)
                {
                    ReconnectionInterval = TimeSpan.FromSeconds(5)
                };
            }
        }
        finally
        {
            await heartbeatCts.CancelAsync();
            try { await heartbeatTask; } catch { /* swallow on shutdown */ }
            registry.Unregister(userId, connId);
            logger.LogInformation("SSE connection closed for user {UserId} (connId {ConnId})", userId, connId);
        }
    }

    private static async Task RunHeartbeatAsync(
        PeriodicTimer timer,
        Channel<SseNotificationPayload> channel,
        CancellationToken ct)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                channel.Writer.TryWrite(new SseNotificationPayload { EventType = "heartbeat" });
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
    }
}
