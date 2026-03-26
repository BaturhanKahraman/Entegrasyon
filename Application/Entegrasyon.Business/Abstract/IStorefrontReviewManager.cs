using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontReviewManager
{
    Task<IDataResult<List<StorefrontReview>>> GetProductReviewsAsync(int tenantId, Guid productId);
    Task<IDataResult<StorefrontReview>> AddReviewAsync(int tenantId, Guid productId, int customerId, int rating, string comment, string? title);
    Task<IDataResult<(double AverageRating, int ReviewCount)>> GetProductRatingAsync(int tenantId, Guid productId);
    Task<IDataResult<List<StorefrontReview>>> GetAllReviewsAsync(int tenantId);
    Task<IResult> ApproveReviewAsync(int id);
    Task<IResult> RejectReviewAsync(int id);
    Task<IResult> ReplyToReviewAsync(int id, string replyText);
}
