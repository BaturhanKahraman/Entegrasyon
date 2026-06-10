using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class PaymentMethodManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IPaymentMethodManager
{
    public async Task<IDataResult<List<PaymentMethodDefinition>>> GetActivePaymentMethodsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var methods = await dbContext.PaymentMethodDefinitions
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return new SuccessDataResult<List<PaymentMethodDefinition>>(methods);
    }

    public async Task<IDataResult<List<PaymentMethodDefinition>>> GetAllPaymentMethodsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var methods = await dbContext.PaymentMethodDefinitions
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return new SuccessDataResult<List<PaymentMethodDefinition>>(methods);
    }

    public async Task<IResult> TogglePaymentMethodAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var method = await dbContext.PaymentMethodDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (method is null)
            return new ErrorResult("Ödeme yöntemi bulunamadı.");

        method.IsActive = !method.IsActive;
        dbContext.PaymentMethodDefinitions.Update(method);
        await dbContext.SaveChangesAsync();

        return new SuccessResult(method.IsActive ? "Ödeme yöntemi aktifleştirildi." : "Ödeme yöntemi pasifleştirildi.");
    }

    public async Task<IResult> UpdatePaymentMethodAsync(int id, string name, string icon, decimal? commissionRate)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var method = await dbContext.PaymentMethodDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (method is null)
            return new ErrorResult("Ödeme yöntemi bulunamadı.");

        method.Name = name;
        method.Icon = icon;
        method.CommissionRate = commissionRate;
        dbContext.PaymentMethodDefinitions.Update(method);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Ödeme yöntemi güncellendi.");
    }

    public async Task<IResult> ReorderPaymentMethodsAsync(List<int> orderedIds)
    {
        // Gonderim ayni id'yi birden cok kez icerebilir (frontend DOM'unda data-id
        // birden fazla yerde bulundugunda) — yinelenenleri ilk-gorulen sirasi
        // korunarak ele; aksi halde SortOrder seyrekleşir/bozulur.
        var distinctIds = orderedIds.Distinct().ToList();

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var methods = await dbContext.PaymentMethodDefinitions
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync();

        for (int i = 0; i < distinctIds.Count; i++)
        {
            var method = methods.FirstOrDefault(x => x.Id == distinctIds[i]);
            if (method is null)
                continue;

            method.SortOrder = i + 1;
            // Global no-tracking varsayilani altinda entity izlenmedigi icin
            // mutasyonun kalici olmasi adina acikca modified olarak isaretlenir.
            dbContext.PaymentMethodDefinitions.Update(method);
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Sıralama güncellendi.");
    }

    public async Task<IResult> CreatePaymentMethodAsync(string name, string systemCode, string icon, bool requiresAuthCode, bool requiresCashInput, int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var exists = await dbContext.PaymentMethodDefinitions
            .AnyAsync(x => x.TenantId == tenantId && x.SystemCode == systemCode);
        if (exists)
            return new ErrorResult("Bu sistem kodu zaten mevcut.");

        var maxSort = await dbContext.PaymentMethodDefinitions
            .Where(x => x.TenantId == tenantId)
            .MaxAsync(x => (int?)x.SortOrder) ?? 0;

        var method = new PaymentMethodDefinition
        {
            Name = name,
            SystemCode = systemCode,
            Icon = icon,
            IsActive = true,
            SortOrder = maxSort + 1,
            RequiresAuthCode = requiresAuthCode,
            RequiresCashInput = requiresCashInput,
            TenantId = tenantId
        };

        dbContext.PaymentMethodDefinitions.Add(method);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Ödeme yöntemi oluşturuldu.");
    }
}
