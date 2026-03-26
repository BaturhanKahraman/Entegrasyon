using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontLoyaltyManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontLoyaltyManager
{
    public async Task<IDataResult<StorefrontLoyaltyPoints>> GetBalanceAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var loyalty = await dbContext.StorefrontLoyaltyPoints
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.CustomerId == customerId);

        if (loyalty is null)
        {
            loyalty = new StorefrontLoyaltyPoints
            {
                TenantId = tenantId,
                CustomerId = customerId,
                TotalEarned = 0,
                TotalSpent = 0,
                CurrentBalance = 0
            };
            dbContext.StorefrontLoyaltyPoints.Add(loyalty);
            await dbContext.SaveChangesAsync();
        }

        return new SuccessDataResult<StorefrontLoyaltyPoints>(loyalty);
    }

    public async Task<IResult> EarnPointsAsync(int tenantId, int customerId, int points, string type, string? referenceId, string? description)
    {
        if (points <= 0)
            return new ErrorResult("Puan sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var loyalty = await dbContext.StorefrontLoyaltyPoints
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.CustomerId == customerId);

        if (loyalty is null)
        {
            loyalty = new StorefrontLoyaltyPoints
            {
                TenantId = tenantId,
                CustomerId = customerId,
                TotalEarned = 0,
                TotalSpent = 0,
                CurrentBalance = 0
            };
            dbContext.StorefrontLoyaltyPoints.Add(loyalty);
            await dbContext.SaveChangesAsync();
        }

        loyalty.TotalEarned += points;
        loyalty.CurrentBalance += points;
        dbContext.StorefrontLoyaltyPoints.Update(loyalty);

        var transaction = new StorefrontLoyaltyTransaction
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Points = points,
            TransactionType = type,
            ReferenceId = referenceId,
            Description = description
        };

        dbContext.StorefrontLoyaltyTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return new SuccessResult($"{points} puan kazanildi.");
    }

    public async Task<IResult> RedeemPointsAsync(int tenantId, int customerId, int points, string? referenceId)
    {
        if (points <= 0)
            return new ErrorResult("Kullanilacak puan sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var settings = await dbContext.StorefrontSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings is not null && points < settings.LoyaltyMinRedemption)
            return new ErrorResult($"Minimum {settings.LoyaltyMinRedemption} puan kullanilabilir.");

        var loyalty = await dbContext.StorefrontLoyaltyPoints
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.CustomerId == customerId);

        if (loyalty is null || loyalty.CurrentBalance < points)
            return new ErrorResult("Yetersiz puan bakiyesi.");

        loyalty.TotalSpent += points;
        loyalty.CurrentBalance -= points;
        dbContext.StorefrontLoyaltyPoints.Update(loyalty);

        var transaction = new StorefrontLoyaltyTransaction
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Points = -points,
            TransactionType = "Redemption",
            ReferenceId = referenceId,
            Description = $"{points} puan kullanildi."
        };

        dbContext.StorefrontLoyaltyTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return new SuccessResult($"{points} puan kullanildi.");
    }

    public async Task<IDataResult<List<StorefrontLoyaltyTransaction>>> GetTransactionsAsync(int tenantId, int customerId, int count = 20)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var transactions = await dbContext.StorefrontLoyaltyTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontLoyaltyTransaction>>(transactions);
    }
}
