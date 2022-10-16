using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Microsoft.AspNetCore.Http;

namespace Entegrasyon.Business.Concrete;

public class ImageManager
{
    private readonly IImageDal _imageDal;

    public ImageManager(IImageDal imageDal)
    {
        _imageDal = imageDal;
    }

    public async Task AddImage(Image img)
    {
        await _imageDal.AddAsync(img);
    }
    public async Task AddImages(IEnumerable<Image> imgs)
    {
        await _imageDal.AddRange(imgs);
    }
}