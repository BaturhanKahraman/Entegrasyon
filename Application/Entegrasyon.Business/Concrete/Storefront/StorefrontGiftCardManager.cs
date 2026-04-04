using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontGiftCardManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontGiftCardManager
{
    private static readonly char[] AlphanumericChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public async Task<IDataResult<StorefrontGiftCard>> CreateGiftCardAsync(
        int tenantId, decimal amount, int? purchasedByCustomerId,
        string? recipientEmail, string? recipientName, string? message)
    {
        if (amount <= 0)
            return new ErrorDataResult<StorefrontGiftCard>(null!, "Hediye karti tutari sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var code = await GenerateUniqueCodeAsync(dbContext, tenantId);

        var giftCard = new StorefrontGiftCard
        {
            TenantId = tenantId,
            Code = code,
            InitialAmount = amount,
            RemainingAmount = amount,
            PurchasedByCustomerId = purchasedByCustomerId,
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            SenderMessage = message,
            ExpiresAt = DateTimeOffset.UtcNow.AddYears(1),
            Status = GiftCardStatus.Active
        };

        dbContext.StorefrontGiftCards.Add(giftCard);

        var transaction = new StorefrontGiftCardTransaction
        {
            GiftCardId = giftCard.Id,
            Amount = amount,
            TransactionType = "Purchase",
            BalanceBefore = 0,
            BalanceAfter = amount
        };

        dbContext.StorefrontGiftCardTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        // Fix up FK after save (EF Core will set Id)
        transaction.GiftCardId = giftCard.Id;
        dbContext.StorefrontGiftCardTransactions.Update(transaction);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<StorefrontGiftCard>(giftCard, "Hediye karti basariyla olusturuldu.");
    }

    public async Task<IDataResult<StorefrontGiftCard>> GetByCodeAsync(int tenantId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorDataResult<StorefrontGiftCard>(null!, "Hediye karti kodu bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var giftCard = await dbContext.StorefrontGiftCards
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.TenantId == tenantId
                && g.Code == code.Trim().ToUpperInvariant()
                && g.Status == GiftCardStatus.Active
                && g.ExpiresAt > DateTimeOffset.UtcNow);

        if (giftCard is null)
            return new ErrorDataResult<StorefrontGiftCard>(null!, "Gecerli hediye karti bulunamadi.");

        return new SuccessDataResult<StorefrontGiftCard>(giftCard);
    }

    public async Task<IDataResult<decimal>> UseGiftCardAsync(int tenantId, string code, decimal amount, Guid orderId)
    {
        if (amount <= 0)
            return new ErrorDataResult<decimal>(0, "Kullanim tutari sifirdan buyuk olmalidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var giftCard = await dbContext.StorefrontGiftCards
            .FirstOrDefaultAsync(g => g.TenantId == tenantId
                && g.Code == code.Trim().ToUpperInvariant()
                && g.Status == GiftCardStatus.Active
                && g.ExpiresAt > DateTimeOffset.UtcNow);

        if (giftCard is null)
            return new ErrorDataResult<decimal>(0, "Gecerli hediye karti bulunamadi.");

        if (giftCard.RemainingAmount < amount)
            return new ErrorDataResult<decimal>(giftCard.RemainingAmount, "Hediye karti bakiyesi yetersiz.");

        var balanceBefore = giftCard.RemainingAmount;
        giftCard.RemainingAmount -= amount;

        if (giftCard.RemainingAmount == 0)
            giftCard.Status = GiftCardStatus.Used;

        dbContext.StorefrontGiftCards.Update(giftCard);

        var transaction = new StorefrontGiftCardTransaction
        {
            GiftCardId = giftCard.Id,
            OrderId = orderId,
            Amount = -amount,
            TransactionType = "Use",
            BalanceBefore = balanceBefore,
            BalanceAfter = giftCard.RemainingAmount
        };

        dbContext.StorefrontGiftCardTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<decimal>(giftCard.RemainingAmount, "Hediye karti basariyla kullanildi.");
    }

    public async Task<IDataResult<decimal>> CheckBalanceAsync(int tenantId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorDataResult<decimal>(0, "Hediye karti kodu bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var giftCard = await dbContext.StorefrontGiftCards
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.TenantId == tenantId
                && g.Code == code.Trim().ToUpperInvariant());

        if (giftCard is null)
            return new ErrorDataResult<decimal>(0, "Hediye karti bulunamadi.");

        if (giftCard.Status == GiftCardStatus.Expired || giftCard.ExpiresAt <= DateTimeOffset.UtcNow)
            return new ErrorDataResult<decimal>(0, "Hediye kartinin suresi dolmus.");

        if (giftCard.Status == GiftCardStatus.Cancelled)
            return new ErrorDataResult<decimal>(0, "Hediye karti iptal edilmis.");

        if (giftCard.Status == GiftCardStatus.Used)
            return new SuccessDataResult<decimal>(0, "Hediye kartinin bakiyesi tukenmis.");

        return new SuccessDataResult<decimal>(giftCard.RemainingAmount);
    }

    public async Task<IDataResult<Pageable<StorefrontGiftCard>>> GetGiftCardsAsync(int tenantId, int pageIndex = 0, int pageSize = 20)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.StorefrontGiftCards
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId && !g.IsDeleted)
            .OrderByDescending(g => g.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new SuccessDataResult<Pageable<StorefrontGiftCard>>(
            new Pageable<StorefrontGiftCard>(items, pageIndex, pageSize, totalCount));
    }

    public async Task<IDataResult<StorefrontGiftCard>> GetByIdAsync(int tenantId, int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var giftCard = await dbContext.StorefrontGiftCards
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId && !g.IsDeleted);

        if (giftCard is null)
            return new ErrorDataResult<StorefrontGiftCard>(null!, "Hediye karti bulunamadi.");

        return new SuccessDataResult<StorefrontGiftCard>(giftCard);
    }

    public async Task<IDataResult<List<StorefrontGiftCardTransaction>>> GetTransactionsAsync(int giftCardId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var transactions = await dbContext.StorefrontGiftCardTransactions
            .AsNoTracking()
            .Where(t => t.GiftCardId == giftCardId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontGiftCardTransaction>>(transactions);
    }

    private static async Task<string> GenerateUniqueCodeAsync(IntegrationDbContext dbContext, int tenantId)
    {
        string code;
        do
        {
            code = GenerateCode(16);
        } while (await dbContext.StorefrontGiftCards.AnyAsync(g => g.TenantId == tenantId && g.Code == code));

        return code;
    }

    private static string GenerateCode(int length)
    {
        var random = Random.Shared;
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = AlphanumericChars[random.Next(AlphanumericChars.Length)];
        return new string(chars);
    }
}
