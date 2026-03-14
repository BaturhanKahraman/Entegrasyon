using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ITrendyolProductService
{
    Task<IDataResult<string>> PublishProductAsync(Guid productId);
    Task<IDataResult<TrendyolBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId);

    /// <summary>
    /// Onaysız ürün güncelleme — tüm alanlar gönderilebilir (unapproved-bulk-update).
    /// </summary>
    Task<IResult> UpdateUnapprovedProductAsync(Guid productId);

    /// <summary>
    /// Onaylı ürün güncelleme — sadece title, description, images, attributes + contentId zorunlu (content-bulk-update).
    /// </summary>
    Task<IResult> UpdateApprovedContentAsync(Guid productId);
}
