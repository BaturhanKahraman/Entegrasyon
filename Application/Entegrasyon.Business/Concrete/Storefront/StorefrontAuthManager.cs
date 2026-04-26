using System.Security.Cryptography;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Utilities;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontAuthManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IOptions<NotificationFeatureFlags> notificationFlags) : IStorefrontAuthManager
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

        if (notificationFlags.Value.PublishEnabled)
            dbContext.AddDomainEvent(new StorefrontNewCustomerEvent(customer.Id, dto.Email));

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

            return new ErrorDataResult<StorefrontCustomerAuth>(auth, "E-posta veya sifre hatali.");
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

    public async Task RecordLoginAttemptAsync(int authId, string? ipAddress, string? userAgent, bool isSuccessful, string? failureReason = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var deviceType = DetectDeviceType(userAgent);

        dbContext.StorefrontLoginHistories.Add(new StorefrontLoginHistory
        {
            AuthId = authId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceType = deviceType,
            LoginAt = DateTimeOffset.UtcNow,
            IsSuccessful = isSuccessful,
            FailureReason = failureReason
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task<IDataResult<List<StorefrontLoginHistory>>> GetLoginHistoryAsync(int authId, int count = 20)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var history = await dbContext.StorefrontLoginHistories
            .Where(h => h.AuthId == authId)
            .OrderByDescending(h => h.LoginAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontLoginHistory>>(history);
    }

    public async Task<IDataResult<StorefrontCustomerAuth>> ExternalLoginAsync(
        int tenantId, string provider, string externalId, string email, string name, string surname)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Check if auth already exists for this tenant + email
        var existing = await dbContext.StorefrontCustomerAuths
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Email == email);

        if (existing is not null)
        {
            // Link external provider if not already linked
            existing.ExternalLoginProvider = provider;
            existing.ExternalLoginId = externalId;
            existing.EmailConfirmed = true; // Provider-verified email
            existing.LastLoginAt = DateTimeOffset.UtcNow;

            dbContext.StorefrontCustomerAuths.Update(existing);
            await dbContext.SaveChangesAsync();

            return new SuccessDataResult<StorefrontCustomerAuth>(existing, "Giris basarili.");
        }

        // Create new customer + auth for external login (no password)
        var customer = new RetailCustomer
        {
            Name = name,
            Surname = surname,
            FullName = $"{name} {surname}",
            CustomerType = "Retail",
            Address = new Address()
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var auth = new StorefrontCustomerAuth
        {
            TenantId = tenantId,
            CustomerId = customer.Id,
            Email = email,
            PasswordHash = Array.Empty<byte>(),
            PasswordSalt = Array.Empty<byte>(),
            EmailConfirmed = true, // Provider verified
            ExternalLoginProvider = provider,
            ExternalLoginId = externalId,
            LastLoginAt = DateTimeOffset.UtcNow,
            KvkkConsentDate = DateTimeOffset.UtcNow
        };

        dbContext.StorefrontCustomerAuths.Add(auth);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<StorefrontCustomerAuth>(auth, "Kayit ve giris basarili.");
    }

    public async Task<IDataResult<string>> ExportCustomerDataAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Profile
        var auth = await dbContext.StorefrontCustomerAuths
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.CustomerId == customerId);

        if (auth is null)
            return new ErrorDataResult<string>(null!, "Musteri bulunamadi.");

        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        // Orders
        var orders = await dbContext.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == customerId)
            .Select(o => new
            {
                o.OrderNumber,
                OrderDate = o.OrderDate ?? o.CreatedAt,
                o.GrossAmount,
                o.SubTotal,
                o.ShippingCost,
                Status = o.StorefrontOrderStatus.ToString(),
                Items = o.OrderItems.Select(i => new
                {
                    i.Barcode,
                    i.Quantity,
                    i.UnitPrice,
                    i.ProductColor,
                    i.ProductSize
                }).ToList()
            })
            .ToListAsync();

        // Reviews
        var reviews = await dbContext.StorefrontReviews
            .Where(r => r.CustomerId == customerId)
            .AsNoTracking()
            .Select(r => new { r.Rating, r.Comment, r.CreatedAt })
            .ToListAsync();

        // Wishlist
        var wishlist = await dbContext.StorefrontWishlistItems
            .Where(w => w.TenantId == tenantId && w.CustomerId == customerId)
            .Include(w => w.Product)
            .AsNoTracking()
            .Select(w => new { ProductTitle = w.Product.Title, w.AddedAt })
            .ToListAsync();

        // Addresses
        var addresses = customer?.Address is not null
            ? new { customer.Address.FullAddress, customer.Address.County, customer.Address.City }
            : null;

        // Login history
        var loginHistory = await dbContext.StorefrontLoginHistories
            .Where(h => h.AuthId == auth.Id)
            .OrderByDescending(h => h.LoginAt)
            .Take(50)
            .AsNoTracking()
            .Select(h => new { h.LoginAt, h.IpAddress, h.DeviceType, h.IsSuccessful })
            .ToListAsync();

        var exportData = new
        {
            ExportDate = DateTimeOffset.UtcNow,
            Profile = new
            {
                customer?.Name,
                customer?.Surname,
                auth.Email,
                customer?.PhoneNumber,
                auth.KvkkConsentDate,
                auth.MarketingConsent,
                auth.MarketingConsentDate,
                auth.EmailConfirmed,
                RegisterDate = auth.CreatedAt
            },
            Address = addresses,
            Orders = orders,
            Reviews = reviews,
            Wishlist = wishlist,
            LoginHistory = loginHistory
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        return new SuccessDataResult<string>(json);
    }

    private static string DetectDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Bilinmiyor";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
            return "Mobil";
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";
        return "Masaustu";
    }

    private static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    // --- Two-Factor Authentication ---

    public async Task<IDataResult<string>> Enable2FAAsync(int authId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths.FindAsync(authId);
        if (auth is null)
            return new ErrorDataResult<string>(null!, "Hesap bulunamadi.");

        // Generate TOTP secret
        var secret = OtpNet.Base32Encoding.ToString(RandomNumberGenerator.GetBytes(20));
        auth.TwoFactorSecret = secret;
        dbContext.StorefrontCustomerAuths.Update(auth);
        await dbContext.SaveChangesAsync();

        // Return otpauth URI for QR code generation
        var uri = $"otpauth://totp/Storefront:{Uri.EscapeDataString(auth.Email)}?secret={secret}&issuer=Storefront&digits=6&period=30";
        return new SuccessDataResult<string>(uri);
    }

    public async Task<IResult> Verify2FAAsync(int authId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorResult("Dogrulama kodu bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths.FindAsync(authId);
        if (auth is null)
            return new ErrorResult("Hesap bulunamadi.");

        if (string.IsNullOrEmpty(auth.TwoFactorSecret))
            return new ErrorResult("2FA yapilandirilmamis.");

        var secretBytes = OtpNet.Base32Encoding.ToBytes(auth.TwoFactorSecret);
        var totp = new OtpNet.Totp(secretBytes, step: 30, totpSize: 6);
        var isValid = totp.VerifyTotp(code.Trim(), out _, new OtpNet.VerificationWindow(previous: 1, future: 1));

        if (!isValid)
            return new ErrorResult("Gecersiz dogrulama kodu.");

        if (!auth.TwoFactorEnabled)
        {
            auth.TwoFactorEnabled = true;
            dbContext.StorefrontCustomerAuths.Update(auth);
            await dbContext.SaveChangesAsync();
        }

        return new SuccessResult("Dogrulama basarili.");
    }

    public async Task<IResult> Disable2FAAsync(int authId, string code)
    {
        var verifyResult = await Verify2FAAsync(authId, code);
        if (!verifyResult.Success)
            return verifyResult;

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var auth = await dbContext.StorefrontCustomerAuths.FindAsync(authId);
        if (auth is null)
            return new ErrorResult("Hesap bulunamadi.");

        auth.TwoFactorEnabled = false;
        auth.TwoFactorSecret = null;
        dbContext.StorefrontCustomerAuths.Update(auth);

        // Remove recovery codes
        var codes = await dbContext.StorefrontTwoFactorRecoveryCodes
            .Where(c => c.AuthId == authId)
            .ToListAsync();
        dbContext.StorefrontTwoFactorRecoveryCodes.RemoveRange(codes);

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Iki faktorlu dogrulama devre disi birakildi.");
    }

    public async Task<IDataResult<List<string>>> GenerateRecoveryCodesAsync(int authId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Remove existing codes
        var existingCodes = await dbContext.StorefrontTwoFactorRecoveryCodes
            .Where(c => c.AuthId == authId)
            .ToListAsync();
        dbContext.StorefrontTwoFactorRecoveryCodes.RemoveRange(existingCodes);

        var plainCodes = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var code = $"{RandomNumberGenerator.GetInt32(100000, 999999)}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
            plainCodes.Add(code);

            var hashedCode = Convert.ToBase64String(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));

            dbContext.StorefrontTwoFactorRecoveryCodes.Add(new StorefrontTwoFactorRecoveryCode
            {
                AuthId = authId,
                Code = hashedCode,
                IsUsed = false
            });
        }

        await dbContext.SaveChangesAsync();
        return new SuccessDataResult<List<string>>(plainCodes);
    }

    public async Task<IResult> VerifyRecoveryCodeAsync(int authId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorResult("Kurtarma kodu bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var hashedInput = Convert.ToBase64String(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code.Trim())));

        var recoveryCode = await dbContext.StorefrontTwoFactorRecoveryCodes
            .FirstOrDefaultAsync(c => c.AuthId == authId && c.Code == hashedInput && !c.IsUsed);

        if (recoveryCode is null)
            return new ErrorResult("Gecersiz veya kullanilmis kurtarma kodu.");

        recoveryCode.IsUsed = true;
        recoveryCode.UsedAt = DateTimeOffset.UtcNow;
        dbContext.StorefrontTwoFactorRecoveryCodes.Update(recoveryCode);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Kurtarma kodu ile giris basarili.");
    }

    public async Task<IDataResult<bool>> IsTwoFactorEnabledAsync(int authId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var auth = await dbContext.StorefrontCustomerAuths.FindAsync(authId);
        return new SuccessDataResult<bool>(auth?.TwoFactorEnabled ?? false);
    }
}
