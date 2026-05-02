using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Printing;

[Authorize]
public sealed class PrintBatchSseController(IPrintBatchProgressBroadcaster broadcaster) : Controller
{
    [HttpGet("/events/print-batch/{batchId:guid}")]
    public ServerSentEventsResult<PrintBatchProgressEvent> Stream(Guid batchId, HttpContext httpContext)
    {
        return TypedResults.ServerSentEvents(StreamCore(batchId, httpContext.RequestAborted));
    }

    private async IAsyncEnumerable<SseItem<PrintBatchProgressEvent>> StreamCore(
        Guid batchId, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var evt in broadcaster.Subscribe(batchId, ct))
        {
            yield return new SseItem<PrintBatchProgressEvent>(evt, "progress")
            {
                ReconnectionInterval = TimeSpan.FromSeconds(5)
            };
        }
    }
}
