using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class SellerManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : ISellerManager
{
    public async Task<IDataResult<Seller>> RegisterSellerAsync(int tenantId, int customerId, SellerRegistrationDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Business Rule: customer can only be a seller once per tenant
        var alreadySeller = await dbContext.Sellers
            .AnyAsync(x => x.TenantId == tenantId && x.CustomerId == customerId);

        if (alreadySeller)
            return new ErrorDataResult<Seller>(null!, "Bu musteri zaten bir satici olarak kayitli.");

        var slug = GenerateSlug(dto.StoreName);

        // Ensure slug uniqueness
        var slugExists = await dbContext.Sellers
            .AnyAsync(x => x.TenantId == tenantId && x.StoreSlug == slug);

        if (slugExists)
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var seller = new Seller
        {
            TenantId = tenantId,
            CustomerId = customerId,
            StoreName = dto.StoreName,
            StoreSlug = slug,
            StoreDescription = dto.StoreDescription,
            CompanyName = dto.CompanyName,
            TaxNumber = dto.TaxNumber,
            TaxOffice = dto.TaxOffice,
            Iban = dto.Iban,
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            Address = dto.Address,
            City = dto.City,
            Status = SellerStatus.Pending,
            DefaultCommissionRate = 10
        };

        dbContext.Sellers.Add(seller);
        await dbContext.SaveChangesAsync();

        // Create initial balance
        var balance = new SellerBalance
        {
            SellerId = seller.Id,
            TotalEarned = 0,
            TotalPaidOut = 0,
            PendingAmount = 0,
            CurrentBalance = 0
        };
        dbContext.SellerBalances.Add(balance);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<Seller>(seller, "Satici basvurusu basariyla olusturuldu.");
    }

    public async Task<IDataResult<Seller>> GetSellerByCustomerIdAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var seller = await dbContext.Sellers
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.CustomerId == customerId);

        return seller is not null
            ? new SuccessDataResult<Seller>(seller)
            : new ErrorDataResult<Seller>(null!, "Satici bulunamadı.");
    }

    public async Task<IDataResult<Seller>> GetSellerBySlugAsync(int tenantId, string slug)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var seller = await dbContext.Sellers
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.StoreSlug == slug);

        return seller is not null
            ? new SuccessDataResult<Seller>(seller)
            : new ErrorDataResult<Seller>(null!, "Mağaza bulunamadı.");
    }

    public async Task<IDataResult<List<Seller>>> GetAllSellersAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var sellers = await dbContext.Sellers
            .Include(x => x.Customer)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<Seller>>(sellers);
    }

    public async Task<IResult> ApproveSellerAsync(int sellerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var seller = await dbContext.Sellers.FindAsync(sellerId);
        if (seller is null)
            return new ErrorResult("Satici bulunamadı.");

        if (seller.Status == SellerStatus.Approved)
            return new ErrorResult("Satici zaten onaylanmis.");

        seller.Status = SellerStatus.Approved;
        seller.ApprovedAt = DateTimeOffset.UtcNow;
        seller.RejectionReason = null;

        dbContext.Sellers.Update(seller);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Satici basariyla onaylandi.");
    }

    public async Task<IResult> RejectSellerAsync(int sellerId, string reason)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var seller = await dbContext.Sellers.FindAsync(sellerId);
        if (seller is null)
            return new ErrorResult("Satici bulunamadı.");

        seller.Status = SellerStatus.Rejected;
        seller.RejectionReason = reason;

        dbContext.Sellers.Update(seller);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Satici basvurusu reddedildi.");
    }

    public async Task<IResult> SuspendSellerAsync(int sellerId, string reason)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var seller = await dbContext.Sellers.FindAsync(sellerId);
        if (seller is null)
            return new ErrorResult("Satici bulunamadı.");

        if (seller.Status != SellerStatus.Approved)
            return new ErrorResult("Sadece onaylanmis saticilar askiya alinabilir.");

        seller.Status = SellerStatus.Suspended;
        seller.RejectionReason = reason;

        dbContext.Sellers.Update(seller);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Satici askiya alindi.");
    }

    public async Task<IResult> UpdateSellerProfileAsync(int sellerId, SellerProfileDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var seller = await dbContext.Sellers.FindAsync(sellerId);
        if (seller is null)
            return new ErrorResult("Satici bulunamadı.");

        seller.StoreName = dto.StoreName;
        seller.StoreDescription = dto.StoreDescription;
        seller.LogoUrl = dto.LogoUrl;
        seller.ContactPhone = dto.ContactPhone;
        seller.ContactEmail = dto.ContactEmail;
        seller.Address = dto.Address;
        seller.City = dto.City;
        seller.Iban = dto.Iban;

        dbContext.Sellers.Update(seller);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Satici profili guncellendi.");
    }

    private static string GenerateSlug(string text)
    {
        // Turkish character mapping
        var turkishMap = new Dictionary<char, string>
        {
            { '\u00e7', "c" }, { '\u00c7', "c" },
            { '\u011f', "g" }, { '\u011e', "g" },
            { '\u0131', "i" }, { '\u0130', "i" },
            { '\u00f6', "o" }, { '\u00d6', "o" },
            { '\u015f', "s" }, { '\u015e', "s" },
            { '\u00fc', "u" }, { '\u00dc', "u" }
        };

        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (turkishMap.TryGetValue(c, out var replacement))
                sb.Append(replacement);
            else
                sb.Append(c);
        }

        var slug = sb.ToString().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
