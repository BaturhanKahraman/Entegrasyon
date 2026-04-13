using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class SaleManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<SaleManager> logger,
    SaleMapper mapper,
    IFluentValidator fluentValidator,
    IOfficeStockManager officeStockManager) : ISaleManager
{
    public async Task<IResult> MakeSale(MakeSaleDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog("Satış yapma isteği geldi.", LogType.Sale, LogAction.Add, dto);
        await fluentValidator.ValidateAndThrowAsync(dto);

        var sale = mapper.MapToEntity(dto);
        sale.SaleNumber = await GenerateSaleNumberAsync(dbContext);
        sale.SaleDate = DateTimeOffset.UtcNow;
        sale.SaleStatus = SaleStatus.Completed;

        // Denormalize barcode ve product title — tek sorguda
        var variantIds = dto.SaleItems.Select(x => x.ProductVariantId).Distinct().ToList();
        var variantInfo = await dbContext.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Barcode, ProductTitle = v.Product.Title })
            .ToDictionaryAsync(v => v.Id);

        // Atomic stok düşme — her ürün için ayrı ayrı
        foreach (var item in sale.SaleItems)
        {
            var stockResult = await officeStockManager.DecreaseStockAtomicAsync(
                dto.BranchOfficeId, item.ProductVariantId, item.Quantity,
                StockMovementType.Sale, "Sale");

            if (!stockResult.Success)
                return new ErrorResult(stockResult.Message!);

            if (variantInfo.TryGetValue(item.ProductVariantId, out var info))
            {
                item.Barcode = info.Barcode ?? "";
                item.ProductTitle = info.ProductTitle ?? "";
            }
        }

        // Ödeme kayıtları oluştur
        var now = DateTimeOffset.UtcNow;
        foreach (var paymentDto in dto.Payments)
        {
            var payment = new SalePayment
            {
                PaymentMethodId = paymentDto.PaymentMethodId,
                Amount = paymentDto.Amount,
                CashReceived = paymentDto.CashReceived,
                ChangeGiven = paymentDto.CashReceived.HasValue
                    ? Math.Max(0, paymentDto.CashReceived.Value - paymentDto.Amount)
                    : null,
                CardAuthCode = paymentDto.CardAuthCode,
                PaidAt = now
            };
            sale.Payments.Add(payment);
        }

        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Satış başarı ile tamamlandı.", LogType.Sale, LogAction.Add, dto);
        logger.LogInformation("Sale completed: {SaleNumber}", sale.SaleNumber);
        return new SuccessResult(Messages.SaleSuccess);
    }

    public async Task<IDataResult<Pageable<SaleListDetailDto>>> GetSalesPageable(SalePageableDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = BuildSaleQuery(dbContext, dto);

        int total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.SaleDate)
            .Skip(dto.PageIndex * dto.PageSize)
            .Take(dto.PageSize)
            .Select(x => new SaleListDetailDto(
                x.Id,
                x.SaleNumber,
                x.SaleDate,
                x.SaleSource,
                x.SaleStatus,
                x.Customer != null ? x.Customer.FullName : null,
                x.SalePerson.Name + " " + x.SalePerson.Surname,
                x.SaleItems.Count(),
                x.SaleItems.Sum(si => si.Quantity),
                x.SaleItems.Sum(si => si.UnitPrice * si.Quantity * (1 + (decimal)si.TaxPercentage / 100m)),
                x.Payments.Select(p => p.PaymentMethod != null ? p.PaymentMethod.Name : "").ToList()))
            .ToListAsync();

        return new SuccessDataResult<Pageable<SaleListDetailDto>>(
            new Pageable<SaleListDetailDto>(items, dto.PageIndex, dto.PageSize, total));
    }

    public async Task<IDataResult<SaleDetailDto>> GetSaleDetailAsync(Guid saleId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sale = await dbContext.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.Payments).ThenInclude(p => p.PaymentMethod)
            .Include(s => s.Returns).ThenInclude(r => r.Items)
            .Include(s => s.Returns).ThenInclude(r => r.ReturnedBy)
            .Include(s => s.SalePerson)
            .Include(s => s.BranchOffice)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null)
            return new ErrorDataResult<SaleDetailDto>(null!, "Satış bulunamadı.");

        var items = sale.SaleItems.Select(si =>
        {
            var unitPriceWithVat = Math.Round(si.UnitPrice * (1 + (decimal)si.TaxPercentage / 100), 2);
            var lineTotalWithVat = Math.Round(si.UnitPrice * si.Quantity * (1 + (decimal)si.TaxPercentage / 100), 2);
            return new SaleDetailItemDto
            {
                Id = si.Id,
                ProductTitle = si.ProductTitle,
                Barcode = si.Barcode,
                Quantity = si.Quantity,
                UnitPriceWithVat = unitPriceWithVat,
                VatRate = (decimal)si.TaxPercentage,
                LineTotalWithVat = lineTotalWithVat,
                ReturnedQuantity = si.ReturnedQuantity
            };
        }).ToList();

        var subTotal = sale.SaleItems.Sum(si => si.UnitPrice * si.Quantity);
        var grandTotal = sale.SaleItems.Sum(si => si.UnitPrice * si.Quantity * (1 + (decimal)si.TaxPercentage / 100));
        var vatTotal = grandTotal - subTotal;

        var vatSummary = sale.SaleItems
            .GroupBy(si => si.TaxPercentage)
            .Select(g =>
            {
                var taxBase = g.Sum(si => si.UnitPrice * si.Quantity);
                var vatAmount = g.Sum(si => si.UnitPrice * si.Quantity * ((decimal)g.Key / 100));
                return new VatSummaryLineDto(
                    VatRate: (decimal)g.Key,
                    TaxBase: Math.Round(taxBase, 2),
                    VatAmount: Math.Round(vatAmount, 2),
                    Total: Math.Round(taxBase + vatAmount, 2));
            })
            .OrderBy(v => v.VatRate)
            .ToList();

        var payments = sale.Payments.Select(p => new SaleDetailPaymentDto
        {
            PaymentMethodName = p.PaymentMethod?.Name ?? "",
            PaymentMethodIcon = p.PaymentMethod?.Icon ?? "",
            Amount = p.Amount,
            CardAuthCode = p.CardAuthCode,
            PaidAt = p.PaidAt
        }).ToList();

        var returns = sale.Returns.Select(r => new SaleReturnSummaryDto
        {
            Id = r.Id,
            ReturnDate = r.ReturnDate,
            ReturnStatus = r.ReturnStatus,
            ReturnReason = r.ReturnReason,
            RefundAmount = r.RefundAmount,
            ReturnedByName = r.ReturnedBy != null ? r.ReturnedBy.Name + " " + r.ReturnedBy.Surname : "",
            ItemCount = r.Items.Count
        }).ToList();

        var dto = new SaleDetailDto
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            SaleDate = sale.SaleDate,
            SaleSource = sale.SaleSource,
            SaleStatus = sale.SaleStatus,
            CustomerName = sale.Customer?.FullName,
            SalePersonName = sale.SalePerson != null ? sale.SalePerson.Name + " " + sale.SalePerson.Surname : "",
            BranchOfficeName = sale.BranchOffice?.Name ?? "",
            Note = sale.Note,
            SubTotal = Math.Round(subTotal, 2),
            VatTotal = Math.Round(vatTotal, 2),
            GrandTotal = Math.Round(grandTotal, 2),
            GeneralDiscount = sale.GeneralDiscount,
            Items = items,
            Payments = payments,
            VatSummary = vatSummary,
            Returns = returns
        };

        return new SuccessDataResult<SaleDetailDto>(dto);
    }

    public async Task<IResult> CancelSaleAsync(Guid saleId, Guid cancelledByUserId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sale = await dbContext.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null)
            return new ErrorResult("Satış bulunamadı.");

        if (sale.SaleStatus == SaleStatus.Cancelled)
            return new ErrorResult("Bu satış zaten iptal edilmiş.");

        if (sale.SaleDate.Date != DateTimeOffset.UtcNow.Date)
            return new ErrorResult("Sadece bugünkü satışlar iptal edilebilir.");

        foreach (var item in sale.SaleItems)
        {
            var stockResult = await officeStockManager.IncreaseStockAtomicAsync(
                sale.BranchOfficeId, item.ProductVariantId, item.Quantity,
                StockMovementType.Return, "SaleCancellation");

            if (!stockResult.Success)
            {
                logger.LogWarning("Stock restore failed for variant {VariantId} on sale cancellation {SaleId}: {Message}",
                    item.ProductVariantId, saleId, stockResult.Message);
            }
        }

        sale.SaleStatus = SaleStatus.Cancelled;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Satış iptal edildi: {sale.SaleNumber}", LogType.Sale, LogAction.Delete);
        logger.LogInformation("Sale cancelled: {SaleNumber} by user {UserId}", sale.SaleNumber, cancelledByUserId);

        return new SuccessResult("Satış iptal edildi.");
    }

    public async Task<IDataResult<SaleSummaryDto>> GetSalesSummaryAsync(SalePageableDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = BuildSaleQuery(dbContext, dto);

        var activeQuery = query.Where(s => s.SaleStatus != SaleStatus.Cancelled);

        var totalSales = await activeQuery
            .SumAsync(s => s.SaleItems.Sum(si => si.UnitPrice * si.Quantity * (1 + (decimal)si.TaxPercentage / 100m)));

        var saleCount = await activeQuery.CountAsync();

        var saleIds = await activeQuery.Select(s => s.Id).ToListAsync();

        var totalReturns = await dbContext.SaleReturns
            .Where(r => r.ReturnStatus == ReturnStatus.Approved && saleIds.Contains(r.SaleId))
            .SumAsync(r => r.RefundAmount);

        var summary = new SaleSummaryDto(
            TotalSales: Math.Round(totalSales, 2),
            SaleCount: saleCount,
            AverageBasket: saleCount > 0 ? Math.Round(totalSales / saleCount, 2) : 0,
            TotalReturns: Math.Round(totalReturns, 2));

        return new SuccessDataResult<SaleSummaryDto>(summary);
    }

    private static IQueryable<Sale> BuildSaleQuery(IntegrationDbContext dbContext, SalePageableDto dto)
    {
        var query = dbContext.Sales.AsQueryable();

        if (dto.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == dto.CustomerId);
        if (dto.DateBetweenStart is not null)
            query = query.Where(x => x.SaleDate >= dto.DateBetweenStart);
        if (dto.DateBetweenEnd is not null)
            query = query.Where(x => x.SaleDate <= dto.DateBetweenEnd);
        if (dto.SalePersonId != Guid.Empty)
            query = query.Where(x => x.SalePersonId == dto.SalePersonId);
        if (dto.SaleSource.HasValue)
            query = query.Where(x => x.SaleSource == dto.SaleSource);
        if (dto.SaleStatus.HasValue)
            query = query.Where(x => x.SaleStatus == dto.SaleStatus);

        return query;
    }

    private static async Task<string> GenerateSaleNumberAsync(IntegrationDbContext dbContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prefix = $"S{today:yyyyMMdd}";

        var lastNumber = await dbContext.Sales
            .Where(s => s.SaleNumber.StartsWith(prefix))
            .OrderByDescending(s => s.SaleNumber)
            .Select(s => s.SaleNumber)
            .FirstOrDefaultAsync();

        var sequence = 1;
        if (lastNumber is not null && int.TryParse(lastNumber[prefix.Length..], out var lastSeq))
            sequence = lastSeq + 1;

        return $"{prefix}{sequence:D4}";
    }
}
