using System.Linq.Expressions;
using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class BrandManager
{
    private readonly IBrandDal _brandDal;
    private readonly FluentValidator _validator;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IMapper _mapper;
    public BrandManager(IBrandDal brandDal,FluentValidator validator,ApplicationLogManager applicationLogManager,IMapper mapper)
    {
        _brandDal = brandDal;
        _validator = validator;
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
    }

    public async Task<IDataResult<List<Brand>>> GetBrands()
    {
        var result = await _brandDal.GetAllAsync();
        return new SuccessDataResult<List<Brand>>(result);
    }
    public async Task<IResult> AddBrand(AddBrandDto brandDto)
    {
        await _applicationLogManager.AddLog("Marka ekleme isteği geldi.",LogType.Brand,LogAction.Add,brandDto);
        var brand = _mapper.Map<AddBrandDto,Brand>(brandDto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(brand.Name));
        if(result != null)
        {
            await _applicationLogManager.AddLog($"Marka eklenemedi. {result.Message}",LogType.Brand,LogAction.Add,brandDto);
            return new ErrorResult(result.Message);
        }
        await _validator.ValidateAndThrowAsync(brand);
        await _brandDal.AddAsync(brand);
        await _applicationLogManager.AddLog("Marka başarıyla eklendi.",LogType.Brand,LogAction.Add,brandDto);
        return new SuccessResult();
    }
    public async Task<IResult> UpdateBrand(Brand brand)
    {
        await _applicationLogManager.AddLog("Marka güncelleme isteği geldi.",LogType.Brand,LogAction.Update,brand);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(brand.Name));
        if(result != null)
        {
            await _applicationLogManager.AddLog($"Marka eklenemedi. {result.Message}",LogType.Brand,LogAction.Add,brand);
            return new ErrorResult(result.Message);
        }
        await _validator.ValidateAndThrowAsync(brand);
        await _brandDal.UpdateAsync(brand);
        await _applicationLogManager.AddLog("Marka başarıyla güncellendi.",LogType.Brand,LogAction.Update,brand);
        return new SuccessResult();
    }
    public async Task<IResult> DeleteBrand(Brand brand)
    {
        await _applicationLogManager.AddLog("Marka silme isteği geldi.",LogType.Brand,LogAction.Delete,brand);
        if(!await _brandDal.Exists(x => x.Id == brand.Id))
        {
            await _applicationLogManager.AddLog("Marka silme başarısız. İlgili id bulunamadı:",LogType.Brand,LogAction.Delete,brand);
            return new ErrorResult("Böyle bir marka bulunamadı");
        }
        await _brandDal.DeleteAsync(brand);
        await _applicationLogManager.AddLog("Marka başarıyla silindi.",LogType.Brand,LogAction.Delete,brand);
        return new SuccessResult();
    }
    public async Task<IDataResult<Pageable<BrandDetailDto>>> GetCategoryDetailPageable(GetCategoryDetailsPageDto dto)
    {
        Expression<Func<Brand,bool>> expr = 
            !string.IsNullOrEmpty(dto.CategoryName)
                ? x => EF.Functions.ILike(x.Name,@$"%{dto.CategoryName}%")
                :null;
        var result = await _brandDal.GetPaginatedTransformedEntities(dto.PageIndex,
            dto.ItemCount,
            x => new BrandDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count),
            expression: expr);
        return new SuccessDataResult<Pageable<BrandDetailDto>>(result);
    }

    private async Task<IResult> CheckIfTheSameNameExits(string name)
    {
        if(await _brandDal.Exists(x => x.Name == name))
            return new ErrorResult("Bu isimde bir marka zaten mevcut");
        return new SuccessResult();
    }
}