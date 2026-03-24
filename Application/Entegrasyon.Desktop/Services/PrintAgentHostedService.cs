using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Runs the PrintAgent web API as an embedded background service within the desktop app.
/// Listens on https://localhost:{port} (default 19100).
/// This eliminates the need for a separate PrintAgent installation.
/// In Photino there is no IHostedService support, so this is started manually via Task.Run.
/// </summary>
public class PrintAgentHostedService
{
    private readonly SettingsService _settingsService;
    private readonly ILogger<PrintAgentHostedService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public bool IsRunning { get; private set; }
    public int Port => _settingsService.Settings.PrintAgentPort;

    public PrintAgentHostedService(
        SettingsService settingsService,
        ILogger<PrintAgentHostedService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Start the embedded PrintAgent web API.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning) return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var port = _settingsService.Settings.PrintAgentPort;

        _runningTask = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("PrintAgent starting on port {Port}", port);

                // Note: Full PrintAgent integration requires the Entegrasyon.PrintAgent project reference.
                // For now, we set up a minimal health endpoint.
                // When PrintAgent is fully integrated, its Program.cs logic will be embedded here.

                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls($"https://localhost:{port}");
                builder.Logging.ClearProviders();

                var app = builder.Build();

                // Health check endpoint
                app.MapGet("/health", () => Results.Ok(new { status = "ok", version = "1.0.0-embedded" }));

                // Print endpoint placeholder — wire up actual PrintService when PrintAgent is referenced
                app.MapPost("/print", () => Results.Ok(new { jobId = Guid.NewGuid().ToString("N")[..8], status = "not-configured" }));

                // Printer discovery placeholder
                app.MapGet("/printers", () => Results.Ok(Array.Empty<object>()));

                IsRunning = true;
                _logger.LogInformation("PrintAgent listening on https://localhost:{Port}", port);

                await app.RunAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PrintAgent stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PrintAgent failed to start");
            }
            finally
            {
                IsRunning = false;
            }
        }, _cts.Token);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the embedded PrintAgent web API.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning || _cts is null) return;

        _cts.Cancel();

        if (_runningTask is not null)
        {
            try
            {
                await _runningTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _cts.Dispose();
        _cts = null;
        IsRunning = false;
        _logger.LogInformation("PrintAgent stopped");
    }
}
