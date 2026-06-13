using Entegrasyon.Entity.Dtos.Marketplace;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Trendyol'a eşli kategorilerin attribute + değerlerini stage API'den toplayıp
/// (id+ad ile dedupe) tenant-cache'te tutar. Özellik eşleme dropdown'larını besler.
/// </summary>
public interface ITrendyolAttributeCatalog
{
    Task<IReadOnlyList<MarketplaceAttributeSearchResult>> SearchAttributesAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<MarketplaceOption>> SearchValuesAsync(int marketplaceAttributeId, string query, CancellationToken ct = default);

    /// <summary>
    /// Belirli bir pazaryeri kategorisi bağlamında özellik değerlerini döndürür. Global birleştirme
    /// yapmaz — aynı özelliğin (ör. "Malzeme") farklı kategorilerdeki farklı değer setleri karışmaz.
    /// </summary>
    Task<IReadOnlyList<MarketplaceOption>> SearchValuesForCategoryAsync(
        int marketplaceCategoryId, int marketplaceAttributeId, string query, CancellationToken ct = default);
}
