using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Printing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Printing;

public sealed record CreatePrintBatchRequest(List<Guid> VariantIds, int Copies = 1);

public sealed record CreatePrintBatchResponse(
    Guid BatchId,
    int TotalItems,
    string OneTimeBatchToken,
    string DeepLink);

public sealed record DevicePrintBatchItemDto(
    int Order, string PrinterLanguage, string ZplContent, byte[]? RawBytes, string Description);

public sealed record DevicePrintBatchResponse(
    Guid BatchId, int TotalItems, List<DevicePrintBatchItemDto> Items);

public sealed record PrintStatusUpdateDto(int Order, bool Printed, string? ErrorMessage);

public sealed record SubmitStatusRequest(List<PrintStatusUpdateDto> Updates);

public class PrintBatchController(
    IPrintBatchManager printBatchManager,
    IBatchTokenService batchTokenService,
    ILabelService labelService,
    ITenantContext tenantContext) : Controller
{
    [HttpPost("/print/batch")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CreatePrintBatchRequest request)
    {
        if (request is null || request.VariantIds is null || request.VariantIds.Count == 0)
            return BadRequest(new { error = "En az bir varyant ID gerekli." });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "Kullanıcı kimliği alınamadı." });

        var tenantId = tenantContext.IsInitialized ? tenantContext.TenantId : 1;

        var labelResult = await labelService.GenerateBulkLabels(request.VariantIds);
        if (!labelResult.Success || labelResult.Data is null || labelResult.Data.Count == 0)
            return BadRequest(new { error = labelResult.Message ?? "Etiket üretilemedi." });

        var copies = Math.Max(1, request.Copies);
        var drafts = new List<PrintBatchItemDraft>();
        var order = 1;
        foreach (var job in labelResult.Data)
        {
            for (int c = 0; c < copies; c++)
            {
                drafts.Add(new PrintBatchItemDraft(
                    Order: order++,
                    PrinterLanguage: job.PrinterLanguage ?? "ZPL",
                    ZplContent: job.ZplContent ?? string.Empty,
                    RawBytes: job.RawBytes,
                    Description: job.JobLabel ?? string.Empty));
            }
        }

        var result = await printBatchManager.CreateAsync(tenantId, userId, drafts);
        if (!result.Success)
            return BadRequest(new { error = result.Message });

        var deepLink = $"entegrasyon-print://batch/{result.Data.BatchId}?t={Uri.EscapeDataString(result.Data.OneTimeBatchToken)}";

        return Ok(new CreatePrintBatchResponse(
            result.Data.BatchId,
            result.Data.TotalItems,
            result.Data.OneTimeBatchToken,
            deepLink));
    }

    [HttpGet("/api/print/batch/{batchId:guid}")]
    public async Task<IActionResult> GetForDevice(Guid batchId)
    {
        if (!TryAuthorizeDevice(batchId, out var deviceId, out var tenantId, out var error))
            return error!;

        var result = await printBatchManager.GetForDeviceAsync(batchId, deviceId, tenantId);
        if (!result.Success)
            return NotFound(new { error = result.Message });

        var batch = result.Data;
        var items = batch.Items
            .OrderBy(i => i.Order)
            .Select(i => new DevicePrintBatchItemDto(
                i.Order, i.PrinterLanguage, i.ZplContent, i.RawBytes, i.Description))
            .ToList();

        return Ok(new DevicePrintBatchResponse(batch.Id, batch.TotalItems, items));
    }

    [HttpPost("/api/print/batch/{batchId:guid}/status")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SubmitStatus(Guid batchId, [FromBody] SubmitStatusRequest request)
    {
        if (request is null || request.Updates is null || request.Updates.Count == 0)
            return BadRequest(new { error = "Güncelleme verisi yok." });

        if (!TryAuthorizeDevice(batchId, out var deviceId, out _, out var error))
            return error!;

        var updates = request.Updates
            .Select(u => new PrintBatchItemStatusUpdate(u.Order, u.Printed, u.ErrorMessage))
            .ToList();

        var result = await printBatchManager.RecordItemStatusAsync(batchId, deviceId, updates);
        return result.Success
            ? Ok(new { ok = true })
            : BadRequest(new { error = result.Message });
    }

    private bool TryAuthorizeDevice(
        Guid batchId, out int deviceId, out int tenantId, out IActionResult? error)
    {
        deviceId = 0;
        tenantId = 0;
        error = null;

        var deviceIdStr = User.FindFirst("DeviceId")?.Value;
        var tenantIdStr = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(deviceIdStr, out deviceId) || !int.TryParse(tenantIdStr, out tenantId))
        {
            error = Unauthorized(new { error = "Cihaz kimliği gerekli (Bearer dev_xxx)." });
            return false;
        }

        var token = Request.Headers["X-Batch-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
        {
            error = Unauthorized(new { error = "X-Batch-Token header gerekli." });
            return false;
        }

        var validation = batchTokenService.Validate(token);
        if (!validation.IsValid || validation.Payload is null)
        {
            error = Unauthorized(new { error = validation.Error ?? "Geçersiz batch token." });
            return false;
        }

        if (validation.Payload.BatchId != batchId)
        {
            error = Unauthorized(new { error = "Token bu batch için geçerli değil." });
            return false;
        }

        if (validation.Payload.TenantId != tenantId)
        {
            error = Unauthorized(new { error = "Token kiracı uyumsuzluğu." });
            return false;
        }

        return true;
    }
}
