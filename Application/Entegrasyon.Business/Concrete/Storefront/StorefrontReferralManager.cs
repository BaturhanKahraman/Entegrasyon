using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontReferralManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontReferralManager
{
    private static readonly char[] AlphanumericChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public async Task<IDataResult<string>> GetOrCreateReferralCodeAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.StorefrontReferrals
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId
                && r.ReferrerCustomerId == customerId
                && r.ReferredCustomerId == null);

        if (existing is not null)
            return new SuccessDataResult<string>(existing.ReferralCode);

        var code = await GenerateUniqueCodeAsync(dbContext, tenantId);

        var referral = new StorefrontReferral
        {
            TenantId = tenantId,
            ReferrerCustomerId = customerId,
            ReferralCode = code,
            Status = ReferralStatus.Pending
        };

        dbContext.StorefrontReferrals.Add(referral);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<string>(code);
    }

    public async Task<IResult> RegisterReferralAsync(int tenantId, string referralCode, int referredCustomerId)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return new ErrorResult("Referans kodu bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var referral = await dbContext.StorefrontReferrals
            .FirstOrDefaultAsync(r => r.TenantId == tenantId
                && r.ReferralCode == referralCode.Trim().ToUpperInvariant()
                && r.Status == ReferralStatus.Pending
                && r.ReferredCustomerId == null);

        if (referral is null)
            return new ErrorResult("Gecerli referans kodu bulunamadı.");

        if (referral.ReferrerCustomerId == referredCustomerId)
            return new ErrorResult("Kendi referans kodunuzu kullanamazsiniz.");

        // Check if this customer was already referred
        var alreadyReferred = await dbContext.StorefrontReferrals
            .AnyAsync(r => r.TenantId == tenantId && r.ReferredCustomerId == referredCustomerId);

        if (alreadyReferred)
            return new ErrorResult("Bu musteri zaten bir referans ile kayit olmus.");

        referral.ReferredCustomerId = referredCustomerId;
        referral.Status = ReferralStatus.Registered;
        dbContext.StorefrontReferrals.Update(referral);

        // Create a new pending referral for the referrer (so they can refer more people)
        var newReferral = new StorefrontReferral
        {
            TenantId = tenantId,
            ReferrerCustomerId = referral.ReferrerCustomerId,
            ReferralCode = referral.ReferralCode, // reuse same code
            Status = ReferralStatus.Pending
        };
        dbContext.StorefrontReferrals.Add(newReferral);

        await dbContext.SaveChangesAsync();

        return new SuccessResult("Referans basariyla kaydedildi.");
    }

    public async Task<IDataResult<List<StorefrontReferral>>> GetReferralsAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var referrals = await dbContext.StorefrontReferrals
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                && r.ReferrerCustomerId == customerId
                && r.ReferredCustomerId != null)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontReferral>>(referrals);
    }

    private static async Task<string> GenerateUniqueCodeAsync(IntegrationDbContext dbContext, int tenantId)
    {
        string code;
        do
        {
            code = GenerateCode(8);
        } while (await dbContext.StorefrontReferrals.AnyAsync(r => r.TenantId == tenantId && r.ReferralCode == code));

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
