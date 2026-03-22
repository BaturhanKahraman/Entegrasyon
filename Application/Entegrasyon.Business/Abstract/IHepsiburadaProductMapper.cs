using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// İç ürün verisini Hepsiburada JSON formatına dönüştürür.
/// </summary>
public interface IHepsiburadaProductMapper
{
    Task<IDataResult<List<HepsiburadaProductItem>>> MapProductAsync(Guid productId);
}
