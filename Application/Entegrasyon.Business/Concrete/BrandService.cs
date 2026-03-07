using System.Linq.Expressions;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Requests;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class BrandService(IFluentValidator validator, IApplicationLogManager applicationLogManager, IMapper mapper,IntegrationDbContext dbContext)
    : IBrandService
{

    public async Task<IDataResult<List<BrandListDetailDto>>> GetBrandListDetails() {
        var order = new List<(string, string)>{ new("CreatedAt","Desc") };
        var result = await .GetTransformedEntitiesAsync(x=>new BrandListDetailDto(x.Id,x.CreatedAt,x.Name,x.Products.Count()),order);
        return new SuccessDataResult<List<BrandListDetailDto>>(result);
    }
    public async Task<IResult> AddBrand(AddBrandDto brandDto)
    {
        await applicationLogManager.AddLog("Marka ekleme isteği geldi.",LogType.Brand,LogAction.Add,brandDto);
        await validator.ValidateAndThrowAsync(brandDto);
        var brand = mapper.Map<AddBrandDto,Brand>(brandDto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(brand.Name));
        if(result != null)
        {
            await applicationLogManager.AddLog($"Marka eklenemedi. {result.Message}",LogType.Brand,LogAction.Add,brandDto);
            return new ErrorResult(result.Message);
        }
        await _brandDal.AddAsync(brand);
        await applicationLogManager.AddLog("Marka başarıyla eklendi.",LogType.Brand,LogAction.Add,brandDto);
        return new SuccessDataResult<Brand>(brand);
    }
    public async Task<IResult> UpdateBrand(Brand brand)
    {
        await applicationLogManager.AddLog("Marka güncelleme isteği geldi.",LogType.Brand,LogAction.Update,brand);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(brand.Name));
        if(result != null)
        {
            await applicationLogManager.AddLog($"Marka eklenemedi. {result.Message}",LogType.Brand,LogAction.Add,brand);
            return new ErrorResult(result.Message);
        }
        await validator.ValidateAndThrowAsync(brand);
        await _brandDal.UpdateAsync(brand);
        await applicationLogManager.AddLog("Marka başarıyla güncellendi.",LogType.Brand,LogAction.Update,brand);

        return new SuccessDataResult<BrandListDetailDto>(await _brandDal.ConvertToBrandDetail(brand),Messages.BrandUpdatedSuccessfuly);
    }
    public async Task<IResult> DeleteBrand(int id)
    {
        var brand = await _brandDal.GetAsync(x => x.Id == id);
        await applicationLogManager.AddLog("Marka silme isteği geldi.",LogType.Brand,LogAction.Delete,brand);
        if(brand==null)
        {
            await applicationLogManager.AddLog("Marka silme başarısız. İlgili id bulunamadı:",LogType.Brand,LogAction.Delete,id);
            return new ErrorResult("Böyle bir marka bulunamadı");
        }
        await _brandDal.SoftDeleteAsync(brand);
        await applicationLogManager.AddLog("Marka başarıyla silindi.",LogType.Brand,LogAction.Delete,brand);
        return new SuccessResult();
    }
    public async Task<IDataResult<Pageable<BrandListDetailDto>>> GetBrandDetailPageable(BrandDetailPaginatedRequest request)
    {
        IQueryable<Brand> query = dbContext.Brands;
        query.ApplyGlobalSearch(request.SearchTerm, nameof(Brand.Name));

        IQueryable<BrandListDetailDto> projectedQuery = query.Select(x => new BrandListDetailDto(
            x.Id,
            x.CreatedAt,
            x.Name,
            x.Products.Count()
        ));

        var pagedResult = await projectedQuery.ToPageableAsync(request);

        return new SuccessDataResult<Pageable<BrandListDetailDto>>(pagedResult);
    }

    public async Task<IDataResult<Brand>> GetBrandById(int id)
    {
        return new SuccessDataResult<Brand>(await dbContext.Brands.FindAsync(id));
    }
    private async Task<IResult> CheckIfTheSameNameExits(string name)
    {
        if(await dbContext.Brands.AnyAsync(x => x.Name == name))
            return new ErrorResult("Bu isimde bir marka zaten mevcut");
        return new SuccessResult();
    }

    public async Task<IDataResult<BrandDetailDto>> GetBrandDetail(int id)
    {
        var entity =
            await dbContext.Brands.Select(x => new BrandDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
                .FirstOrDefaultAsync(x => x.Id == id);

        return new SuccessDataResult<BrandDetailDto>(entity);
    }
}
