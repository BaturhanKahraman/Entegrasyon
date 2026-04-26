using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Storefront;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontReviewManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IOptions<NotificationFeatureFlags> notificationFlags) : IStorefrontReviewManager
{
    public async Task<IDataResult<List<StorefrontReview>>> GetProductReviewsAsync(int tenantId, Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var reviews = await dbContext.StorefrontReviews
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontReview>>(reviews);
    }

    public async Task<IDataResult<StorefrontReview>> AddReviewAsync(
        int tenantId, Guid productId, int customerId, int rating, string comment, string? title)
    {
        if (rating < 1 || rating > 5)
            return new ErrorDataResult<StorefrontReview>(null!, "Puan 1-5 arasinda olmalidir.");

        if (string.IsNullOrWhiteSpace(comment))
            return new ErrorDataResult<StorefrontReview>(null!, "Yorum alani bos birakilamaz.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var alreadyReviewed = await dbContext.StorefrontReviews
            .AnyAsync(r => r.TenantId == tenantId && r.ProductId == productId && r.CustomerId == customerId);

        if (alreadyReviewed)
            return new ErrorDataResult<StorefrontReview>(null!, "Bu urun icin zaten yorum yaptiniz.");

        var isVerifiedPurchase = await dbContext.Orders
            .AnyAsync(o => o.CustomerId == customerId
                && o.OrderItems.Any(oi => oi.Product != null && oi.Product.ProductId == productId));

        var review = new StorefrontReview
        {
            TenantId = tenantId,
            ProductId = productId,
            CustomerId = customerId,
            Rating = rating,
            Title = title,
            Comment = comment,
            IsApproved = false,
            IsVerifiedPurchase = isVerifiedPurchase
        };

        dbContext.StorefrontReviews.Add(review);

        if (notificationFlags.Value.PublishEnabled)
            dbContext.AddDomainEvent(new StorefrontReviewSubmittedEvent(review.Id, productId, rating));

        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<StorefrontReview>(review, "Yorumunuz basariyla gonderildi. Onaylandiktan sonra yayinlanacaktir.");
    }

    public async Task<IDataResult<(double AverageRating, int ReviewCount)>> GetProductRatingAsync(int tenantId, Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var approvedReviews = dbContext.StorefrontReviews
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.ProductId == productId && r.IsApproved);

        var count = await approvedReviews.CountAsync();
        var avg = count > 0 ? await approvedReviews.AverageAsync(r => (double)r.Rating) : 0;

        return new SuccessDataResult<(double, int)>((avg, count));
    }

    public async Task<IDataResult<List<StorefrontReview>>> GetAllReviewsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var reviews = await dbContext.StorefrontReviews
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontReview>>(reviews);
    }

    public async Task<IResult> ApproveReviewAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var review = await dbContext.StorefrontReviews.FindAsync(id);
        if (review is null)
            return new ErrorResult("Yorum bulunamadi.");

        review.IsApproved = true;
        dbContext.StorefrontReviews.Update(review);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Yorum onaylandi.");
    }

    public async Task<IResult> RejectReviewAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var review = await dbContext.StorefrontReviews.FindAsync(id);
        if (review is null)
            return new ErrorResult("Yorum bulunamadi.");

        review.IsApproved = false;
        review.IsDeleted = true;
        review.DeletedAt = DateTimeOffset.UtcNow;
        dbContext.StorefrontReviews.Update(review);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Yorum reddedildi.");
    }

    public async Task<IResult> ReplyToReviewAsync(int id, string replyText)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var review = await dbContext.StorefrontReviews.FindAsync(id);
        if (review is null)
            return new ErrorResult("Yorum bulunamadi.");

        review.ReplyText = replyText;
        review.RepliedAt = DateTimeOffset.UtcNow;
        dbContext.StorefrontReviews.Update(review);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Yanit kaydedildi.");
    }
}
