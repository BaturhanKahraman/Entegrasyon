using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
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
    public async Task<IDataResult<Guid>> MakeSale(MakeSaleDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog("Satış yapma isteği geldi.", LogType.Sale, LogAction.Add, dto);
        await fluentValidator.ValidateAndThrowAsync(dto);

        var sale = mapper.MapToEntity(dto);
        sale.SaleNumber = await GenerateSaleNumberAsync(dbContext);
        sale.ReturnCode = await GenerateReturnCodeAsync(dbContext);
        sale.SaleDate = DateTimeOffset.UtcNow;
        sale.SaleStatus = SaleStatus.Completed;

        // Denormalize barcode ve product title — tek sorguda
        var variantIds = dto.SaleItems.Select(x => x.ProductVariantId).Distinct().ToList();
        var variantInfo = await dbContext.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Barcode, ProductTitle = v.Product.Title })
            .ToDictionaryAsync(v => v.Id);

        // Atomic stok düşme — başarısızlık compensating transaction ile telafi edilir
        var decreasedItems = new List<(Guid variantId, int quantity)>();
        foreach (var item in sale.SaleItems)
        {
            var stockResult = await officeStockManager.DecreaseStockAtomicAsync(
                dto.BranchOfficeId, item.ProductVariantId, item.Quantity,
                StockMovementType.Sale, "Sale");

            if (!stockResult.Success)
            {
                // Daha önce düşürülmüş stokları geri al (compensating transaction)
                foreach (var (vid, qty) in decreasedItems)
                    await officeStockManager.IncreaseStockAtomicAsync(
                        dto.BranchOfficeId, vid, qty,
                        StockMovementType.Return, "SaleRollback");
                return new ErrorDataResult<Guid>(Guid.Empty, stockResult.Message!);
            }

            decreasedItems.Add((item.ProductVariantId, item.Quantity));

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

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch
        {
            // Sale persist fail → düşürülmüş tüm stokları geri al
            foreach (var (vid, qty) in decreasedItems)
                await officeStockManager.IncreaseStockAtomicAsync(
                    dto.BranchOfficeId, vid, qty,
                    StockMovementType.Return, "SaleRollback");
            throw;
        }

        var successMessage = dto.GeneralDiscount > 0m
            ? $"Satış başarı ile tamamlandı. Sepet indirimi: {dto.GeneralDiscount:N2} TL"
            : "Satış başarı ile tamamlandı.";
        await applicationLogManager.AddLog(successMessage, LogType.Sale, LogAction.Add, dto);
        logger.LogInformation("Sale completed: {SaleNumber}", sale.SaleNumber);
        return new SuccessDataResult<Guid>(sale.Id, Messages.SaleSuccess);
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
                x.SaleItems.Sum(si => Math.Max(0m, si.UnitPrice * si.Quantity - (si.DiscountAmount ?? 0m)) * (1 + (decimal)si.TaxPercentage / 100m)),
                x.Payments.Select(p => p.PaymentMethod != null ? p.PaymentMethod.Name : "").ToList()))
            .ToListAsync();

        return new SuccessDataResult<Pageable<SaleListDetailDto>>(
            new Pageable<SaleListDetailDto>(items, dto.PageIndex, dto.PageSize, total));
    }

    public async Task<IDataResult<SaleDetailDto>> GetSaleDetailAsync(Guid saleId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sale = await dbContext.Sales
            .Include(s => s.SaleItems).ThenInclude(si => si.ProductVariant).ThenInclude(pv => pv.Product)
            .Include(s => s.SaleItems).ThenInclude(si => si.ProductVariant).ThenInclude(pv => pv.ProductVariantAttributes)
            .Include(s => s.Payments).ThenInclude(p => p.PaymentMethod)
            .Include(s => s.Returns).ThenInclude(r => r.Items)
            .Include(s => s.Returns).ThenInclude(r => r.ReturnedBy)
            .Include(s => s.SalePerson)
            .Include(s => s.BranchOffice)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null)
            return new ErrorDataResult<SaleDetailDto>(null!, "Satış bulunamadı.");

        foreach (var si in sale.SaleItems)
        {
            if (si.DiscountAmount.HasValue && si.DiscountAmount.Value > si.UnitPrice * si.Quantity)
            {
                logger.LogWarning(
                    "SaleItem {SaleItemId} has DiscountAmount {Discount} exceeding line total {LineTotal} on Sale {SaleId}",
                    si.Id, si.DiscountAmount.Value, si.UnitPrice * si.Quantity, sale.Id);
            }
        }

        var items = sale.SaleItems.Select(si =>
        {
            var unitPriceWithVat = Math.Round(si.UnitPrice * (1 + (decimal)si.TaxPercentage / 100), 2);
            var netAfterDiscount = NetAfterDiscount(si);
            var lineTotalWithVat = Math.Round(netAfterDiscount * (1 + (decimal)si.TaxPercentage / 100), 2);
            var variantDisplayName = si.ProductVariant != null
                ? VariantNameExtensions.ResolveDisplayName(
                    si.ProductVariant.Name,
                    si.ProductVariant.ProductVariantAttributes.Select((a, i) =>
                        new VariantAttributeLite(a.CategoryAttributeValue, a.IsVarianter, a.IsSlicer, i)),
                    si.ProductVariant.Product?.Title ?? si.ProductTitle)
                : si.ProductTitle;

            return new SaleDetailItemDto
            {
                Id = si.Id,
                ProductTitle = si.ProductTitle,
                VariantDisplayName = variantDisplayName,
                Barcode = si.Barcode,
                Quantity = si.Quantity,
                UnitPriceWithVat = unitPriceWithVat,
                VatRate = (decimal)si.TaxPercentage,
                LineTotalWithVat = lineTotalWithVat,
                ReturnedQuantity = si.ReturnedQuantity
            };
        }).ToList();

        // Net satır toplamı = UnitPrice*Qty - DiscountAmount (kalem + pro-rata genel indirim birleşik)
        var subTotal = sale.SaleItems.Sum(si => NetAfterDiscount(si));

        // KDV dahil grand total
        var grandTotal = sale.SaleItems.Sum(si =>
            NetAfterDiscount(si) * (1 + (decimal)si.TaxPercentage / 100));

        var vatTotal = grandTotal - subTotal;

        var vatSummary = sale.SaleItems
            .GroupBy(si => si.TaxPercentage)
            .Select(g =>
            {
                var taxBase = g.Sum(si => NetAfterDiscount(si));
                var vatAmount = g.Sum(si => NetAfterDiscount(si) * ((decimal)g.Key / 100));
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
            ReturnReason = r.ReturnReason != null ? r.ReturnReason.Name : (r.CustomReason ?? ""),
            RefundAmount = r.RefundAmount,
            ReturnedByName = r.ReturnedBy != null ? r.ReturnedBy.Name + " " + r.ReturnedBy.Surname : "",
            ItemCount = r.Items.Count
        }).ToList();

        var dto = new SaleDetailDto
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            ReturnCode = sale.ReturnCode,
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

    public async Task<IDataResult<SaleDetailDto>> GetSaleByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorDataResult<SaleDetailDto>(null!, "Kod boş olamaz.");

        var normalized = code.Trim().ToUpperInvariant();

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleId = await dbContext.Sales
            .AsNoTracking()
            .Where(s => s.ReturnCode == normalized || s.SaleNumber == normalized)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (saleId is null)
            return new ErrorDataResult<SaleDetailDto>(null!, "Satış bulunamadı.");

        return await GetSaleDetailAsync(saleId.Value);
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

        var saleLocalDate = TurkeyTime.StartOfDay(sale.SaleDate);
        if (saleLocalDate != TurkeyTime.StartOfToday)
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
            .SumAsync(s => s.SaleItems.Sum(si =>
                Math.Max(0m, si.UnitPrice * si.Quantity - (si.DiscountAmount ?? 0m))
                * (1 + (decimal)si.TaxPercentage / 100m)));

        var saleCount = await activeQuery.CountAsync();

        var saleIds = await activeQuery.Select(s => s.Id).ToListAsync();

        var totalReturns = await dbContext.SaleReturns
            .Where(r => r.ReturnStatus == ReturnStatus.Approved && r.SaleId.HasValue && saleIds.Contains(r.SaleId.Value))
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

    private static decimal NetAfterDiscount(SaleItem si)
        => Math.Max(0m, si.UnitPrice * si.Quantity - (si.DiscountAmount ?? 0m));

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

    private static Task<string> GenerateReturnCodeAsync(IntegrationDbContext dbContext)
        => ReturnCodeGenerator.GenerateUniqueAsync(code =>
            dbContext.Sales.AnyAsync(s => s.ReturnCode == code));
}
