using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
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

    /// <summary>
    /// Ürünü Trendyol'dan siler.
    /// Silinebilir: onay bekleyenler + 1 günden fazla arşivlenmiş ürünler.
    /// </summary>
    Task<IResult> DeleteProductAsync(Guid productId);

    /// <summary>
    /// Trendyol'a gönderilecek ürünün ön izlemesini oluştur.
    /// Override'lar varsa (title, description, variant fiyatları) bunları uygula.
    /// </summary>
    Task<IDataResult<TrendyolSendPreviewDto>> GetSendPreviewAsync(
        Guid productId,
        MarketplaceOverrideDetailDto? overrides);
}
