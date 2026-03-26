using System.Security.Cryptography;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontAuthManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontAuthManager
{
    public async Task<IDataResult<StorefrontCustomerAuth>> RegisterAsync(StorefrontRegisterDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Validation
        if (dto.Password != dto.ConfirmPassword)
            return new ErrorDataResult<StorefrontCustomerAuth>(null!, "Sifreler eslesmiyor.");

        if (!dto.KvkkConsent)
            return new ErrorDataResult<StorefrontCustomerAuth>(null!, "KVKK onayi zorunludur.");

        // Business Rule: email unique per tenant
        var emailExists = await dbContext.StorefrontCustomerAuths
            .AnyAsync(x => x.TenantId == dto.TenantId && x.Email == dto.Email);

        if (emailExists)
            return new ErrorDataResult<StorefrontCustomerAuth>(null!, "Bu e-posta adresi zaten kayitli.");

        // Create RetailCustomer
        var customer = new RetailCustomer
        {
            Name = dto.Name,
            Surname = dto.Surname,
            FullName = $"{dto.Name} {dto.Surname}",
            PhoneNumber = dto.Phone,
            CustomerType = "Retail",
            Address = new Address()
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        // Create auth
        HashingHelper.CreatePasswordHash(dto.Password, out var hash, out var salt);

        var auth = new StorefrontCustomerAuth
        {
            TenantId = dto.TenantId,
            CustomerId = customer.Id,
            Email = dto.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            EmailConfirmed = false,
            EmailConfirmationToken = GenerateToken(),
            EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            MarketingConsent = dto.MarketingConsent,
            MarketingConsentDate = dto.MarketingConsent ? DateTimeOffset.UtcNow : null,
            KvkkConsentDate = DateTimeOffset.UtcNow
        };

        dbContext.StorefrontCustomerAuths.Add(auth);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<StorefrontCustomerAuth>(auth, "Kayit basarili.");
    }

    public async Task<IDataResult<StorefrontCustomerAuth>> LoginAsync(int tenantId, string email, string password)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Email == email);

        if (auth is null)
            return new ErrorDataResult<StorefrontCustomerAuth>(null!, "E-posta veya sifre hatali.");

        // Check lock
        if (auth.LockedUntil.HasValue && auth.LockedUntil.Value > DateTimeOffset.UtcNow)
            return new ErrorDataResult<StorefrontCustomerAuth>(null!,
                "Hesabiniz cok fazla basarisiz giris denemesi nedeniyle kilitlendi. Lutfen daha sonra tekrar deneyin.");

        // Verify password
        if (!HashingHelper.VerifyPasswordHash(password, auth.PasswordHash, auth.PasswordSalt))
        {
            auth.LoginFailedCount++;
            if (auth.LoginFailedCount >= 5)
                auth.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);

            dbContext.StorefrontCustomerAuths.Update(auth);
            await dbContext.SaveChangesAsync();

            return new ErrorDataResult<StorefrontCustomerAuth>(null!, "E-posta veya sifre hatali.");
        }

        // Success: reset counters
        auth.LoginFailedCount = 0;
        auth.LockedUntil = null;
        auth.LastLoginAt = DateTimeOffset.UtcNow;

        dbContext.StorefrontCustomerAuths.Update(auth);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<StorefrontCustomerAuth>(auth, "Giris basarili.");
    }

    public async Task<IResult> ConfirmEmailAsync(int tenantId, string token)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths
            .FirstOrDefaultAsync(x => x.TenantId == tenantId
                                      && x.EmailConfirmationToken == token);

        if (auth is null)
            return new ErrorResult("Gecersiz dogrulama tokeni.");

        if (auth.EmailConfirmationTokenExpiresAt.HasValue &&
            auth.EmailConfirmationTokenExpiresAt.Value < DateTimeOffset.UtcNow)
            return new ErrorResult("Dogrulama tokeninin suresi dolmus.");

        auth.EmailConfirmed = true;
        auth.EmailConfirmationToken = null;
        auth.EmailConfirmationTokenExpiresAt = null;

        dbContext.StorefrontCustomerAuths.Update(auth);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("E-posta dogrulandi.");
    }

    public async Task<IDataResult<string>> RequestPasswordResetAsync(int tenantId, string email)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Email == email);

        if (auth is null)
            return new ErrorDataResult<string>(null!, "Bu e-posta adresiyle kayitli kullanici bulunamadi.");

        var token = GenerateToken();
        auth.PasswordResetToken = token;
        auth.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

        dbContext.StorefrontCustomerAuths.Update(auth);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<string>(token, "Sifre sifirlama tokeni olusturuldu.");
    }

    public async Task<IResult> ResetPasswordAsync(int tenantId, string token, string newPassword)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths
            .FirstOrDefaultAsync(x => x.TenantId == tenantId
                                      && x.PasswordResetToken == token);

        if (auth is null)
            return new ErrorResult("Gecersiz sifre sifirlama tokeni.");

        if (auth.PasswordResetTokenExpiresAt.HasValue &&
            auth.PasswordResetTokenExpiresAt.Value < DateTimeOffset.UtcNow)
            return new ErrorResult("Sifre sifirlama tokeninin suresi dolmus.");

        HashingHelper.CreatePasswordHash(newPassword, out var hash, out var salt);
        auth.PasswordHash = hash;
        auth.PasswordSalt = salt;
        auth.PasswordResetToken = null;
        auth.PasswordResetTokenExpiresAt = null;
        auth.LoginFailedCount = 0;
        auth.LockedUntil = null;

        dbContext.StorefrontCustomerAuths.Update(auth);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Sifre basariyla sifirlandi.");
    }

    public async Task<IResult> ChangePasswordAsync(int authId, string currentPassword, string newPassword)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths
            .FirstOrDefaultAsync(x => x.Id == authId);

        if (auth is null)
            return new ErrorResult("Kullanici bulunamadi.");

        if (!HashingHelper.VerifyPasswordHash(currentPassword, auth.PasswordHash, auth.PasswordSalt))
            return new ErrorResult("Mevcut sifre hatali.");

        HashingHelper.CreatePasswordHash(newPassword, out var hash, out var salt);
        auth.PasswordHash = hash;
        auth.PasswordSalt = salt;

        dbContext.StorefrontCustomerAuths.Update(auth);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Sifre basariyla degistirildi.");
    }

    public async Task<IDataResult<StorefrontCustomerAuth>> GetAuthByCustomerIdAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CustomerId == customerId);

        if (auth is null)
            return new ErrorDataResult<StorefrontCustomerAuth>(null!, "Musteri auth bilgisi bulunamadi.");

        return new SuccessDataResult<StorefrontCustomerAuth>(auth);
    }

    public async Task<IResult> UpdateProfileAsync(int customerId, StorefrontProfileDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
            return new ErrorResult("Musteri bulunamadi.");

        customer.Name = dto.Name;
        customer.Surname = dto.Surname;
        customer.FullName = $"{dto.Name} {dto.Surname}";
        customer.PhoneNumber = dto.Phone;

        dbContext.Customers.Update(customer);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Profil guncellendi.");
    }

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
