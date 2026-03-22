using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAmazonListingService
{
    Task<IDataResult<AmazonListingSubmissionResponse>> PutListingItemAsync(
        string sellerId, string sku, AmazonListingItem item, string[] marketplaceIds, CancellationToken ct = default);
    Task<IDataResult<AmazonListingSubmissionResponse>> PatchListingItemAsync(
        string sellerId, string sku, AmazonListingPatchRequest patches, string[] marketplaceIds, CancellationToken ct = default);
    Task<IDataResult<AmazonListingItemResponse>> GetListingItemAsync(
        string sellerId, string sku, string[] marketplaceIds, CancellationToken ct = default);
    Task<IResult> DeleteListingItemAsync(
        string sellerId, string sku, string[] marketplaceIds, CancellationToken ct = default);
}
