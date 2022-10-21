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
    private static readonly SemaphoreSlim SemaphoreSlim = new(1);

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
            await _officeStockManager.CheckIfOfficeExists(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks.Select(y => y.BranchOfficeId)).ToArray()),
            _officeStockManager.CheckIfProductCountZero(dto.ProductVariants.SelectMany(x => x.BranchOfficeStocks).ToArray())
            );
        if (result != null)
            return result;
        foreach (var productVariantDto in dto.ProductVariants.Where(productVariantDto => string.IsNullOrEmpty(productVariantDto.Barcode)))
            productVariantDto.Barcode = _randomGenerator.GetRandomCode(13, true, false, false);
        var product = _mapper.Map<MainProduct>(dto);
        await _productDal.AddAsync(product);
        await _imageManager.AddProductImages(dto,product);
        return new SuccessResult();
    }
    public async Task<IResult> UpdateProduct()
    {
        await SemaphoreSlim.WaitAsync();
        SemaphoreSlim.Release();
        return new SuccessResult();
    }
    public async Task<IResult> DeactiveProduct()
    {
        return new SuccessResult();
    }
    public async Task<IResult> UpdateProductStock()
    {
        return new SuccessResult();
    }
    public async Task<IResult> GetProductDetail()
    {
        
        return new SuccessResult();
    }
    public async Task<IResult> GetProductsDetailsPageable()
    {
        return new SuccessResult();
    }
    
}