using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using Shared.FileStorage;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class ImageManager
{
    private readonly IImageDal _imageDal;
    private readonly IFileStorage _fileStorage;

    public ImageManager(IImageDal imageDal, IFileStorage fileStorage)
    {
        _imageDal = imageDal;
        _fileStorage = fileStorage;
    }

    public async Task<IResult> AddProductImages(AddProductDto dto,MainProduct addedProduct)
    {
        var images = new List<Image>();
        foreach(var productVariant in addedProduct.ProductVariants)
        {
            var productVariantDto =
                dto.ProductVariants.FirstOrDefault(x => x.Barcode == productVariant.Barcode);
            if(productVariantDto!.UploadedImages == null)
                continue;
            foreach(var formFile in productVariantDto.UploadedImages)
            {
                string containerName = Path.Combine("images",productVariant.Id.ToString());
                string extension = formFile.FileName.Split('.').LastOrDefault();
                string imageName = Path.GetRandomFileName() + "." + extension;
                string imageUrl = await _fileStorage.UploadFile(formFile.OpenReadStream(),imageName,
                    containerName);
                var image = new Image()
                {
                    ProductVariant = productVariant,
                    Src = imageUrl,
                    AlternativeText = formFile.FileName,
                    FileStorageType = _fileStorage.FileStorageType
                };
                images.Add(image);
            }
        }
        await _imageDal.AddRange(images);
        return new SuccessResult();
    }
}