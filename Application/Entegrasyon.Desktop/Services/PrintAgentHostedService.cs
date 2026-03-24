using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Entegrasyon.PrintAgent;
using Entegrasyon.PrintAgent.Configuration;
using Entegrasyon.PrintAgent.Contracts;
using Entegrasyon.PrintAgent.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Runs the full PrintAgent web API as an embedded background service within the desktop app.
/// Listens on https://localhost:{port} (default 19100).
/// Registers real PrintService, transports, and API endpoints — no separate PrintAgent installation needed.
/// In Photino there is no IHostedService support, so this is started manually via Task.Run.
/// </summary>
public class PrintAgentHostedService
{
    private readonly SettingsService _settingsService;
    private readonly ILogger<PrintAgentHostedService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;
    private WebApplication? _app;

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
    /// Start the embedded PrintAgent web API with full PrintService integration.
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

                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EntegrasyonPrintAgent");
                Directory.CreateDirectory(configDir);

                var agentConfigPath = Path.Combine(configDir, "agent-config.json");
                var printersConfigPath = Path.Combine(configDir, "printers.json");
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

                // Generate API key on first run
                if (!File.Exists(agentConfigPath))
                {
                    var generatedKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    var defaultConfig = new
                    {
                        ApiKey = generatedKey,
                        AllowedOrigins = new[] { "https://localhost:5001" },
                        Port = port
                    };
                    await File.WriteAllTextAsync(agentConfigPath,
                        JsonSerializer.Serialize(defaultConfig, jsonOptions));

                    _logger.LogInformation(
                        "PrintAgent API key generated. Config: {ConfigPath}", agentConfigPath);
                }

                // Create default printer config if missing
                if (!File.Exists(printersConfigPath))
                {
                    var defaultPrinters = new
                    {
                        Printers = new[]
                        {
                            new
                            {
                                Name = "Varsayilan-Yazici",
                                Type = "Network",
                                Address = "192.168.1.50",
                                Port = 9100,
                                IsDefault = true
                            }
                        }
                    };
                    await File.WriteAllTextAsync(printersConfigPath,
                        JsonSerializer.Serialize(defaultPrinters, jsonOptions));
                }

                var builder = WebApplication.CreateBuilder();
                builder.WebHost.UseUrls($"https://localhost:{port}");

                // Load agent and printer configuration from JSON files
                builder.Configuration.AddJsonFile(agentConfigPath, optional: false, reloadOnChange: true);
                builder.Configuration.AddJsonFile(printersConfigPath, optional: false, reloadOnChange: true);

                builder.Services.Configure<AgentConfiguration>(builder.Configuration);
                builder.Services.Configure<PrinterConfiguration>(builder.Configuration);

                // Register real transport layer based on OS
                builder.Services.AddSingleton<IPrinterTransport, RawTcpPrinterTransport>();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    builder.Services.AddSingleton<ILocalPrinterTransport, WindowsUsbPrinterTransport>();
                else
                    builder.Services.AddSingleton<ILocalPrinterTransport, CupsPrinterTransport>();

                // Register the real PrintService
                builder.Services.AddSingleton<PrintService>();

                // CORS — allow all origins (agent only listens on localhost)
                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                });

                // Suppress noisy ASP.NET Core logs in embedded mode
                builder.Logging.SetMinimumLevel(LogLevel.Warning);

                _app = builder.Build();
                _app.UseCors();
                _app.UseMiddleware<ApiKeyMiddleware>();

                // Real endpoints — mirrors standalone PrintAgent Program.cs
                _app.MapGet("/health", () =>
                    Results.Ok(new AgentHealthResponse("ok", "1.0.0-embedded")));

                _app.MapGet("/printers", async (PrintService svc, CancellationToken ct) =>
                    Results.Ok(await svc.GetAllPrintersAsync(ct)));

                _app.MapGet("/printers/{name}/status", async (string name, PrintService svc, CancellationToken ct) =>
                    Results.Ok(await svc.GetPrinterStatusAsync(name, ct)));

                _app.MapPost("/print", async (PrintJobRequest request, PrintService svc, CancellationToken ct) =>
                {
                    var result = await svc.ProcessJobAsync(request, ct);
                    return result.Status.StartsWith("error")
                        ? Results.BadRequest(result)
                        : Results.Ok(result);
                });

                IsRunning = true;
                _logger.LogInformation("PrintAgent listening on https://localhost:{Port} (embedded)", port);

                await _app.RunAsync();
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
                _app = null;
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

        await _cts.CancelAsync();

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
