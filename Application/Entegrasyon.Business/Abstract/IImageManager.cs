using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface IImageManager
{
    Task<IResult> AddProductImages(AddProductDto dto, Product addedProduct);
}
