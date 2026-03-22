using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 ürün servisi — ürün kaydetme (SaveProduct) ve silme işlemleri.
/// N11'de batch publish yerine tekil SaveProduct SOAP call'u kullanılır.
/// </summary>
public interface IN11ProductService
{
    Task<IDataResult<long>> SaveProductAsync(Guid productId);
    Task<IResult> DeleteProductAsync(Guid productId);
}
