using System.Xml.Linq;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// N11 ürün mapper — ürün verisini N11 SaveProduct XML formatına dönüştürür.
/// </summary>
public interface IN11ProductMapper
{
    Task<IDataResult<XElement>> MapProductAsync(Guid productId);
}
