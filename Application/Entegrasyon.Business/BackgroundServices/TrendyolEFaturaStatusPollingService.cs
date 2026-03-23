using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Trendyol e-Fatura durum takip servisi.
/// Her 5 dakikada:
/// 1. "Shipped" durumunda + faturasi olmayan siparisler icin fatura olustur
/// 2. Processing/Created/Sent durumundaki fatura kayitlarinin durumunu sorgula
/// 3. Onaylanmis + marketplace'e gonderilmemis faturalarin PDF URL'ini al ve marketplace'e gonder
/// </summary>
public class TrendyolEFaturaStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrendyolEFaturaStatusPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Baslangicta biraz bekle — diger servisler ayaga kalksin
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during e-Fatura status polling");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollAndProcessAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var eFaturaService = scope.ServiceProvider.GetRequiredService<ITrendyolEFaturaService>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<ITrendyolInvoiceService>();

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        // 1. Islenmekte olan fatura kayitlarinin durumunu sorgula
        var pendingRecords = await dbContext.EFaturaRecords
            .Where(r => r.Status == EFaturaStatus.Processing
                     || r.Status == EFaturaStatus.Created
                     || r.Status == EFaturaStatus.Sent)
            .Take(20) // Batch limit
            .ToListAsync(ct);

        foreach (var record in pendingRecords)
        {
            try
            {
                await eFaturaService.CheckInvoiceStatusAsync(record.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to check invoice status for {RecordId}", record.Id);
            }
        }

        // 2. Onaylanmis + marketplace'e gonderilmemis faturalar → PDF URL al + marketplace'e gonder
        var approvedRecords = await dbContext.EFaturaRecords
            .Include(r => r.Order)
            .Where(r => r.Status == EFaturaStatus.Approved
                     && !r.InvoiceLinkSentToMarketplace
                     && r.Order.MarketPlaceId == TrendyolMarketPlaceId)
            .Take(10)
            .ToListAsync(ct);

        foreach (var record in approvedRecords)
        {
            try
            {
                // PDF URL al
                var pdfResult = await eFaturaService.GetInvoicePdfUrlAsync(record.Id, ct);
                if (!pdfResult.Success || string.IsNullOrEmpty(pdfResult.Data))
                {
                    logger.LogWarning("PDF URL alinamadi: {RecordId}", record.Id);
                    continue;
                }

                // Marketplace'e fatura linkini gonder (mevcut TrendyolInvoiceService)
                if (record.Order.ShipmentPackageId.HasValue)
                {
                    var sendResult = await invoiceService.SendInvoiceLinkAsync(
                        record.Order.ShipmentPackageId.Value,
                        pdfResult.Data,
                        invoiceNumber: record.InvoiceId);

                    if (sendResult.Success)
                    {
                        // Track olarak guncelle
                        var trackContext = await contextFactory.CreateDbContextAsync(ct);
                        var trackRecord = await trackContext.EFaturaRecords
                            .FirstOrDefaultAsync(r => r.Id == record.Id, ct);
                        if (trackRecord is not null)
                        {
                            trackRecord.InvoiceLinkSentToMarketplace = true;
                            await trackContext.SaveChangesAsync(ct);
                        }

                        logger.LogInformation(
                            "Invoice link sent to Trendyol marketplace. RecordId={RecordId}, PackageId={PackageId}",
                            record.Id, record.Order.ShipmentPackageId);
                    }
                    else
                    {
                        logger.LogWarning("Invoice link send failed: {Message}", sendResult.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process approved invoice {RecordId}", record.Id);
            }
        }

        if (pendingRecords.Count > 0 || approvedRecords.Count > 0)
        {
            logger.LogInformation(
                "e-Fatura poll completed. StatusChecked={Pending}, LinksSent={Approved}",
                pendingRecords.Count, approvedRecords.Count(r => r.InvoiceLinkSentToMarketplace));
        }
    }
}
