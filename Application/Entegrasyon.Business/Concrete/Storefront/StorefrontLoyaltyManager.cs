using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Storefront;
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

    public async Task<LoyaltyDashboardDto> GetDashboardAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var allPoints = await dbContext.StorefrontLoyaltyPoints
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync();

        var activeMembers = allPoints.Count;
        var totalEarned = allPoints.Sum(l => l.TotalEarned);
        var totalSpent = allPoints.Sum(l => l.TotalSpent);

        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var thisMonthEarned = await dbContext.StorefrontLoyaltyTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.Points > 0 && t.CreatedAt >= monthStart)
            .SumAsync(t => t.Points);

        var topMembers = allPoints
            .OrderByDescending(l => l.TotalEarned)
            .Take(10)
            .Select(l => new LoyaltyMemberDto(
                l.CustomerId,
                null,
                l.TotalEarned,
                l.CurrentBalance,
                GetLevel(l.TotalEarned)))
            .ToList();

        // Müşteri adlarını toplu çek
        var customerIds = topMembers.Select(m => m.CustomerId).ToList();
        var customerNames = await dbContext.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName ?? $"Musteri #{c.Id}");

        topMembers = topMembers.Select(m => m with
        {
            CustomerName = customerNames.GetValueOrDefault(m.CustomerId, $"Musteri #{m.CustomerId}")
        }).ToList();

        return new LoyaltyDashboardDto(activeMembers, totalEarned, totalSpent, thisMonthEarned, topMembers);
    }

    private static string GetLevel(int totalEarned) => totalEarned switch
    {
        >= 5000 => "Platin",
        >= 1500 => "Altin",
        >= 500 => "Gumus",
        _ => "Bronz"
    };
}
