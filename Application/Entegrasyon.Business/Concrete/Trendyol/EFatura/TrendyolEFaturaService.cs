using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Invoices;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol.EFatura;

/// <summary>
/// Trendyol e-Faturam entegrasyonu — e-arsiv ve e-fatura islemleri.
/// </summary>
public sealed class TrendyolEFaturaService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolEFaturaApiClient apiClient,
    TrendyolEFaturaInvoiceBuilder invoiceBuilder,
    IApplicationLogManager applicationLogManager,
    ILogger<TrendyolEFaturaService> logger) : ITrendyolEFaturaService
{
    public async Task<IDataResult<bool>> CheckTaxPayerAsync(string taxId, CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.GetAsync($"api/invoice/taxpayers/{taxId}?showDeleted=false", ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Mukellef sorgulama başarısız: {Status}", response.StatusCode);
                return new SuccessDataResult<bool>(false, "Mukellef sorgulanamadi, e-arsiv kullanilacak.");
            }

            var taxpayers = await response.Content.ReadFromJsonAsync<List<EFaturaTaxPayerInfo>>(cancellationToken: ct);

            var isEInvoiceTaxPayer = taxpayers?.Any(t =>
                string.Equals(t.AliasType, "INVOICE", StringComparison.OrdinalIgnoreCase)) ?? false;

            return new SuccessDataResult<bool>(isEInvoiceTaxPayer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mukellef sorgulama hatasi: {TaxId}", taxId);
            return new SuccessDataResult<bool>(false, "Mukellef sorgulama hatasi, e-arsiv kullanilacak.");
        }
    }

    public async Task<IDataResult<EFaturaRecord>> CreateInvoiceForOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            return new ErrorDataResult<EFaturaRecord>(null!, "Sipariş bulunamadı.");

        // Token cache'den companyId ve userId al
        var tokenInfo = TrendyolEFaturaApiClient.GetCachedTokenInfo(TrendyolMarketPlaceId);
        if (tokenInfo is null || string.IsNullOrEmpty(tokenInfo.CustomerAccessToken))
            return new ErrorDataResult<EFaturaRecord>(null!, "e-Fatura oturumu acilmamis. Lutfen ayarlari kontrol edin.");

        // Mukellef kontrolu (BillingAddress'ten TaxId alinmali — su an null)
        // TODO: Order entity'ye TaxId alanıeklenmeli. Simdilik e-arsiv kullanilacak.
        var isEInvoice = false;

        // Request body olustur
        var request = invoiceBuilder.BuildFromOrder(
            order,
            tokenInfo.CompanyId,
            tokenInfo.UserId,
            isEInvoice);

        // API'ye gonder
        var endpoint = isEInvoice
            ? "api/invoice/documents/outgoing-einvoice"
            : "api/invoice/documents/earchive";

        var response = await apiClient.PostAsync(endpoint, request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            var errorMsg = $"e-Fatura olusturulamadi. HTTP {(int)response.StatusCode}: {errorBody}";
            logger.LogError("TrendyolEFaturaService: {Message}", errorMsg);
            await applicationLogManager.AddLog(errorMsg, Entity.Logs.LogType.Invoice, Entity.Logs.LogAction.Add, new { orderId });

            return new ErrorDataResult<EFaturaRecord>(null!, errorMsg);
        }

        var createResult = await response.Content.ReadFromJsonAsync<EFaturaCreateResponse>(cancellationToken: ct);

        if (createResult is null)
            return new ErrorDataResult<EFaturaRecord>(null!, "API yaniti okunamadi.");

        // EFaturaRecord kaydet
        var record = new EFaturaRecord
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            InvoiceUuid = createResult.InvoiceUuid,
            InvoiceId = createResult.InvoiceId,
            EnvelopeId = createResult.EnvelopeId,
            InvoiceType = isEInvoice ? EFaturaType.EInvoice : EFaturaType.EArchive,
            Status = MapApiStatus(createResult.Status),
            ApiStatusCode = createResult.Status,
            PayableAmountKurus = createResult.PayableAmount,
            TaxAmountKurus = createResult.TaxAmount,
            LocalReferenceId = request.LocalReferenceId
        };

        dbContext.Set<EFaturaRecord>().Add(record);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("e-Fatura olusturuldu. OrderId={OrderId}, UUID={UUID}, Type={Type}",
            orderId, record.InvoiceUuid, record.InvoiceType);

        return new SuccessDataResult<EFaturaRecord>(record, "Fatura olusturuldu.");
    }

    public async Task<IDataResult<EFaturaRecord>> CheckInvoiceStatusAsync(Guid invoiceRecordId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var record = await dbContext.Set<EFaturaRecord>()
            .FirstOrDefaultAsync(r => r.Id == invoiceRecordId, ct);

        if (record is null)
            return new ErrorDataResult<EFaturaRecord>(null!, "Fatura kaydi bulunamadı.");

        if (string.IsNullOrEmpty(record.InvoiceUuid))
            return new ErrorDataResult<EFaturaRecord>(null!, "Fatura UUID'si bos.");

        var statusEndpoint = record.InvoiceType == EFaturaType.EInvoice
            ? $"api/invoice/documents/outgoing-einvoice/status/{record.InvoiceUuid}"
            : $"api/invoice/documents/earchive/status/{record.InvoiceUuid}";

        var response = await apiClient.GetAsync(statusEndpoint, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Fatura durum sorgulama başarısız: {Status} {Body}", response.StatusCode, errorBody);
            return new ErrorDataResult<EFaturaRecord>(record, $"Durum sorgulama hatasi: {response.StatusCode}");
        }

        var statusResult = await response.Content.ReadFromJsonAsync<EFaturaStatusResponse>(cancellationToken: ct);

        if (statusResult is not null)
        {
            record.ApiStatusCode = statusResult.Status;
            record.Status = MapApiStatus(statusResult.Status);

            if (record.Status == EFaturaStatus.Approved)
                record.ApprovedAt = DateTimeOffset.UtcNow;

            if (record.Status == EFaturaStatus.Sent)
                record.SentToGibAt ??= DateTimeOffset.UtcNow;

            if (record.Status == EFaturaStatus.Error)
                record.ErrorMessage = $"API status: {statusResult.Status}, GIB: {statusResult.GibStatus}";

            await dbContext.SaveChangesAsync(ct);
        }

        return new SuccessDataResult<EFaturaRecord>(record);
    }

    public async Task<IResult> CancelInvoiceAsync(Guid invoiceRecordId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var record = await dbContext.Set<EFaturaRecord>()
            .FirstOrDefaultAsync(r => r.Id == invoiceRecordId, ct);

        if (record is null)
            return new ErrorResult("Fatura kaydi bulunamadı.");

        if (record.InvoiceType != EFaturaType.EArchive)
            return new ErrorResult("Sadece e-arsiv faturalar iptal edilebilir.");

        if (string.IsNullOrEmpty(record.InvoiceUuid))
            return new ErrorResult("Fatura UUID'si bos.");

        var tokenInfo = TrendyolEFaturaApiClient.GetCachedTokenInfo(TrendyolMarketPlaceId);
        if (tokenInfo is null)
            return new ErrorResult("e-Fatura oturumu acilmamis.");

        var cancelRequest = new EFaturaCancelRequest(record.InvoiceUuid, tokenInfo.CompanyId);
        var response = await apiClient.PostAsync("api/invoice/documents/earchive/cancel", cancelRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return new ErrorResult($"Fatura iptal başarısız: {response.StatusCode} - {errorBody}");
        }

        record.Status = EFaturaStatus.Cancelled;
        record.ApiStatusCode = 305;
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("e-Arsiv fatura iptal edildi. RecordId={RecordId}, UUID={UUID}",
            invoiceRecordId, record.InvoiceUuid);

        return new SuccessResult("Fatura iptal edildi.");
    }

    public async Task<IDataResult<string>> GetInvoicePdfUrlAsync(Guid invoiceRecordId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var record = await dbContext.Set<EFaturaRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == invoiceRecordId, ct);

        if (record is null)
            return new ErrorDataResult<string>(null!, "Fatura kaydi bulunamadı.");

        // Onceden indirilmis URL varsa dondur
        if (!string.IsNullOrEmpty(record.PdfDownloadUrl))
            return new SuccessDataResult<string>(record.PdfDownloadUrl);

        if (string.IsNullOrEmpty(record.InvoiceUuid))
            return new ErrorDataResult<string>(null!, "Fatura UUID'si bos.");

        var tokenInfo = TrendyolEFaturaApiClient.GetCachedTokenInfo(TrendyolMarketPlaceId);
        if (tokenInfo is null)
            return new ErrorDataResult<string>(null!, "e-Fatura oturumu acilmamis.");

        var documentType = record.InvoiceType == EFaturaType.EInvoice ? "EINVOICE" : "EARCHIVE";
        var downloadRequest = new EFaturaDownloadRequest(documentType, "pdf", record.InvoiceUuid, tokenInfo.CompanyId);

        var response = await apiClient.PostAsync("api/invoice/documents/download/permanent-url", downloadRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return new ErrorDataResult<string>(null!, $"PDF URL alinamadi: {response.StatusCode}");
        }

        var pdfUrl = await response.Content.ReadAsStringAsync(ct);
        pdfUrl = pdfUrl.Trim('"'); // JSON string olarak donebilir

        // URL'i DB'ye kaydet
        var trackRecord = await dbContext.Set<EFaturaRecord>()
            .FirstOrDefaultAsync(r => r.Id == invoiceRecordId, ct);
        if (trackRecord is not null)
        {
            trackRecord.PdfDownloadUrl = pdfUrl;
            await dbContext.SaveChangesAsync(ct);
        }

        return new SuccessDataResult<string>(pdfUrl);
    }

    public async Task<IDataResult<int>> GetRemainingCreditsAsync(CancellationToken ct = default)
    {
        var tokenInfo = TrendyolEFaturaApiClient.GetCachedTokenInfo(TrendyolMarketPlaceId);
        if (tokenInfo is null)
            return new ErrorDataResult<int>(0, "e-Fatura oturumu acilmamis.");

        // partnerId ayardan okunmali — su an tokenInfo'dan partnerCustomerId kullaniyoruz
        var url = $"api/invoice/partners/0/credits/remaining/customers/{tokenInfo.PartnerCustomerId}";
        var response = await apiClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            return new ErrorDataResult<int>(0, $"Kontor sorgulanamadi: {response.StatusCode}");

        var creditResult = await response.Content.ReadFromJsonAsync<EFaturaRemainingCreditResponse>(cancellationToken: ct);
        return new SuccessDataResult<int>(creditResult?.RemainingCredit ?? 0);
    }

    private static EFaturaStatus MapApiStatus(int apiStatus) => apiStatus switch
    {
        10 or 20 => EFaturaStatus.Processing,
        29 => EFaturaStatus.Error,
        30 => EFaturaStatus.Created,
        40 or 50 => EFaturaStatus.Sent,
        100 or 105 => EFaturaStatus.Error,
        200 or 205 => EFaturaStatus.Approved,
        305 => EFaturaStatus.Cancelled,
        405 => EFaturaStatus.Error,
        _ => EFaturaStatus.Processing
    };
}
