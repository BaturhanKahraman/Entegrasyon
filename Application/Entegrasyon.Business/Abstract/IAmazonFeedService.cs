using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAmazonFeedService
{
    Task<IDataResult<string>> SubmitFeedAsync(
        string feedType, string contentType, byte[] content,
        string[] marketplaceIds, CancellationToken ct = default);
    Task<IDataResult<AmazonFeedStatusResponse>> GetFeedStatusAsync(
        string feedId, CancellationToken ct = default);
}
