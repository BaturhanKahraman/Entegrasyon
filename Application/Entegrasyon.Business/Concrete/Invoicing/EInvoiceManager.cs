using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.EInvoice;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Invoicing;

public sealed class EInvoiceManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IFluentValidator fluentValidator,
    IEInvoiceIntegratorClient integratorClient) : IEInvoiceManager
{
    public async Task<IDataResult<Guid>> CreateInvoice(CreateEInvoiceDto dto)
    {
        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        // 2. Business Rules — su anda ek kural yok, ileride LogicRunner eklenebilir

        // 3. Execution
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog("E-Fatura olusturma istegi geldi.", LogType.Invoice, LogAction.Add, dto);

        var invoice = MapToEntity(dto);
        CalculateLineTotals(invoice);
        invoice.XmlContent = UblTrXmlBuilder.Build(invoice);

        dbContext.EInvoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("E-Fatura basariyla olusturuldu.", LogType.Invoice, LogAction.Add,
            new { invoice.Id, invoice.InvoiceNumber });

        return new SuccessDataResult<Guid>(invoice.Id, "Fatura basariyla olusturuldu.");
    }

    public async Task<IDataResult<List<Guid>>> CreateBulkInvoices(BulkInvoiceDto dto)
    {
        var createdIds = new List<Guid>();
        var errors = new List<string>();

        foreach (var invoiceDto in dto.Invoices)
        {
            var result = await CreateInvoice(invoiceDto);
            if (result.Success)
                createdIds.Add(result.Data);
            else
                errors.Add(result.Message ?? "Bilinmeyen hata.");
        }

        if (errors.Count > 0 && createdIds.Count == 0)
            return new ErrorDataResult<List<Guid>>(createdIds,
                $"Toplu fatura olusturulamadi: {string.Join("; ", errors)}");

        return new SuccessDataResult<List<Guid>>(createdIds,
            $"{createdIds.Count} fatura olusturuldu.");
    }

    public async Task<IDataResult<Pageable<EInvoiceListDto>>> GetInvoices(EInvoiceFilterDto filter)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.EInvoices.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.InvoiceType.HasValue)
            query = query.Where(x => x.InvoiceType == filter.InvoiceType.Value);
        if (filter.StartDate.HasValue)
            query = query.Where(x => x.IssueDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            query = query.Where(x => x.IssueDate <= filter.EndDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(x =>
                x.InvoiceNumber.Contains(filter.SearchTerm) ||
                x.CustomerTitle.Contains(filter.SearchTerm));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.IssueDate)
            .Skip(filter.PageIndex * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new EInvoiceListDto(
                x.Id,
                x.InvoiceNumber,
                x.InvoiceType,
                x.Status,
                x.CustomerTitle,
                x.IssueDate,
                x.GrandTotal,
                x.IntegratorProvider))
            .ToListAsync();

        return new SuccessDataResult<Pageable<EInvoiceListDto>>(
            new Pageable<EInvoiceListDto>(items, filter.PageIndex, filter.PageSize, total));
    }

    public async Task<IDataResult<EInvoiceSummaryDto>> GetInvoiceSummary(EInvoiceFilterDto filter)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Liste sayfasiyla ayni filtre (Status haric — KPI kartlari status'a gore sabit
        // sayim yapar; status filtresi sadece tablo gorunumunu daraltir, ozet kartlari degil).
        var query = dbContext.EInvoices.AsQueryable();

        if (filter.InvoiceType.HasValue)
            query = query.Where(x => x.InvoiceType == filter.InvoiceType.Value);
        if (filter.StartDate.HasValue)
            query = query.Where(x => x.IssueDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            query = query.Where(x => x.IssueDate <= filter.EndDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(x =>
                x.InvoiceNumber.Contains(filter.SearchTerm) ||
                x.CustomerTitle.Contains(filter.SearchTerm));

        // Tek round-trip: Draft + Sent sayilarini conditional-count ile cek (no-tracking,
        // entity materialize edilmez — scalar projeksiyon, IX_EInvoices_Status uzerinden).
        var counts = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                DraftCount = g.Count(x => x.Status == EInvoiceStatus.Draft),
                SentCount = g.Count(x => x.Status == EInvoiceStatus.Sent)
            })
            .FirstOrDefaultAsync();

        // Bu ay kesilen (iptal haric) genel toplam — IssueDate ay penceresi,
        // IX_EInvoices_IssueDate uzerinden range scan.
        var now = DateTime.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var nextMonthStart = monthStart.AddMonths(1);

        var monthGrandTotal = await query
            .Where(x => x.IssueDate >= monthStart
                        && x.IssueDate < nextMonthStart
                        && x.Status != EInvoiceStatus.Cancelled)
            .SumAsync(x => (decimal?)x.GrandTotal) ?? 0m;

        var summary = new EInvoiceSummaryDto(
            counts?.DraftCount ?? 0,
            counts?.SentCount ?? 0,
            monthGrandTotal);

        return new SuccessDataResult<EInvoiceSummaryDto>(summary);
    }

    public async Task<IDataResult<EInvoiceDetailDto>> GetInvoiceDetail(Guid invoiceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var invoice = await dbContext.EInvoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == invoiceId);

        if (invoice is null)
            return new ErrorDataResult<EInvoiceDetailDto>(null!, "Fatura bulunamadı.");

        var detail = new EInvoiceDetailDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceType,
            invoice.Status,
            invoice.CustomerTaxId,
            invoice.CustomerTitle,
            invoice.IssueDate,
            invoice.TotalAmount,
            invoice.TaxAmount,
            invoice.GrandTotal,
            invoice.GibUuid,
            invoice.PdfUrl,
            invoice.SaleId,
            invoice.MarketplaceOrderId,
            invoice.IntegratorProvider,
            invoice.ErrorMessage,
            invoice.SentAt,
            invoice.AcceptedAt,
            invoice.CancelledAt,
            invoice.Lines.Select(l => new EInvoiceLineDetailDto(
                l.Id, l.ProductName, l.Quantity, l.UnitPrice,
                l.TaxRate, l.TaxAmount, l.LineTotal)).ToList());

        return new SuccessDataResult<EInvoiceDetailDto>(detail);
    }

    public async Task<IResult> CancelInvoice(Guid invoiceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var invoice = await dbContext.EInvoices.FindAsync(invoiceId);
        if (invoice is null)
            return new ErrorResult("Fatura bulunamadı.");

        if (invoice.Status == EInvoiceStatus.Cancelled)
            return new ErrorResult("Fatura zaten iptal edilmis.");

        if (!string.IsNullOrEmpty(invoice.GibUuid))
        {
            var cancelResult = await integratorClient.CancelInvoice(invoice.GibUuid);
            if (!cancelResult.Success)
                return cancelResult;
        }

        invoice.Status = EInvoiceStatus.Cancelled;
        invoice.CancelledAt = DateTimeOffset.UtcNow;
        dbContext.EInvoices.Update(invoice);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("E-Fatura iptal edildi.", LogType.Invoice, LogAction.Delete,
            new { invoiceId });

        return new SuccessResult("Fatura basariyla iptal edildi.");
    }

    public async Task<IResult> SendToGib(Guid invoiceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var invoice = await dbContext.EInvoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == invoiceId);

        if (invoice is null)
            return new ErrorResult("Fatura bulunamadı.");

        if (invoice.Status != EInvoiceStatus.Draft)
            return new ErrorResult("Sadece taslak durumundaki faturalar gonderilebilir.");

        var sendResult = await integratorClient.SendInvoice(invoice);
        if (!sendResult.Success)
        {
            invoice.ErrorMessage = sendResult.Message;
            dbContext.EInvoices.Update(invoice);
            await dbContext.SaveChangesAsync();
            return new ErrorResult(sendResult.Message ?? "Fatura gonderilemedi.");
        }

        invoice.GibUuid = sendResult.Data;
        invoice.Status = EInvoiceStatus.Sent;
        invoice.SentAt = DateTimeOffset.UtcNow;
        invoice.ErrorMessage = null;
        dbContext.EInvoices.Update(invoice);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("E-Fatura GIB'e gonderildi.", LogType.Invoice, LogAction.Update,
            new { invoiceId, invoice.GibUuid });

        return new SuccessResult("Fatura basariyla gonderildi.");
    }

    public async Task<IDataResult<byte[]>> DownloadPdf(Guid invoiceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var invoice = await dbContext.EInvoices.FindAsync(invoiceId);
        if (invoice is null)
            return new ErrorDataResult<byte[]>([], "Fatura bulunamadı.");

        if (string.IsNullOrEmpty(invoice.GibUuid))
            return new ErrorDataResult<byte[]>([], "Fatura henuz GIB'e gonderilmemis.");

        return await integratorClient.DownloadPdf(invoice.GibUuid);
    }

    public async Task<IDataResult<CreateEInvoiceDto>> GetInvoiceFromSale(Guid saleId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sale = await dbContext.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null)
            return new ErrorDataResult<CreateEInvoiceDto>(null!, "Satis bulunamadı.");

        var dto = new CreateEInvoiceDto
        {
            InvoiceType = EInvoiceType.EArsiv,
            CustomerTaxId = string.Empty,
            CustomerTitle = sale.Customer?.FullName ?? "Bilinmeyen Musteri",
            IssueDate = DateTimeOffset.UtcNow,
            SaleId = saleId,
            Lines = sale.SaleItems.Select(si => new CreateEInvoiceLineDto(
                $"Urun #{si.ProductVariantId.ToString()[..8]}",
                si.Quantity,
                si.UnitPrice,
                (int)si.TaxPercentage)).ToList()
        };

        return new SuccessDataResult<CreateEInvoiceDto>(dto);
    }

    private static EInvoice MapToEntity(CreateEInvoiceDto dto)
    {
        var invoice = new EInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = GenerateInvoiceNumber(),
            InvoiceType = dto.InvoiceType,
            Status = EInvoiceStatus.Draft,
            CustomerTaxId = dto.CustomerTaxId,
            CustomerTitle = dto.CustomerTitle,
            IssueDate = dto.IssueDate,
            IntegratorProvider = dto.IntegratorProvider,
            SaleId = dto.SaleId,
            MarketplaceOrderId = dto.MarketplaceOrderId,
            Lines = dto.Lines.Select(l => new EInvoiceLine
            {
                Id = Guid.NewGuid(),
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TaxRate = l.TaxRate
            }).ToList()
        };
        return invoice;
    }

    private static void CalculateLineTotals(EInvoice invoice)
    {
        decimal totalAmount = 0;
        decimal taxAmount = 0;

        foreach (var line in invoice.Lines)
        {
            var lineSubtotal = line.UnitPrice * line.Quantity;
            line.TaxAmount = Math.Round(lineSubtotal * line.TaxRate / 100m, 2);
            line.LineTotal = lineSubtotal + line.TaxAmount;
            totalAmount += lineSubtotal;
            taxAmount += line.TaxAmount;
        }

        invoice.TotalAmount = totalAmount;
        invoice.TaxAmount = taxAmount;
        invoice.GrandTotal = totalAmount + taxAmount;
    }

    private static string GenerateInvoiceNumber()
    {
        var prefix = "EFT";
        var year = DateTime.UtcNow.Year;
        var seq = Guid.NewGuid().ToString("N")[..9].ToUpperInvariant();
        return $"{prefix}{year}{seq}";
    }
}
