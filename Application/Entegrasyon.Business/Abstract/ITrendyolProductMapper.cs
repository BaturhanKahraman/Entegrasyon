using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Product + Variants → List&lt;TrendyolProductItem&gt; dönüşümü.
/// Her varyant ayrı bir TrendyolProductItem olur, hepsi aynı productMainId altında gruplanır.
/// </summary>
public interface ITrendyolProductMapper
{
    Task<IDataResult<TrendyolCreateProductRequest>> MapProductAsync(Guid productId);
}
