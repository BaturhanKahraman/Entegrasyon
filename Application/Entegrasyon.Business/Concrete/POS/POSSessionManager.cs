using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.POS;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.POS;

public sealed class POSSessionManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ISaleManager saleManager,
    IFluentValidator fluentValidator,
    IApplicationLogManager applicationLogManager,
    ILogger<POSSessionManager> logger) : IPOSSessionManager
{
    public async Task<IDataResult<POSSession>> OpenSessionAsync(OpenSessionDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        var logicResult = LogicRunner.Run(await CheckNoOpenSessionExists(dbContext, dto.BranchOfficeId, dto.TerminalId));
        if (logicResult != null)
            return new ErrorDataResult<POSSession>(null!, logicResult.Message!);

        // 3. Execution
        var session = new POSSession
        {
            BranchOfficeId = dto.BranchOfficeId,
            CashierId = dto.CashierId,
            OpeningCash = dto.OpeningCash,
            OpenedAt = DateTimeOffset.UtcNow,
            Status = POSSessionStatus.Open,
            TerminalId = dto.TerminalId
        };

        dbContext.POSSessions.Add(session);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"POS oturumu acildi. Sube: {dto.BranchOfficeId}, Terminal: {dto.TerminalId ?? "yok"}",
            LogType.Sale, LogAction.Add, dto);
        logger.LogInformation("POS session opened for BranchOffice {BranchOfficeId}, Terminal {TerminalId}",
            dto.BranchOfficeId, dto.TerminalId);

        return new SuccessDataResult<POSSession>(session, "POS oturumu basariyla acildi.");
    }

    public async Task<IResult> CloseSessionAsync(CloseSessionDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        var session = await dbContext.POSSessions
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == dto.SessionId);

        if (session == null)
            return new ErrorResult("Oturum bulunamadi.");

        var logicResult = LogicRunner.Run(CheckSessionIsOpen(session));
        if (logicResult != null)
            return new ErrorResult(logicResult.Message!);

        // 3. Execution
        var transactions = await dbContext.POSTransactions
            .Where(t => t.POSSessionId == dto.SessionId)
            .ToListAsync();

        var cashMovements = await dbContext.CashMovements
            .Where(cm => cm.POSSessionId == dto.SessionId)
            .ToListAsync();

        // TODO: PaymentMethod moved to SalePayment — cash total should filter by payment method from SalePayment
        var totalCash = transactions
            .Sum(t => t.CashReceived - t.ChangeGiven);

        var totalCashIn = cashMovements
            .Where(cm => cm.MovementType == CashMovementType.CashIn)
            .Sum(cm => cm.Amount);

        var totalCashOut = cashMovements
            .Where(cm => cm.MovementType == CashMovementType.CashOut)
            .Sum(cm => cm.Amount);

        var expectedCash = session.OpeningCash + totalCash + totalCashIn - totalCashOut;

        session.Status = POSSessionStatus.Closed;
        session.ClosedAt = DateTimeOffset.UtcNow;
        session.ClosingCash = dto.ClosingCash;

        await dbContext.SaveChangesAsync();

        var difference = dto.ClosingCash - expectedCash;

        await applicationLogManager.AddLog(
            $"POS oturumu kapatildi. Beklenen: {expectedCash:F2}, Sayilan: {dto.ClosingCash:F2}, Fark: {difference:F2}",
            LogType.Sale, LogAction.Update, dto);
        logger.LogInformation("POS session {SessionId} closed. Expected: {Expected}, Counted: {Counted}, Diff: {Difference}",
            dto.SessionId, expectedCash, dto.ClosingCash, difference);

        return new SuccessResult("POS oturumu basariyla kapatildi.");
    }

    public async Task<IDataResult<POSSession>> GetActiveSessionAsync(int branchOfficeId, string? terminalId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.POSSessions
            .Where(s => s.BranchOfficeId == branchOfficeId && s.Status == POSSessionStatus.Open);

        if (terminalId != null)
            query = query.Where(s => s.TerminalId == terminalId);

        var session = await query.FirstOrDefaultAsync();

        return session != null
            ? new SuccessDataResult<POSSession>(session)
            : new ErrorDataResult<POSSession>(null!, "Acik oturum bulunamadi.");
    }

    public async Task<IDataResult<POSTransaction>> RecordTransactionAsync(POSTransactionDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        var session = await dbContext.POSSessions
            .FirstOrDefaultAsync(s => s.Id == dto.POSSessionId);

        if (session == null)
            return new ErrorDataResult<POSTransaction>(null!, "Oturum bulunamadi.");

        var logicResult = LogicRunner.Run(CheckSessionIsOpen(session));
        if (logicResult != null)
            return new ErrorDataResult<POSTransaction>(null!, logicResult.Message!);

        // 3. Execution - Call ISaleManager.MakeSale which triggers stock sync to all marketplaces
        var saleResult = await saleManager.MakeSale(dto.Sale);
        if (!saleResult.Success)
            return new ErrorDataResult<POSTransaction>(null!, saleResult.Message!);

        // Calculate total from sale items
        var saleTotal = dto.Sale.SaleItems.Sum(si => si.UnitPrice * si.Quantity);
        var changeGiven = dto.PaymentMethod == PaymentMethod.Cash
            ? Math.Max(0, dto.CashReceived - saleTotal)
            : 0m;

        var transaction = new POSTransaction
        {
            POSSessionId = dto.POSSessionId,
            SaleId = saleResult.Data, // Actual Sale Id from MakeSale
            // TODO: PaymentMethod moved to SalePayment — will be wired in later task
            CashReceived = dto.CashReceived,
            ChangeGiven = changeGiven,
            // TODO: CardAuthCode moved to SalePayment — will be wired in later task
            TransactionAt = DateTimeOffset.UtcNow
        };

        dbContext.POSTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"POS islem kaydedildi. Odeme: {dto.PaymentMethod}, Tutar: {saleTotal:F2}",
            LogType.Sale, LogAction.Add, dto);
        logger.LogInformation("POS transaction recorded for session {SessionId}, PaymentMethod: {PaymentMethod}",
            dto.POSSessionId, dto.PaymentMethod);

        return new SuccessDataResult<POSTransaction>(transaction, "Islem basariyla kaydedildi.");
    }

    public async Task<IResult> AddTransactionRecordAsync(
        long sessionId,
        Guid saleId,
        decimal cashReceived,
        decimal changeGiven)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var session = await dbContext.POSSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null)
            return new ErrorResult("Oturum bulunamadi.");

        var logicResult = LogicRunner.Run(CheckSessionIsOpen(session));
        if (logicResult != null)
            return new ErrorResult(logicResult.Message!);

        var saleExists = await dbContext.Sales.AnyAsync(s => s.Id == saleId);
        if (!saleExists)
            return new ErrorResult("Satis bulunamadi.");

        dbContext.POSTransactions.Add(new POSTransaction
        {
            POSSessionId = sessionId,
            SaleId = saleId,
            CashReceived = cashReceived,
            ChangeGiven = changeGiven,
            TransactionAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        logger.LogInformation("POS transaction link created — session={SessionId} sale={SaleId}", sessionId, saleId);
        return new SuccessResult("Islem baglandi.");
    }

    public async Task<IResult> AddCashMovementAsync(AddCashMovementDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 1. Validation
        await fluentValidator.ValidateAndThrowAsync(dto);

        // 2. Business Rules
        var session = await dbContext.POSSessions
            .FirstOrDefaultAsync(s => s.Id == dto.POSSessionId);

        if (session == null)
            return new ErrorResult("Oturum bulunamadi.");

        var logicResult = LogicRunner.Run(CheckSessionIsOpen(session));
        if (logicResult != null)
            return new ErrorResult(logicResult.Message!);

        // 3. Execution
        var movement = new CashMovement
        {
            POSSessionId = dto.POSSessionId,
            MovementType = dto.MovementType,
            Amount = dto.Amount,
            Reason = dto.Reason,
            CreatedByUserId = session.CashierId
        };

        dbContext.CashMovements.Add(movement);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Kasa hareketi eklendi. Tip: {dto.MovementType}, Tutar: {dto.Amount:F2}",
            LogType.Sale, LogAction.Add, dto);
        logger.LogInformation("Cash movement added to session {SessionId}: {Type} {Amount}",
            dto.POSSessionId, dto.MovementType, dto.Amount);

        return new SuccessResult("Kasa hareketi basariyla eklendi.");
    }

    public async Task<IDataResult<POSSummaryDto>> GetSessionSummaryAsync(long sessionId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var session = await dbContext.POSSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return new ErrorDataResult<POSSummaryDto>(null!, "Oturum bulunamadi.");

        var saleIds = await dbContext.POSTransactions
            .Where(t => t.POSSessionId == sessionId)
            .Select(t => t.SaleId)
            .ToListAsync();

        var transactions = await dbContext.POSTransactions
            .Where(t => t.POSSessionId == sessionId)
            .Include(t => t.Sale)
            .ThenInclude(s => s.SaleItems)
            .ToListAsync();

        var salePayments = await dbContext.SalePayments
            .Include(p => p.PaymentMethod)
            .Where(p => saleIds.Contains(p.SaleId))
            .ToListAsync();

        var cashMovements = await dbContext.CashMovements
            .Where(cm => cm.POSSessionId == sessionId)
            .ToListAsync();

        var totalCash = salePayments
            .Where(p => p.PaymentMethod.SystemCode == "Cash")
            .Sum(p => p.Amount);

        var totalCard = salePayments
            .Where(p => p.PaymentMethod.SystemCode != "Cash")
            .Sum(p => p.Amount);

        var totalSales = transactions
            .Sum(t => t.Sale?.SaleItems?.Sum(si => si.UnitPrice * si.Quantity) ?? 0m);

        var totalCashIn = cashMovements
            .Where(cm => cm.MovementType == CashMovementType.CashIn)
            .Sum(cm => cm.Amount);

        var totalCashOut = cashMovements
            .Where(cm => cm.MovementType == CashMovementType.CashOut)
            .Sum(cm => cm.Amount);

        var expectedCash = session.OpeningCash + totalCash + totalCashIn - totalCashOut;
        var difference = session.ClosingCash.HasValue ? session.ClosingCash.Value - expectedCash : (decimal?)null;

        var summary = new POSSummaryDto(
            SessionId: sessionId,
            OpeningCash: session.OpeningCash,
            TotalSales: totalSales,
            TotalCash: totalCash,
            TotalCard: totalCard,
            TransactionCount: transactions.Count,
            ExpectedCash: expectedCash,
            ClosingCash: session.ClosingCash,
            Difference: difference);

        return new SuccessDataResult<POSSummaryDto>(summary);
    }

    public async Task<IDataResult<POSSummaryDto>> GetDailySummaryAsync(int branchOfficeId, DateOnly date)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var startOfDay = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        var sessions = await dbContext.POSSessions
            .Where(s => s.BranchOfficeId == branchOfficeId && s.OpenedAt >= startOfDay && s.OpenedAt < endOfDay)
            .ToListAsync();

        if (!sessions.Any())
            return new ErrorDataResult<POSSummaryDto>(null!, "Belirtilen tarihte oturum bulunamadi.");

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var transactions = await dbContext.POSTransactions
            .Where(t => sessionIds.Contains(t.POSSessionId))
            .Include(t => t.Sale)
            .ThenInclude(s => s.SaleItems)
            .ToListAsync();

        var dailySaleIds = transactions.Select(t => t.SaleId).ToList();

        var dailySalePayments = await dbContext.SalePayments
            .Include(p => p.PaymentMethod)
            .Where(p => dailySaleIds.Contains(p.SaleId))
            .ToListAsync();

        var cashMovements = await dbContext.CashMovements
            .Where(cm => sessionIds.Contains(cm.POSSessionId))
            .ToListAsync();

        var totalCash = dailySalePayments
            .Where(p => p.PaymentMethod.SystemCode == "Cash")
            .Sum(p => p.Amount);

        var totalCard = dailySalePayments
            .Where(p => p.PaymentMethod.SystemCode != "Cash")
            .Sum(p => p.Amount);

        var totalSales = transactions
            .Sum(t => t.Sale?.SaleItems?.Sum(si => si.UnitPrice * si.Quantity) ?? 0m);

        var openingCash = sessions.Sum(s => s.OpeningCash);

        var totalCashIn = cashMovements
            .Where(cm => cm.MovementType == CashMovementType.CashIn)
            .Sum(cm => cm.Amount);

        var totalCashOut = cashMovements
            .Where(cm => cm.MovementType == CashMovementType.CashOut)
            .Sum(cm => cm.Amount);

        var expectedCash = openingCash + totalCash + totalCashIn - totalCashOut;

        var summary = new POSSummaryDto(
            SessionId: 0,
            OpeningCash: openingCash,
            TotalSales: totalSales,
            TotalCash: totalCash,
            TotalCard: totalCard,
            TransactionCount: transactions.Count,
            ExpectedCash: expectedCash,
            ClosingCash: null,
            Difference: null);

        return new SuccessDataResult<POSSummaryDto>(summary);
    }

    public async Task<IDataResult<POSReportDto>> GetXReportAsync(long sessionId)
    {
        return await BuildReportAsync(sessionId, isZReport: false);
    }

    public async Task<IDataResult<POSReportDto>> GetZReportAsync(long sessionId)
    {
        return await BuildReportAsync(sessionId, isZReport: true);
    }

    private async Task<IDataResult<POSReportDto>> BuildReportAsync(long sessionId, bool isZReport)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var session = await dbContext.POSSessions
            .Include(s => s.Cashier)
            .Include(s => s.CashMovements)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
            return new ErrorDataResult<POSReportDto>(null!, "Kasa oturumu bulunamadi.");

        var saleIds = await dbContext.POSTransactions
            .Where(t => t.POSSessionId == sessionId)
            .Select(t => t.SaleId)
            .ToListAsync();

        var sales = await dbContext.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.Payments).ThenInclude(p => p.PaymentMethod)
            .Where(s => saleIds.Contains(s.Id) && s.SaleStatus != SaleStatus.Cancelled)
            .ToListAsync();

        var paymentBreakdown = sales
            .SelectMany(s => s.Payments)
            .GroupBy(p => p.PaymentMethod.Name)
            .Select(g => new PaymentMethodSummaryDto(g.Key, g.Count(), g.Sum(p => p.Amount)))
            .ToList();

        var vatBreakdown = sales
            .SelectMany(s => s.SaleItems)
            .GroupBy(si => (decimal)si.TaxPercentage)
            .Select(g =>
            {
                var taxBase = g.Sum(si => si.UnitPrice * si.Quantity);
                var vatAmount = taxBase * g.Key / 100m;
                return new VatSummaryLineDto(g.Key, Math.Round(taxBase, 2), Math.Round(vatAmount, 2), Math.Round(taxBase + vatAmount, 2));
            })
            .OrderBy(v => v.VatRate)
            .ToList();

        var totalSales = sales.Sum(s => s.Payments.Sum(p => p.Amount));
        var totalCash = sales
            .SelectMany(s => s.Payments)
            .Where(p => p.PaymentMethod.SystemCode == "Cash")
            .Sum(p => p.Amount);

        var cashMovementsNet = session.CashMovements
            .Sum(cm => cm.MovementType == CashMovementType.CashIn ? cm.Amount : -cm.Amount);

        var totalReturns = await dbContext.SaleReturns
            .Where(r => r.SaleId.HasValue && saleIds.Contains(r.SaleId.Value) && r.ReturnStatus == ReturnStatus.Approved)
            .SumAsync(r => r.RefundAmount);

        var expectedCash = session.OpeningCash + totalCash + cashMovementsNet;

        var report = new POSReportDto
        {
            SessionId = sessionId,
            CashierName = $"{session.Cashier.Name} {session.Cashier.Surname}",
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            OpeningCash = session.OpeningCash,
            TransactionCount = sales.Count,
            TotalSales = totalSales,
            TotalReturns = totalReturns,
            NetSales = totalSales - totalReturns,
            PaymentBreakdown = paymentBreakdown,
            VatBreakdown = vatBreakdown,
            ExpectedCash = expectedCash,
            ActualCash = isZReport ? session.ClosingCash : null,
            CashDifference = isZReport && session.ClosingCash.HasValue
                ? session.ClosingCash.Value - expectedCash
                : null
        };

        return new SuccessDataResult<POSReportDto>(report);
    }

    private static async Task<IResult> CheckNoOpenSessionExists(IntegrationDbContext dbContext, int branchOfficeId, string? terminalId)
    {
        var query = dbContext.POSSessions
            .Where(s => s.BranchOfficeId == branchOfficeId && s.Status == POSSessionStatus.Open);

        if (terminalId != null)
            query = query.Where(s => s.TerminalId == terminalId);

        var exists = await query.AnyAsync();
        return exists
            ? new ErrorResult("Bu sube/terminal icin zaten acik bir oturum var.")
            : new SuccessResult();
    }

    private static IResult CheckSessionIsOpen(POSSession session)
    {
        return session.Status != POSSessionStatus.Open
            ? new ErrorResult("Oturum acik degil. Mevcut durum: " + session.Status)
            : new SuccessResult();
    }
}
