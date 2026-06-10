using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontAddressManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontAddressManager
{
    public async Task<IDataResult<List<StorefrontAddress>>> GetCustomerAddressesAsync(int tenantId, int customerId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var addresses = await dbContext.StorefrontAddresses
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontAddress>>(addresses);
    }

    public async Task<IDataResult<StorefrontAddress>> GetByIdAsync(int tenantId, int customerId, int addressId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var address = await dbContext.StorefrontAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == addressId && a.TenantId == tenantId && a.CustomerId == customerId);

        if (address is null)
            return new ErrorDataResult<StorefrontAddress>(null!, "Adres bulunamadı.");

        return new SuccessDataResult<StorefrontAddress>(address);
    }

    public async Task<IResult> AddAsync(int tenantId, int customerId, StorefrontAddressDto dto)
    {
        // 1. Validation
        var validation = Validate(dto);
        if (validation is not null) return validation;

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 2. Business Rules: ilk adres her zaman varsayilan; istenirse digerleri sifirlanir
        var hasAny = await dbContext.StorefrontAddresses
            .AnyAsync(a => a.TenantId == tenantId && a.CustomerId == customerId);

        var makeDefault = dto.IsDefault || !hasAny;
        if (makeDefault && hasAny)
            await ClearDefaultsAsync(dbContext, tenantId, customerId);

        // 3. Execution
        dbContext.StorefrontAddresses.Add(new StorefrontAddress
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Label = dto.Label,
            FullName = dto.FullName,
            Phone = dto.Phone,
            City = dto.City,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            PostalCode = dto.PostalCode,
            AddressLine = dto.AddressLine,
            IsDefault = makeDefault
        });

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Adres eklendi.");
    }

    public async Task<IResult> UpdateAsync(int tenantId, int customerId, int addressId, StorefrontAddressDto dto)
    {
        // 1. Validation
        var validation = Validate(dto);
        if (validation is not null) return validation;

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 2. Business Rules: adres bu musteriye ait mi
        var address = await dbContext.StorefrontAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.TenantId == tenantId && a.CustomerId == customerId);

        if (address is null)
            return new ErrorResult("Adres bulunamadı.");

        if (dto.IsDefault && !address.IsDefault)
            await ClearDefaultsAsync(dbContext, tenantId, customerId);

        // 3. Execution
        address.Label = dto.Label;
        address.FullName = dto.FullName;
        address.Phone = dto.Phone;
        address.City = dto.City;
        address.District = dto.District;
        address.Neighborhood = dto.Neighborhood;
        address.PostalCode = dto.PostalCode;
        address.AddressLine = dto.AddressLine;
        if (dto.IsDefault) address.IsDefault = true;

        dbContext.StorefrontAddresses.Update(address);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Adres guncellendi.");
    }

    public async Task<IResult> DeleteAsync(int tenantId, int customerId, int addressId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var address = await dbContext.StorefrontAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.TenantId == tenantId && a.CustomerId == customerId);

        if (address is null)
            return new ErrorResult("Adres bulunamadı.");

        var wasDefault = address.IsDefault;
        dbContext.StorefrontAddresses.Remove(address);
        await dbContext.SaveChangesAsync();

        // Varsayilan silindiyse kalan en yeni adresi varsayilan yap
        if (wasDefault)
        {
            var next = await dbContext.StorefrontAddresses
                .Where(a => a.TenantId == tenantId && a.CustomerId == customerId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            if (next is not null)
            {
                next.IsDefault = true;
                dbContext.StorefrontAddresses.Update(next);
                await dbContext.SaveChangesAsync();
            }
        }

        return new SuccessResult("Adres silindi.");
    }

    public async Task<IResult> SetDefaultAsync(int tenantId, int customerId, int addressId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var address = await dbContext.StorefrontAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.TenantId == tenantId && a.CustomerId == customerId);

        if (address is null)
            return new ErrorResult("Adres bulunamadı.");

        await ClearDefaultsAsync(dbContext, tenantId, customerId);
        address.IsDefault = true;
        dbContext.StorefrontAddresses.Update(address);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Varsayilan adres guncellendi.");
    }

    private static IResult? Validate(StorefrontAddressDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Label))
            return new ErrorResult("Adres etiketi zorunludur.");
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return new ErrorResult("Ad soyad zorunludur.");
        if (string.IsNullOrWhiteSpace(dto.AddressLine))
            return new ErrorResult("Acik adres zorunludur.");
        return null;
    }

    private static async Task ClearDefaultsAsync(IntegrationDbContext dbContext, int tenantId, int customerId)
    {
        var defaults = await dbContext.StorefrontAddresses
            .Where(a => a.TenantId == tenantId && a.CustomerId == customerId && a.IsDefault)
            .ToListAsync();
        foreach (var d in defaults)
            d.IsDefault = false;
    }
}
