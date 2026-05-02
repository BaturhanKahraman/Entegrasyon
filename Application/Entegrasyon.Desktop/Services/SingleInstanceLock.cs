using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Cross-platform tek-instance kilidi + URL forwarding.
/// Windows: NamedPipeServerStream + named mutex.
/// Linux/macOS: NamedPipeServerStream Unix socket'i kullanır (.NET runtime soyutlar).
/// </summary>
public sealed class SingleInstanceLock : IDisposable
{
    private const string PipeName = "EntegrasyonDesktopUrlPipe";
    private const string MutexName = "Global\\EntegrasyonDesktopMutex";

    private Mutex? _mutex;
    private CancellationTokenSource? _serverCts;
    private Task? _serverTask;
    private readonly ILogger<SingleInstanceLock>? _logger;

    public event Action<string>? UrlReceived;

    public SingleInstanceLock(ILogger<SingleInstanceLock>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Kilidi tutmaya çalış. true = primary instance (UI ya da deep-link sahibi),
    /// false = ikinci instance (URL forward edilir).
    /// </summary>
    public bool TryAcquire()
    {
        try
        {
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, out var createdNew);
            return createdNew;
        }
        catch (UnauthorizedAccessException)
        {
            // Global mutex restricted — fallback: try local mutex
            _mutex = new Mutex(initiallyOwned: true, name: MutexName.Replace("Global\\", ""), out var createdNew);
            return createdNew;
        }
    }

    /// <summary>
    /// Primary instance: yeni URL'leri dinle ve event olarak yayınla.
    /// </summary>
    public void StartServer()
    {
        if (_serverTask is not null) return;
        _serverCts = new CancellationTokenSource();
        _serverTask = Task.Run(() => ServerLoopAsync(_serverCts.Token));
        _logger?.LogInformation("Single-instance pipe server started: {Pipe}", PipeName);
    }

    /// <summary>
    /// Secondary instance: birinci instance'a URL'i ilet ve çık.
    /// </summary>
    public static async Task<bool> ForwardUrlAsync(string url, TimeSpan? timeout = null)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync((int)(timeout ?? TimeSpan.FromSeconds(3)).TotalMilliseconds);
            var bytes = Encoding.UTF8.GetBytes(url);
            await client.WriteAsync(bytes);
            await client.FlushAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ServerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(ct);

                using var ms = new MemoryStream();
                var buffer = new byte[4096];
                int read;
                while ((read = await server.ReadAsync(buffer, ct)) > 0)
                    ms.Write(buffer, 0, read);

                var url = Encoding.UTF8.GetString(ms.ToArray());
                _logger?.LogInformation("Pipe received URL: {Url}", url);
                UrlReceived?.Invoke(url);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Pipe server iteration failed; retrying");
                await Task.Delay(500, ct);
            }
        }
    }

    public void Dispose()
    {
        _serverCts?.Cancel();
        try { _serverTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _serverCts?.Dispose();
        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch { }
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
