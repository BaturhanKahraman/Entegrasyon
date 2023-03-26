using System.Linq.Expressions;
using AutoMapper;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Logs;
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

    public async Task<IDataResult<List<BrandListDetailDto>>> GetBrandListDetails() {
        var order = new List<(string, string)>{ new("CreatedAt","Desc") };
        var result = await _brandDal.GetTransformedEntitiesAsync(x=>new BrandListDetailDto(x.Id,x.CreatedAt,x.Name,x.Products.Count()),order);
        return new SuccessDataResult<List<BrandListDetailDto>>(result);
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
        return new SuccessDataResult<BrandListDetailDto>(await _brandDal.ConvertToBrandDetail(brand));
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

        return new SuccessDataResult<BrandListDetailDto>(await _brandDal.ConvertToBrandDetail(brand),Messages.BrandUpdatedSuccessfuly);
    }
    public async Task<IResult> DeleteBrand(int id)
    {
        var brand = await _brandDal.GetAsync(x => x.Id == id);
        await _applicationLogManager.AddLog("Marka silme isteği geldi.",LogType.Brand,LogAction.Delete,brand);
        if(brand==null)
        {
            await _applicationLogManager.AddLog("Marka silme başarısız. İlgili id bulunamadı:",LogType.Brand,LogAction.Delete,id);
            return new ErrorResult("Böyle bir marka bulunamadı");
        }
        await _brandDal.SoftDeleteAsync(brand);
        await _applicationLogManager.AddLog("Marka başarıyla silindi.",LogType.Brand,LogAction.Delete,brand);
        return new SuccessResult();
    }
    public async Task<IDataResult<Pageable<BrandListDetailDto>>> GetCategoryDetailPageable(GetCategoryDetailsPageDto dto)
    {
        Expression<Func<Brand,bool>> expr = 
            !string.IsNullOrEmpty(dto.CategoryName)
                ? x => EF.Functions.ILike(x.Name,@$"%{dto.CategoryName}%")
                :null;
        var result = await _brandDal.GetPaginatedTransformedEntities(dto.PageIndex,
            dto.ItemCount,
            x => new BrandListDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()),
            expression: expr);
        return new SuccessDataResult<Pageable<BrandListDetailDto>>(result);
    }

    public async Task<IDataResult<Brand>> GetBrandById(int id)
    {
        return new SuccessDataResult<Brand>(await _brandDal.GetAsync(b => b.Id == id));
    }
    private async Task<IResult> CheckIfTheSameNameExits(string name)
    {
        if(await _brandDal.Exists(x => x.Name == name))
            return new ErrorResult("Bu isimde bir marka zaten mevcut");
        return new SuccessResult();
    }

    public async Task<IDataResult<BrandDetailDto>> GetBrandDetail(int id)
    {
        var entity =
            await _brandDal.GetTransformedEntity(x => new BrandDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()),
                x => x.Id == id);
        return new SuccessDataResult<BrandDetailDto>(entity);
    }
}