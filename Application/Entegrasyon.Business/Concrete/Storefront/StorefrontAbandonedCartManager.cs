using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontAbandonedCartManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IStorefrontEmailService emailService,
    IStorefrontSettingsManager settingsManager) : IStorefrontAbandonedCartManager
{
    public async Task<IResult> ProcessAbandonedCartsAsync(int tenantId)
    {
        var settingsResult = await settingsManager.GetByTenantIdAsync(tenantId);
        if (!settingsResult.Success || !settingsResult.Data.AbandonedCartRecoveryEnabled)
            return new SuccessResult("Terk edilen sepet kurtarma devre disi.");

        var settings = settingsResult.Data;
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;

        // Find carts with a customer that haven't been modified for the threshold period
        var abandonedCarts = await dbContext.Carts
            .Where(c => c.TenantId == tenantId
                && c.CustomerId != null
                && c.Items.Any()
                && !c.IsDeleted)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
            .ToListAsync();

        var processed = 0;

        foreach (var cart in abandonedCarts)
        {
            var hoursSinceUpdate = (now - cart.UpdatedAt).TotalHours;
            if (hoursSinceUpdate < settings.AbandonedCartEmail1Hours)
                continue;

            // Determine which step to send
            var existingEmails = await dbContext.StorefrontAbandonedCartEmails
                .Where(e => e.CartId == cart.Id && e.TenantId == tenantId && !e.Converted)
                .ToListAsync();

            var maxStep = existingEmails.Count > 0 ? existingEmails.Max(e => e.EmailStep) : 0;
            var nextStep = maxStep + 1;

            if (nextStep > 3) continue;
            if (nextStep == 2 && hoursSinceUpdate < settings.AbandonedCartEmail2Hours) continue;
            if (nextStep == 3 && !settings.AbandonedCartEmail3Enabled) continue;

            // Get customer info
            var auth = await dbContext.StorefrontCustomerAuths
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.CustomerId == cart.CustomerId);

            if (auth is null) continue;

            // Generate coupon for step 3
            string? couponCode = null;
            if (nextStep == 3 && settings.AbandonedCartEmail3DiscountPercent.HasValue)
            {
                couponCode = $"SEPET{cart.Id.ToString()[..8].ToUpper()}";
            }

            var productNames = cart.Items
                .Take(3)
                .Select(i => i.ProductVariant?.Product?.Title ?? "Urun")
                .ToList();

            var email = new StorefrontAbandonedCartEmail
            {
                TenantId = tenantId,
                CartId = cart.Id,
                CustomerId = cart.CustomerId!.Value,
                EmailStep = nextStep,
                Status = AbandonedCartEmailStatus.Pending,
                CouponCode = couponCode
            };

            dbContext.StorefrontAbandonedCartEmails.Add(email);
            await dbContext.SaveChangesAsync();

            // Send email (fire-and-forget style but we await for correctness)
            var domainResult = await dbContext.StorefrontDomainMappings
                .FirstOrDefaultAsync(d => d.TenantId == tenantId);
            var domain = domainResult?.DomainName ?? "localhost";

            await emailService.SendAbandonedCartReminderAsync(
                auth.Email,
                auth.Customer.Name ?? "Musterimiz",
                settings.StoreName,
                domain,
                nextStep,
                productNames,
                couponCode);

            email.Status = AbandonedCartEmailStatus.Sent;
            email.SentAt = DateTimeOffset.UtcNow;
            dbContext.StorefrontAbandonedCartEmails.Update(email);
            await dbContext.SaveChangesAsync();

            processed++;
        }

        return new SuccessResult($"{processed} terk edilen sepet emaili gonderildi.");
    }

    public async Task<IResult> MarkAsConvertedAsync(Guid cartId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var emails = await dbContext.StorefrontAbandonedCartEmails
            .Where(e => e.CartId == cartId && !e.Converted)
            .ToListAsync();

        foreach (var email in emails)
        {
            email.Converted = true;
            email.Status = AbandonedCartEmailStatus.Converted;
            dbContext.StorefrontAbandonedCartEmails.Update(email);
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult();
    }

    public async Task<IDataResult<List<StorefrontAbandonedCartEmail>>> GetAbandonedCartEmailsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var emails = await dbContext.StorefrontAbandonedCartEmails
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(200)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontAbandonedCartEmail>>(emails);
    }
}
