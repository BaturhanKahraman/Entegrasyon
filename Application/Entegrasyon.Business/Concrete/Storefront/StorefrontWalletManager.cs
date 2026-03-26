using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontWalletManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontWalletManager
{
    public async Task<IDataResult<StorefrontWallet>> GetOrCreateWalletAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var wallet = await dbContext.StorefrontWallets
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.CustomerId == customerId);

        if (wallet is null)
        {
            wallet = new StorefrontWallet
            {
                TenantId = tenantId,
                CustomerId = customerId,
                Balance = 0
            };
            dbContext.StorefrontWallets.Add(wallet);
            await dbContext.SaveChangesAsync();
        }

        return new SuccessDataResult<StorefrontWallet>(wallet);
    }

    public async Task<IResult> CreditAsync(int tenantId, int customerId, decimal amount, WalletTransactionType type, string? referenceId, string? description)
    {
        if (amount <= 0)
            return new ErrorResult("Tutar sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var wallet = await dbContext.StorefrontWallets
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.CustomerId == customerId);

        if (wallet is null)
        {
            wallet = new StorefrontWallet
            {
                TenantId = tenantId,
                CustomerId = customerId,
                Balance = 0
            };
            dbContext.StorefrontWallets.Add(wallet);
            await dbContext.SaveChangesAsync();
        }

        var balanceBefore = wallet.Balance;
        wallet.Balance += amount;

        var transaction = new StorefrontWalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            TransactionType = type,
            ReferenceId = referenceId,
            Description = description,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance
        };

        dbContext.StorefrontWallets.Update(wallet);
        dbContext.StorefrontWalletTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Bakiye yuklendi.");
    }

    public async Task<IResult> DebitAsync(int tenantId, int customerId, decimal amount, WalletTransactionType type, string? referenceId, string? description)
    {
        if (amount <= 0)
            return new ErrorResult("Tutar sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var wallet = await dbContext.StorefrontWallets
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.CustomerId == customerId);

        if (wallet is null || wallet.Balance < amount)
            return new ErrorResult("Yetersiz bakiye.");

        var balanceBefore = wallet.Balance;
        wallet.Balance -= amount;

        var transaction = new StorefrontWalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -amount,
            TransactionType = type,
            ReferenceId = referenceId,
            Description = description,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance
        };

        dbContext.StorefrontWallets.Update(wallet);
        dbContext.StorefrontWalletTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Odeme basarili.");
    }

    public async Task<IDataResult<List<StorefrontWalletTransaction>>> GetTransactionsAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var wallet = await dbContext.StorefrontWallets
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.CustomerId == customerId);

        if (wallet is null)
            return new SuccessDataResult<List<StorefrontWalletTransaction>>(new List<StorefrontWalletTransaction>());

        var transactions = await dbContext.StorefrontWalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontWalletTransaction>>(transactions);
    }
}
