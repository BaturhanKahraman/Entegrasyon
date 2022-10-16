using System.Collections;
using System.Transactions;
using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Shared.FileStorage;
using Shared.Helpers;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class ProductManager
{
    private readonly IMainProductDal _productDal;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IMapper _mapper;
    private readonly IFileStorage _fileStorage;
    private readonly FluentValidator _validator;
    private readonly OfficeStockManager _officeStockManager;
    private readonly ImageManager _imageManager;
    public ProductManager(IMainProductDal productDal, ApplicationLogManager applicationLogManager, IRandomGenerator randomGenerator, IMapper mapper, IFileStorage fileStorage, FluentValidator validator, OfficeStockManager officeStockManager, ImageManager imageManager)
    {
        _productDal = productDal;
        _applicationLogManager = applicationLogManager;
        _randomGenerator = randomGenerator;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _validator = validator;
        _officeStockManager = officeStockManager;
        _imageManager = imageManager;
    }

    public async Task<IResult> AddProduct(AddProductDto dto)
    {
        await _applicationLogManager.AddLog("Ürün ekleme isteği geldi.",LogType.Product,LogAction.Add,dto);
        await _validator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(
            await _officeStockManager.CheckIfOfficeExists(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks.Select(y => y.BranchOfficeId)).ToArray())
            );
        if (result != null)
            return result;
        foreach (var productVariantDto in dto.ProductVariants.Where(productVariantDto => string.IsNullOrEmpty(productVariantDto.Barcode)))
            productVariantDto.Barcode = _randomGenerator.GetRandomCode(13, true, false, false);
        
        
        var product = _mapper.Map<MainProduct>(dto);
        await _productDal.AddAsync(product);
        //product.StockCode = _randomGenerator.GetRandomCode(10);
        //foreach (var variant in product.ProductVariants)
        //  variant.Barcode = _randomGenerator.GetRandomCode(8,true,false,false);
        //todo gerekirse ayrı yere taşı.
        var images = new List<Image>();
        foreach(var productVariant in product.ProductVariants)
        {
            var productVariantDto =
                dto.ProductVariants.FirstOrDefault(x => x.Barcode == productVariant.Barcode);
            if(productVariantDto!.UploadedImages == null)
                continue;
            foreach(var formFile in productVariantDto.UploadedImages)
            {
                string extension = formFile.FileName.Split('.').LastOrDefault();
                string imageGuid =Guid.NewGuid() +"."+ extension;
                await _fileStorage.UploadFile(formFile.OpenReadStream(),imageGuid,
                    productVariant.Id.ToString());
                var image = new Image()
                {
                    ProductVariant = productVariant,
                    Src = Path.Combine(productVariant.Id.ToString(),imageGuid)
                };
                images.Add(image);
            }
        }
        await _imageManager.AddImages(images);
        return new SuccessResult();
    }
    public async Task<IResult> UpdateProduct()
    {
        return new SuccessResult();
    }
    public async Task<IResult> GetProductDetail()
    {
        return new SuccessResult();
    }
    public async Task<IResult> GetProductsDetails()
    {
        return new SuccessResult();
    }
    
}