using System.Collections.Concurrent;
using System.Threading.Channels;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete.Printing;

/// <summary>
/// Bellek-içi pub/sub. Yazdırma batch ilerlemesini SSE abonelerine push eder.
/// Multi-tenant uyumlu — her batchId tenant başına benzersiz Guid; abonelik
/// batchId üzerinden, çakışma yok. Redis'e geçilmek istenirse aynı interface kalır.
/// </summary>
public sealed class PrintBatchProgressBroadcaster : IPrintBatchProgressBroadcaster
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<Channel<PrintBatchProgressEvent>>> _subscribers = new();

    public void Publish(PrintBatchProgressEvent payload)
    {
        if (!_subscribers.TryGetValue(payload.BatchId, out var channels)) return;
        foreach (var ch in channels)
            ch.Writer.TryWrite(payload);
    }

    public async IAsyncEnumerable<PrintBatchProgressEvent> Subscribe(
        Guid batchId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateBounded<PrintBatchProgressEvent>(new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        var bag = _subscribers.GetOrAdd(batchId, _ => []);
        bag.Add(channel);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct))
                yield return evt;
        }
        finally
        {
            channel.Writer.TryComplete();
            // Cleanup — kullanılmayan kanalları periyodik olarak temizleyebiliriz;
            // ConcurrentBag remove desteklemediği için bag'i sadeleştirme background job'la yapılır.
        }
    }
}
