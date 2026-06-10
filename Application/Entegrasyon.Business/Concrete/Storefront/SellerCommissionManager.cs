using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class SellerCommissionManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : ISellerCommissionManager
{
    public async Task<decimal> GetCommissionRateAsync(int sellerId, int? categoryId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Check seller+category specific commission first
        if (categoryId.HasValue)
        {
            var categoryCommission = await dbContext.SellerCommissions
                .FirstOrDefaultAsync(c => c.SellerId == sellerId && c.CategoryId == categoryId);

            if (categoryCommission is not null)
                return categoryCommission.CommissionRate;
        }

        // Fallback to seller default commission
        var seller = await dbContext.Sellers.FindAsync(sellerId);
        return seller?.DefaultCommissionRate ?? 10;
    }

    public async Task<IResult> RecordSaleCommissionAsync(int sellerId, Guid orderId, decimal saleAmount, decimal commissionRate)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var balance = await dbContext.SellerBalances
            .FirstOrDefaultAsync(b => b.SellerId == sellerId);

        if (balance is null)
            return new ErrorResult("Satici bakiyesi bulunamadı.");

        var commissionAmount = saleAmount * (commissionRate / 100m);
        var netAmount = saleAmount - commissionAmount;

        // Record sale transaction
        var saleTx = new SellerTransaction
        {
            SellerId = sellerId,
            OrderId = orderId,
            Amount = saleAmount,
            TransactionType = "Sale",
            Description = $"Sipariş satisi",
            BalanceAfter = balance.CurrentBalance + netAmount
        };

        // Record commission deduction transaction
        var commissionTx = new SellerTransaction
        {
            SellerId = sellerId,
            OrderId = orderId,
            Amount = -commissionAmount,
            TransactionType = "Commission",
            Description = $"Komisyon kesintisi (%{commissionRate})",
            BalanceAfter = balance.CurrentBalance + netAmount
        };

        balance.TotalEarned += netAmount;
        balance.CurrentBalance += netAmount;

        dbContext.SellerTransactions.Add(saleTx);
        dbContext.SellerTransactions.Add(commissionTx);
        dbContext.SellerBalances.Update(balance);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Komisyon kaydedildi.");
    }

    public async Task<IResult> ProcessOrderCommissionsAsync(Guid orderId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var order = await dbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorResult("Sipariş bulunamadı.");

        var sellerItems = order.OrderItems.Where(oi => oi.SellerId.HasValue).ToList();
        if (!sellerItems.Any())
            return new SuccessResult("Satici urunu yok, komisyon islenmedi.");

        foreach (var item in sellerItems)
        {
            var sellerId = item.SellerId!.Value;
            var saleAmount = item.UnitPrice * item.Quantity;

            // Look up the product's category for category-specific commission
            int? categoryId = null;
            if (item.ProductId.HasValue)
            {
                var sellerProduct = await dbContext.SellerProducts
                    .Include(sp => sp.Product)
                    .FirstOrDefaultAsync(sp => sp.SellerId == sellerId && sp.ProductId == item.ProductId.Value);

                categoryId = sellerProduct?.Product?.CategoryId;
            }

            var rate = await GetCommissionRateInternalAsync(dbContext, sellerId, categoryId);

            var commissionAmount = saleAmount * (rate / 100m);

            // Update the order item with commission info
            item.CommissionRate = rate;
            item.CommissionAmount = commissionAmount;
            dbContext.Entry(item).State = EntityState.Modified;

            // Record commission in balance
            var balance = await dbContext.SellerBalances
                .FirstOrDefaultAsync(b => b.SellerId == sellerId);

            if (balance is null) continue;

            var netAmount = saleAmount - commissionAmount;

            var saleTx = new SellerTransaction
            {
                SellerId = sellerId,
                OrderId = orderId,
                Amount = saleAmount,
                TransactionType = "Sale",
                Description = $"Sipariş satisi",
                BalanceAfter = balance.CurrentBalance + netAmount
            };

            var commissionTx = new SellerTransaction
            {
                SellerId = sellerId,
                OrderId = orderId,
                Amount = -commissionAmount,
                TransactionType = "Commission",
                Description = $"Komisyon kesintisi (%{rate})",
                BalanceAfter = balance.CurrentBalance + netAmount
            };

            balance.TotalEarned += netAmount;
            balance.CurrentBalance += netAmount;

            dbContext.SellerTransactions.Add(saleTx);
            dbContext.SellerTransactions.Add(commissionTx);
            dbContext.SellerBalances.Update(balance);
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Komisyonlar basariyla islendi.");
    }

    public async Task<IDataResult<List<SellerCommission>>> GetCommissionsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var commissions = await dbContext.SellerCommissions
            .Include(c => c.Seller)
            .Where(c => c.TenantId == tenantId && !c.IsDeleted)
            .OrderBy(c => c.Seller.StoreName)
            .ToListAsync();

        return new SuccessDataResult<List<SellerCommission>>(commissions);
    }

    public async Task<IResult> UpdateCommissionRateAsync(int commissionId, decimal newRate)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var commission = await dbContext.SellerCommissions.FindAsync(commissionId);
        if (commission is null)
            return new ErrorResult("Komisyon kaydi bulunamadı.");

        commission.CommissionRate = newRate;
        dbContext.SellerCommissions.Update(commission);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Komisyon orani guncellendi.");
    }

    private static async Task<decimal> GetCommissionRateInternalAsync(
        IntegrationDbContext dbContext, int sellerId, int? categoryId)
    {
        if (categoryId.HasValue)
        {
            var categoryCommission = await dbContext.SellerCommissions
                .FirstOrDefaultAsync(c => c.SellerId == sellerId && c.CategoryId == categoryId);

            if (categoryCommission is not null)
                return categoryCommission.CommissionRate;
        }

        var seller = await dbContext.Sellers.FindAsync(sellerId);
        return seller?.DefaultCommissionRate ?? 10;
    }
}
