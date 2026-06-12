using System.Text.Json;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Notifications.Handlers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Entegrasyon.Business.Channels;

/// <summary>
/// Task 0.12: Outbox dispatcher — PostgreSQL LISTEN/NOTIFY ile event-driven,
/// PeriodicTimer ile safety-polling, FOR UPDATE SKIP LOCKED ile concurrent-safe
/// batch çekme, exponential-backoff retry ve dead-letter desteği.
/// </summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxDispatchOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    // Singleton DbContext factory inject etmek scope-validation hatasına yol açıyordu;
    // ihtiyaç olduğunda scope açıp oradan resolve ediyoruz (notification-aware,
    // dev/prod ayrım gözetmeksizin çalışır).
    private readonly OutboxDispatchOptions _opts = options.Value;

    // BaseEvent assembly'sindeki tüm concrete event tiplerini name→Type cache'le.
    private static readonly Dictionary<string, Type> _eventTypeCache = BuildEventTypeCache();

    // ─── BackgroundService entry point ───────────────────────────────────────

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ResetStuckProcessingRowsAsync(stoppingToken);

        var listen = ListenLoopAsync(stoppingToken);
        var poll = PollLoopAsync(stoppingToken);
        // İkisi birlikte çalışır; birisi iptal/hata ile dursa diğeri de durur.
        await Task.WhenAny(listen, poll);
    }

    // Önceki process crash'inde Processing olarak kalan satırları Pending'e çeker.
    private async Task ResetStuckProcessingRowsAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var db = await factory.CreateDbContextAsync(ct);

            var stuckCutoff = DateTimeOffset.UtcNow.AddMinutes(-10);
            var stuckRows = await db.NotificationOutbox
                .AsTracking()
                .Where(x => x.Status == OutboxStatus.Processing && x.UpdatedAt < stuckCutoff)
                .ToListAsync(ct);

            if (stuckRows.Count == 0) return;

            foreach (var row in stuckRows)
                row.Status = OutboxStatus.Pending;

            await db.SaveChangesAsync(ct);
            logger.LogWarning("OutboxDispatcher startup: {Count} stuck Processing rows reset to Pending", stuckRows.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OutboxDispatcher startup recovery failed");
        }
    }

    // ─── LISTEN loop ─────────────────────────────────────────────────────────

    private async Task ListenLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var listenScope = scopeFactory.CreateAsyncScope();
                var listenFactory = listenScope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
                await using var db = await listenFactory.CreateDbContextAsync(stoppingToken);
                var conn = (NpgsqlConnection)db.Database.GetDbConnection();
                await conn.OpenAsync(stoppingToken);

                conn.Notification += async (_, _) =>
                {
                    try { await DispatchPendingAsync(stoppingToken); }
                    catch (Exception ex) { logger.LogError(ex, "Outbox dispatch (on NOTIFY) failed"); }
                };

                await using (var cmd = new NpgsqlCommand("LISTEN notification_outbox_new;", conn))
                    await cmd.ExecuteNonQueryAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                    await conn.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxDispatcher LISTEN loop failed; restarting in 2s");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    // ─── Polling loop (safety net) ────────────────────────────────────────────

    private async Task PollLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_opts.SafetyPollingInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await DispatchPendingAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Outbox dispatch (polling) failed"); }
        }
    }

    // ─── Core dispatch ────────────────────────────────────────────────────────

    /// <summary>
    /// Pending satırları FOR UPDATE SKIP LOCKED ile çek, Processing olarak işaretle,
    /// transaction commit et; ardından paralel (MaxConcurrency ile sınırlı) dispatch et.
    /// Public — integration testlerinden doğrudan çağrılabilir.
    /// </summary>
    public async Task DispatchPendingAsync(CancellationToken ct)
    {
        await using var pendingScope = scopeFactory.CreateAsyncScope();
        var pendingFactory = pendingScope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await pendingFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // FromSql ile FormattableString kullan — EF Core SQL injection koruması.
        // Enum int cast ve BatchSize sabit değerler olduğundan parameterize edilebilir.
        int pendingStatus = (int)OutboxStatus.Pending;
        int batchSize = _opts.BatchSize;
        // Tablo adı snake_case (notification_outbox), ama column'lar EF default'u olarak quoted-PascalCase.
        var pending = await db.NotificationOutbox
            .FromSql($"""
                SELECT * FROM notification_outbox
                WHERE "Status" = {pendingStatus}
                  AND ("NextRetryAt" IS NULL OR "NextRetryAt" <= NOW())
                ORDER BY "CreatedAt"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .AsTracking()
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            await tx.CommitAsync(ct);
            return;
        }

        logger.LogDebug("OutboxDispatcher: {Count} pending rows found", pending.Count);

        foreach (var row in pending)
            row.Status = OutboxStatus.Processing;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        using var sem = new SemaphoreSlim(_opts.MaxConcurrency);
        var tasks = pending.Select(async row =>
        {
            await sem.WaitAsync(ct);
            try { await DispatchSingleAsync(row, ct); }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
    }

    // ─── Single row dispatch ──────────────────────────────────────────────────

    private async Task DispatchSingleAsync(NotificationOutbox row, CancellationToken ct)
    {
        // Her satır için izole scope + izole DbContext (multi-tenant safe)
        await using var scope = scopeFactory.CreateAsyncScope();
        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<IntegrationDbContext>>()
            .CreateDbContextAsync(ct);

        try
        {
            if (!_eventTypeCache.TryGetValue(row.EventType, out var evtType))
                throw new InvalidOperationException($"Unknown event type: {row.EventType}");

            var deserializeOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var evt = JsonSerializer.Deserialize(row.PayloadJson, evtType, deserializeOptions)
                      ?? throw new InvalidOperationException($"Payload deserialize returned null for {row.EventType}");

            var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(evtType);
            var handlers = scope.ServiceProvider.GetServices(handlerInterface).ToList();

            logger.LogDebug("OutboxDispatcher: dispatching {EventType} to {HandlerCount} handler(s)", row.EventType, handlers.Count);

            var handleMethod = handlerInterface.GetMethod(nameof(IDomainEventHandler<BaseEvent>.HandleAsync))!;
            foreach (var handler in handlers)
                await (Task)handleMethod.Invoke(handler, [evt, ct])!;

            // Başarılı — Completed olarak işaretle
            var fresh = await db.NotificationOutbox.AsTracking().FirstOrDefaultAsync(x => x.Id == row.Id, ct)
                        ?? throw new InvalidOperationException($"Outbox row {row.Id} disappeared");
            fresh.Status = OutboxStatus.Completed;
            fresh.ProcessedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("OutboxDispatcher: {EventType} row {RowId} dispatched OK", row.EventType, row.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OutboxDispatcher: dispatch failed for {EventType} row {RowId}", row.EventType, row.Id);

            try
            {
                var fresh = await db.NotificationOutbox.AsTracking().FirstOrDefaultAsync(x => x.Id == row.Id, ct);
                if (fresh is null) return;

                fresh.RetryCount++;
                fresh.LastError = ex.Message.Length > 4000 ? ex.Message[..4000] : ex.Message;

                if (fresh.RetryCount >= _opts.MaxRetryCount)
                {
                    // Dead letter — başarısızlık limit aşıldı
                    db.DeadLetterOutbox.Add(new DeadLetterOutbox
                    {
                        OriginalOutboxId = fresh.Id,
                        TenantId = fresh.TenantId,
                        EventType = fresh.EventType,
                        PayloadJson = fresh.PayloadJson,
                        FinalError = ex.ToString(),
                        TotalRetryCount = fresh.RetryCount,
                        MovedAt = DateTimeOffset.UtcNow
                    });
                    fresh.Status = OutboxStatus.Failed;
                    logger.LogWarning("OutboxDispatcher: row {RowId} dead-lettered after {RetryCount} retries", row.Id, fresh.RetryCount);
                }
                else
                {
                    // Exponential backoff: 2^retryCount saniye
                    fresh.Status = OutboxStatus.Pending;
                    fresh.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2, fresh.RetryCount));
                    logger.LogWarning("OutboxDispatcher: row {RowId} retry {RetryCount} scheduled at {NextRetry}",
                        row.Id, fresh.RetryCount, fresh.NextRetryAt);
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "OutboxDispatcher: failed to update error state for row {RowId}", row.Id);
            }
        }
    }

    // ─── Type cache builder ───────────────────────────────────────────────────

    /// <summary>
    /// BaseEvent assembly'sindeki tüm concrete subclass'ları Name→Type olarak önbelleğe alır.
    /// Compile-time assembly scan — yeni event tipler eklendikçe otomatik yakalanır.
    /// </summary>
    private static Dictionary<string, Type> BuildEventTypeCache()
    {
        var baseType = typeof(BaseEvent);
        return baseType.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && baseType.IsAssignableFrom(t))
            .ToDictionary(t => t.Name, t => t);
    }
}
