using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class CommissionCalculator(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    ILogger<CommissionCalculator> logger) : ICommissionCalculator
{
    public async Task<IDataResult<CommissionCalculationResult>> CalculateAsync(
        int marketPlaceId, int? categoryId, decimal salePrice, decimal costPrice,
        CancellationToken ct = default)
    {
        using var dbContext = contextFactory.CreateDbContext();

        // Kategori bazlı oran bul, yoksa default
        var rate = await dbContext.MarketplaceCommissionRates
            .AsNoTracking()
            .Where(r => r.MarketPlaceId == marketPlaceId)
            .Where(r => (categoryId.HasValue && r.CategoryId == categoryId) || r.IsDefault)
            .OrderByDescending(r => r.CategoryId) // Kategori spesifik olan önce
            .FirstOrDefaultAsync(ct);

        if (rate is null)
            return new ErrorDataResult<CommissionCalculationResult>(null!,
                "Bu pazaryeri için komisyon oranı tanımlanmamış.");

        var commissionAmount = Math.Round(salePrice * rate.CommissionPercent / 100m, 2);
        var serviceFeeAmount = Math.Round(salePrice * (rate.ServiceFeePercent ?? 0m) / 100m, 2);
        var transactionFee = rate.TransactionFeeFixed ?? 0m;
        var totalDeductions = commissionAmount + serviceFeeAmount + transactionFee;
        var netRevenue = salePrice - totalDeductions;
        var netProfit = netRevenue - costPrice;
        var profitMarginPercent = salePrice > 0
            ? Math.Round(netProfit / salePrice * 100m, 2)
            : 0m;

        var result = new CommissionCalculationResult
        {
            SalePrice = salePrice,
            CommissionAmount = commissionAmount,
            ServiceFeeAmount = serviceFeeAmount,
            TransactionFee = transactionFee,
            TotalDeductions = totalDeductions,
            NetRevenue = netRevenue,
            CostPrice = costPrice,
            NetProfit = netProfit,
            ProfitMarginPercent = profitMarginPercent
        };

        return new SuccessDataResult<CommissionCalculationResult>(result);
    }

    public async Task<IDataResult<List<MarketplaceCommissionRateDto>>> GetCommissionRatesAsync(
        int marketPlaceId, CancellationToken ct = default)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var rates = await dbContext.MarketplaceCommissionRates
            .AsNoTracking()
            .Include(r => r.MarketPlace)
            .Include(r => r.Category)
            .Where(r => r.MarketPlaceId == marketPlaceId)
            .OrderByDescending(r => r.IsDefault)
            .ThenBy(r => r.Category != null ? r.Category.Name : "")
            .Select(r => new MarketplaceCommissionRateDto
            {
                Id = r.Id,
                MarketPlaceId = r.MarketPlaceId,
                MarketPlaceName = r.MarketPlace.Name,
                CategoryId = r.CategoryId,
                CategoryName = r.Category != null ? r.Category.Name : null,
                CommissionPercent = r.CommissionPercent,
                ServiceFeePercent = r.ServiceFeePercent,
                TransactionFeeFixed = r.TransactionFeeFixed,
                Description = r.Description,
                IsDefault = r.IsDefault
            })
            .ToListAsync(ct);

        return new SuccessDataResult<List<MarketplaceCommissionRateDto>>(rates);
    }

    public async Task<IResult> SaveCommissionRateAsync(SaveCommissionRateDto dto, CancellationToken ct = default)
    {
        // 1. Validation
        await validator.ValidateAndThrowAsync(dto);

        using var dbContext = contextFactory.CreateDbContext();

        // 2. Business Rules
        var marketplaceExists = await dbContext.MarketPlaces.AsNoTracking()
            .AnyAsync(m => m.Id == dto.MarketPlaceId, ct);
        if (!marketplaceExists)
            return new ErrorResult("Pazaryeri bulunamadı.");

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await dbContext.Categories.AsNoTracking()
                .AnyAsync(c => c.Id == dto.CategoryId.Value, ct);
            if (!categoryExists)
                return new ErrorResult("Kategori bulunamadı.");
        }

        // Default oran benzersiz olmalı
        if (dto.IsDefault)
        {
            var existingDefault = await dbContext.MarketplaceCommissionRates
                .AsNoTracking()
                .AnyAsync(r => r.MarketPlaceId == dto.MarketPlaceId
                               && r.IsDefault
                               && (!dto.Id.HasValue || r.Id != dto.Id.Value), ct);
            if (existingDefault)
                return new ErrorResult("Bu pazaryeri için zaten bir varsayılan komisyon oranı mevcut.");
        }

        // Aynı marketplace + kategori kombinasyonu benzersiz olmalı
        if (dto.CategoryId.HasValue)
        {
            var duplicateCategory = await dbContext.MarketplaceCommissionRates
                .AsNoTracking()
                .AnyAsync(r => r.MarketPlaceId == dto.MarketPlaceId
                               && r.CategoryId == dto.CategoryId
                               && (!dto.Id.HasValue || r.Id != dto.Id.Value), ct);
            if (duplicateCategory)
                return new ErrorResult("Bu pazaryeri ve kategori kombinasyonu için zaten bir komisyon oranı mevcut.");
        }

        // 3. Execution
        if (dto.Id.HasValue)
        {
            var existing = await dbContext.MarketplaceCommissionRates
                .FirstOrDefaultAsync(r => r.Id == dto.Id.Value, ct);
            if (existing is null)
                return new ErrorResult("Komisyon oranı bulunamadı.");

            existing.MarketPlaceId = dto.MarketPlaceId;
            existing.CategoryId = dto.CategoryId;
            existing.CommissionPercent = dto.CommissionPercent;
            existing.ServiceFeePercent = dto.ServiceFeePercent;
            existing.TransactionFeeFixed = dto.TransactionFeeFixed;
            existing.Description = dto.Description;
            existing.IsDefault = dto.IsDefault;

            dbContext.MarketplaceCommissionRates.Update(existing);
        }
        else
        {
            var entity = new MarketplaceCommissionRate
            {
                MarketPlaceId = dto.MarketPlaceId,
                CategoryId = dto.CategoryId,
                CommissionPercent = dto.CommissionPercent,
                ServiceFeePercent = dto.ServiceFeePercent,
                TransactionFeeFixed = dto.TransactionFeeFixed,
                Description = dto.Description,
                IsDefault = dto.IsDefault
            };

            dbContext.MarketplaceCommissionRates.Add(entity);
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Komisyon oranı kaydedildi: MarketPlace={MarketPlaceId}, Category={CategoryId}, Rate={Rate}%",
            dto.MarketPlaceId, dto.CategoryId, dto.CommissionPercent);

        return new SuccessResult("Komisyon oranı kaydedildi.");
    }

    public async Task<IResult> DeleteCommissionRateAsync(int rateId, CancellationToken ct = default)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var rate = await dbContext.MarketplaceCommissionRates
            .FirstOrDefaultAsync(r => r.Id == rateId, ct);

        if (rate is null)
            return new ErrorResult("Komisyon oranı bulunamadı.");

        rate.IsDeleted = true;
        rate.DeletedAt = DateTimeOffset.UtcNow;
        dbContext.MarketplaceCommissionRates.Update(rate);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Komisyon oranı silindi: Id={RateId}", rateId);

        return new SuccessResult("Komisyon oranı silindi.");
    }
}
