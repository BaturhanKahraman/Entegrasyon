using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public record VariantImageStream(Guid VariantId, Stream ImageStream, string FileName, bool IsMain);

public interface IImageManager
{
    Task<IResult> AddProductImages(Guid productId, IEnumerable<VariantImageStream> images);
}
