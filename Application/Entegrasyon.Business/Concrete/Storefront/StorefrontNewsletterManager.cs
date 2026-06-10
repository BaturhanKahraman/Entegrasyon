using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontNewsletterManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontNewsletterManager
{
    public async Task<IResult> SubscribeAsync(int tenantId, string email, string? name)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new ErrorResult("E-posta alanızorunludur.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.StorefrontNewsletters
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.Email == email);

        if (existing is not null)
        {
            if (existing.IsActive)
                return new ErrorResult("Bu e-posta adresi zaten kayitli.");

            existing.IsActive = true;
            existing.Name = name;
            dbContext.StorefrontNewsletters.Update(existing);
            await dbContext.SaveChangesAsync();
            return new SuccessResult("Bulten aboneligi yeniden aktif edildi.");
        }

        var newsletter = new StorefrontNewsletter
        {
            TenantId = tenantId,
            Email = email,
            Name = name,
            IsActive = true
        };

        dbContext.StorefrontNewsletters.Add(newsletter);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Bulten aboneliginiz basariyla olusturuldu.");
    }

    public async Task<IResult> UnsubscribeAsync(int tenantId, string email)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.StorefrontNewsletters
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.Email == email);

        if (existing is null)
            return new ErrorResult("Abone kaydi bulunamadı.");

        existing.IsActive = false;
        dbContext.StorefrontNewsletters.Update(existing);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Bulten aboneligi iptal edildi.");
    }

    public async Task<IDataResult<List<StorefrontNewsletter>>> GetSubscribersAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var subscribers = await dbContext.StorefrontNewsletters
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontNewsletter>>(subscribers);
    }
}
