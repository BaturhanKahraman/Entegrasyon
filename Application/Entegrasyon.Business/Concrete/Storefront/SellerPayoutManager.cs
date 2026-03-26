using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class SellerPayoutManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : ISellerPayoutManager
{
    public async Task<IDataResult<SellerBalance>> GetBalanceAsync(int sellerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var balance = await dbContext.SellerBalances
            .FirstOrDefaultAsync(b => b.SellerId == sellerId);

        return balance is not null
            ? new SuccessDataResult<SellerBalance>(balance)
            : new ErrorDataResult<SellerBalance>(null!, "Bakiye bulunamadi.");
    }

    public async Task<IDataResult<List<SellerTransaction>>> GetTransactionsAsync(int sellerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var transactions = await dbContext.SellerTransactions
            .Where(t => t.SellerId == sellerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<SellerTransaction>>(transactions);
    }

    public async Task<IResult> RequestPayoutAsync(int sellerId, decimal amount)
    {
        if (amount <= 0)
            return new ErrorResult("Odeme tutari sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var balance = await dbContext.SellerBalances
            .FirstOrDefaultAsync(b => b.SellerId == sellerId);

        if (balance is null)
            return new ErrorResult("Bakiye bulunamadi.");

        var availableBalance = balance.CurrentBalance - balance.PendingAmount;
        if (amount > availableBalance)
            return new ErrorResult($"Yetersiz bakiye. Kullanilabilir: {availableBalance:C2}");

        var seller = await dbContext.Sellers.FindAsync(sellerId);
        if (seller is null)
            return new ErrorResult("Satici bulunamadi.");

        var payout = new PayoutRequest
        {
            SellerId = sellerId,
            Amount = amount,
            Iban = seller.Iban ?? "",
            Status = PayoutStatus.Pending
        };

        balance.PendingAmount += amount;

        dbContext.PayoutRequests.Add(payout);
        dbContext.SellerBalances.Update(balance);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Odeme talebi olusturuldu.");
    }

    public async Task<IDataResult<List<PayoutRequest>>> GetPayoutRequestsAsync(int sellerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var requests = await dbContext.PayoutRequests
            .Where(p => p.SellerId == sellerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<PayoutRequest>>(requests);
    }

    public async Task<IDataResult<List<PayoutRequest>>> GetAllPendingPayoutsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sellerIds = await dbContext.Sellers
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.Id)
            .ToListAsync();

        var requests = await dbContext.PayoutRequests
            .Include(p => p.Seller)
            .Where(p => sellerIds.Contains(p.SellerId) && p.Status == PayoutStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<PayoutRequest>>(requests);
    }

    public async Task<IResult> ProcessPayoutAsync(int payoutId, bool approve, string? note)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var payout = await dbContext.PayoutRequests
            .FirstOrDefaultAsync(p => p.Id == payoutId);

        if (payout is null)
            return new ErrorResult("Odeme talebi bulunamadi.");

        if (payout.Status != PayoutStatus.Pending)
            return new ErrorResult("Bu talep zaten islenmis.");

        var balance = await dbContext.SellerBalances
            .FirstOrDefaultAsync(b => b.SellerId == payout.SellerId);

        if (balance is null)
            return new ErrorResult("Satici bakiyesi bulunamadi.");

        if (approve)
        {
            payout.Status = PayoutStatus.Completed;
            payout.ProcessedAt = DateTimeOffset.UtcNow;
            payout.Note = note;

            balance.PendingAmount -= payout.Amount;
            balance.CurrentBalance -= payout.Amount;
            balance.TotalPaidOut += payout.Amount;

            var tx = new SellerTransaction
            {
                SellerId = payout.SellerId,
                Amount = -payout.Amount,
                TransactionType = "Payout",
                Description = $"Odeme talebi onaylandi #{payoutId}",
                BalanceAfter = balance.CurrentBalance
            };
            dbContext.SellerTransactions.Add(tx);
        }
        else
        {
            payout.Status = PayoutStatus.Rejected;
            payout.ProcessedAt = DateTimeOffset.UtcNow;
            payout.Note = note;

            balance.PendingAmount -= payout.Amount;
        }

        dbContext.PayoutRequests.Update(payout);
        dbContext.SellerBalances.Update(balance);
        await dbContext.SaveChangesAsync();

        return new SuccessResult(approve ? "Odeme talebi onaylandi." : "Odeme talebi reddedildi.");
    }
}
