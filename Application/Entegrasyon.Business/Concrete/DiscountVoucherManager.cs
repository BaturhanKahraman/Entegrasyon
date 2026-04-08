using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using System.Linq.Expressions;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.DiscountVouchers;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.DiscountVouchers;
using Entegrasyon.Business.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class DiscountVoucherManager : IDiscountVoucherManager
{
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IFluentValidator _fluentValidator;
    private const int CodeLength = 6;

    public DiscountVoucherManager(IApplicationLogManager applicationLogManager, IDbContextFactory<IntegrationDbContext> contextFactory,
        IRandomGenerator randomGenerator, IFluentValidator fluentValidator)
    {
        _applicationLogManager = applicationLogManager;
        _contextFactory = contextFactory;
        _randomGenerator = randomGenerator;
        _fluentValidator = fluentValidator;
    }

    public async Task<bool> CodeExits(string code)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.DiscountVouchers.AnyAsync(x => x.Code == code);
    }

    public async Task<IDataResult<string>> CreateDiscountVoucher(CreateDiscountVoucherDto dto)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        await _applicationLogManager.AddLog("İndirim kodu oluşturulma isteği geldi.", LogType.DiscountVoucher, LogAction.Add, dto);
        await _fluentValidator.ValidateAndThrowAsync(dto);

        var discountVoucher = new DiscountVoucher
        {
            DiscountType = dto.DiscountType,
            Amount = dto.DiscountType == DiscountType.FixedAmount ? dto.Amount : 0,
            Percentage = dto.DiscountType == DiscountType.Percentage ? dto.Percentage : 0,
            ExpiringDate = dto.ExpiringDay,
            CustomerId = dto.CustomerId,
            MaxUsageCount = dto.MaxUsageCount,
            MinimumCartAmount = dto.MinimumCartAmount,
            IsActive = true
        };
        string code = _randomGenerator.GetRandomCode(CodeLength, true, true, false);
        while (await CodeExits(code))
            code = _randomGenerator.GetRandomCode(CodeLength, true, true, false);
        discountVoucher.Code = code;
        dbContext.DiscountVouchers.Add(discountVoucher);
        await dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog($"İndirim kodu başarı ile oluşturuldu. {code}", LogType.DiscountVoucher, LogAction.Add);
        return new SuccessDataResult<string>(discountVoucher.Code);
    }

    public async Task<IResult> MakePassiveDiscountVouchers(IEnumerable<int> voucherIds)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        await _applicationLogManager.AddLog("İndirim kodları pasife çekiliyor.", LogType.DiscountVoucher, LogAction.Update);
        var vouchers = await dbContext.DiscountVouchers.AsTracking().Where(x => voucherIds.Contains(x.Id)).ToListAsync();
        vouchers.ForEach(x => x.IsActive = false);
        await dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("İndirim kodları pasife çekildi.", LogType.DiscountVoucher, LogAction.Update);
        return new SuccessResult("Tüm indirim kuponları pasif hale getirildi.");
    }

    public async Task<IResult> MakeActiveDiscountVouchers(IEnumerable<int> voucherIds)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        await _applicationLogManager.AddLog("İndirim kodları aktife çekiliyor.", LogType.DiscountVoucher, LogAction.Update);
        var vouchers = await dbContext.DiscountVouchers.AsTracking().Where(x => voucherIds.Contains(x.Id)).ToListAsync();
        vouchers.ForEach(x => x.IsActive = true);
        await dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("İndirim kodları aktife çekildi.", LogType.DiscountVoucher, LogAction.Update);
        return new SuccessResult("Tüm indirim kuponları aktif hale getirildi.");
    }

    public async Task<IDataResult<Pageable<DiscountVoucherDto>>> GetDiscountVouchers(int pageIndex, int pagesize, string? searchParam = null)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var query = dbContext.DiscountVouchers.AsQueryable();
        if (!string.IsNullOrEmpty(searchParam))
            query = query.Where(x => x.Code == searchParam || x.Customer!.FullName!.Contains(searchParam));

        int total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pagesize)
            .Take(pagesize)
            .Select(d => new DiscountVoucherDto(
                d.Id, d.Code!, d.DiscountType, d.Percentage, d.Amount,
                d.ExpiringDate, d.IsActive,
                d.MaxUsageCount, d.CurrentUsageCount, d.MinimumCartAmount,
                d.Customer != null ? d.Customer.FullName : null))
            .ToListAsync();

        return new SuccessDataResult<Pageable<DiscountVoucherDto>>(new Pageable<DiscountVoucherDto>(items, pageIndex, pagesize, total));
    }

    public async Task<IResult> CheckVoucherValid(string code)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var discountVoucher = await dbContext.DiscountVouchers.FirstOrDefaultAsync(x => x.Code == code);
        if (discountVoucher == null)
            return new ErrorResult($"{code} kodunda herhangi bir kupon bulunamamıştır.");
        if (!discountVoucher.IsActive)
            return new ErrorResult($"{code} kodundaki kupon pasif durumdadır.");
        if (discountVoucher.ExpiringDate.HasValue && discountVoucher.ExpiringDate.Value < DateTimeOffset.UtcNow)
            return new ErrorResult($"{code} kodundaki kuponun tarihi geçmiştir. Son tarih: {discountVoucher.ExpiringDate.Value:dd-MM-yyyy}");
        if (discountVoucher.MaxUsageCount.HasValue && discountVoucher.CurrentUsageCount >= discountVoucher.MaxUsageCount.Value)
            return new ErrorResult($"{code} kodundaki kuponun kullanım limiti dolmuştur. ({discountVoucher.MaxUsageCount} kullanım)");
        return new SuccessResult();
    }

    public async Task<IResult> DeleteVoucher(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var voucher = await dbContext.DiscountVouchers.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (voucher is null)
            return new ErrorResult("Kupon bulunamadı.");
        if (voucher.CurrentUsageCount > 0)
            return new ErrorResult("Kullanılmış kupon silinemez.");

        voucher.IsDeleted = true;
        voucher.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog($"İndirim kodu silindi: {voucher.Code}", LogType.DiscountVoucher, LogAction.Delete);
        return new SuccessResult("İndirim kodu silindi.");
    }
}
