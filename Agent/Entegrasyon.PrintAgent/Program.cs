using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Entegrasyon.PrintAgent;
using Entegrasyon.PrintAgent.Configuration;
using Entegrasyon.PrintAgent.Contracts;
using Entegrasyon.PrintAgent.Transport;

var builder = WebApplication.CreateBuilder(args);

var useMock = args.Contains("--mock") || builder.Configuration.GetValue("UseMock", false);

// Konfigürasyon dosyaları
var configDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "EntegrasyonPrintAgent");

Directory.CreateDirectory(configDir);

var agentConfigPath = Path.Combine(configDir, "agent-config.json");
var printersConfigPath = Path.Combine(configDir, "printers.json");

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

// İlk çalışmada API key üret
if (!File.Exists(agentConfigPath))
{
    var generatedKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var defaultConfig = new { ApiKey = generatedKey, AllowedOrigins = new[] { "https://localhost:5001" }, Port = 19100 };
    await File.WriteAllTextAsync(agentConfigPath, JsonSerializer.Serialize(defaultConfig, jsonOptions));

    Console.WriteLine("============================================");
    Console.WriteLine($"  API Key: {generatedKey}");
    Console.WriteLine($"  Konfigurasyon: {agentConfigPath}");
    Console.WriteLine("  Bu key'i Blazor ayarlarına girin.");
    Console.WriteLine("============================================");
}

if (!File.Exists(printersConfigPath))
{
    var defaultPrinters = new { Printers = new[] { new { Name = "Varsayilan-Yazici", Type = "Network", Address = "192.168.1.50", Port = 9100, IsDefault = true } } };
    await File.WriteAllTextAsync(printersConfigPath, JsonSerializer.Serialize(defaultPrinters, jsonOptions));
}

builder.Configuration.AddJsonFile(agentConfigPath, optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile(printersConfigPath, optional: false, reloadOnChange: true);

// Servisleri kaydet
builder.Services.Configure<AgentConfiguration>(builder.Configuration);
builder.Services.Configure<PrinterConfiguration>(builder.Configuration);

if (useMock)
{
    Console.WriteLine("  [MOCK MOD] Sanal yazıcılar kullanılıyor");
    builder.Services.AddSingleton<IPrinterTransport, MockPrinterTransport>();
    builder.Services.AddSingleton<ILocalPrinterTransport, MockPrinterTransport>();
}
else
{
    builder.Services.AddSingleton<IPrinterTransport, RawTcpPrinterTransport>();

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        builder.Services.AddSingleton<ILocalPrinterTransport, WindowsUsbPrinterTransport>();
    else
        builder.Services.AddSingleton<ILocalPrinterTransport, CupsPrinterTransport>();
}

builder.Services.AddSingleton<PrintService>();

// CORS — tüm origin'lere izin ver (agent sadece localhost'ta dinliyor)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Kestrel — sadece localhost'ta dinle
var port = builder.Configuration.GetValue("Port", 19100);
builder.WebHost.UseUrls($"https://localhost:{port}");

var app = builder.Build();

app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();

// Endpoints
app.MapGet("/health", () =>
    Results.Ok(new AgentHealthResponse("ok", "1.0.0")));

app.MapGet("/printers", async (PrintService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetAllPrintersAsync(ct)));

app.MapGet("/printers/{name}/status", async (string name, PrintService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetPrinterStatusAsync(name, ct)));

app.MapPost("/print", async (PrintJobRequest request, PrintService svc, CancellationToken ct) =>
{
    var result = await svc.ProcessJobAsync(request, ct);
    return result.Status.StartsWith("error") ? Results.BadRequest(result) : Results.Ok(result);
});

Console.WriteLine($"  Dinleniyor: https://localhost:{port}");
Console.WriteLine($"  Mock mod: {(useMock ? "AKTIF" : "PASIF")}");

app.Run();
