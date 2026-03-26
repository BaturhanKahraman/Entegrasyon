using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontCouponManager(IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontCouponManager
{
    public async Task<IDataResult<CouponValidationResult>> ValidateCouponAsync(
        string code, decimal cartTotal, int? customerId)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ErrorDataResult<CouponValidationResult>(null!, "Kupon kodu bos olamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var voucher = await dbContext.DiscountVouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Code == code);

        if (voucher is null)
            return new ErrorDataResult<CouponValidationResult>(null!, "Gecersiz kupon kodu.");

        if (!voucher.IsActive)
            return new ErrorDataResult<CouponValidationResult>(null!, "Bu kupon kodu artik gecerli degil.");

        if (voucher.ExpiringDate.HasValue && voucher.ExpiringDate.Value < DateTimeOffset.UtcNow)
            return new ErrorDataResult<CouponValidationResult>(null!, "Bu kupon kodunun suresi dolmus.");

        // If the voucher is assigned to a specific customer, validate ownership
        if (voucher.CustomerId.HasValue && voucher.CustomerId != customerId)
            return new ErrorDataResult<CouponValidationResult>(null!, "Bu kupon kodu hesabiniza tanimli degil.");

        // Calculate discount
        decimal discountAmount;
        string description;

        if (voucher.Percentage > 0)
        {
            discountAmount = Math.Round(cartTotal * (decimal)(voucher.Percentage / 100.0), 2);
            description = $"%{voucher.Percentage:0.##} indirim";
        }
        else if (voucher.Amount > 0)
        {
            discountAmount = Math.Min(voucher.Amount, cartTotal);
            description = $"{voucher.Amount:N2} TL indirim";
        }
        else
        {
            return new ErrorDataResult<CouponValidationResult>(null!, "Kupon kodu gecerli bir indirim icermiyor.");
        }

        if (discountAmount <= 0)
            return new ErrorDataResult<CouponValidationResult>(null!, "Bu kupon kodu sepet tutariniza uygulanamaz.");

        var result = new CouponValidationResult(voucher.Id, voucher.Code!, discountAmount, description);
        return new SuccessDataResult<CouponValidationResult>(result, "Kupon basariyla uygulandi.");
    }
}
