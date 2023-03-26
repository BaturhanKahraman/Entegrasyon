using AutoMapper;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Results;
using System.Linq.Expressions;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;

namespace Entegrasyon.Business.Concrete
{
    public class CategoryManager
    {
        private readonly ICategoryDal _categoryDal;
        private readonly ApplicationLogManager _applicationLogManager;
        private readonly CategoryAttributeManager _categoryAttributeManager;
        private readonly CategoryAttributeCategoryManager _categoryAttributeCategoryManager;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly FluentValidator _fluentValidator;

        public CategoryManager(ICategoryDal categoryDal,ApplicationLogManager applicationLogManager,IMapper mapper, CategoryAttributeManager categoryAttributeManager,IUnitOfWork unitOfWork, CategoryAttributeCategoryManager categoryAttributeCategoryManager, FluentValidator fluentValidator)
        {
            _categoryDal = categoryDal;
            _applicationLogManager = applicationLogManager;
            _mapper = mapper;
            _categoryAttributeManager = categoryAttributeManager;
            _unitOfWork = unitOfWork;
            _categoryAttributeCategoryManager = categoryAttributeCategoryManager;
            _fluentValidator = fluentValidator;
        }

        public async Task<IDataResult<CategoryDetailDto>> AddCategory(AddCategoryDto dto)
        {
            await _applicationLogManager.AddLog("Kategori ekleniyor.",LogType.Category,LogAction.Add,dto);
            var category = _mapper.Map<Category>(dto);
            //TODO 
            //2 den fazla varyant/slicer eklenememeli.
            var categoryAttributes = _mapper.Map<List<CategoryAttribute>>(dto.CategoryAttributes);
            categoryAttributes = await _categoryAttributeManager.AddIfNotExits(categoryAttributes);
            categoryAttributes.ForEach(ca =>
            {
                category.CategoryAttributes.Add(new CategoryAttributeCategory
                {
                    CategoryAttributeId = ca.Id,
                    IsRequired = ca.IsRequired,
                    IsSlicer = ca.IsSlicer,
                    IsVarianter = ca.IsVarianter
                });
            });
            
            await _categoryDal.AddAsync(category);
            await _applicationLogManager.AddLog(Messages.CategoryAdded,LogType.Category,LogAction.Add);
            CategoryDetailDto detail = await _categoryDal.ConvertToCategoryDetail(category);
            return new SuccessDataResult<CategoryDetailDto>(detail);
        }

        public async Task<IDataResult<CategoryDetailDto>> UpdateCategory(EditCategoryDto dto)
        {
            await _fluentValidator.ValidateAndThrowAsync(dto);
            var dbCategory = await _categoryDal.GetAsync(x => x.Id == dto.Id, true);
            if (dbCategory==null)
            {
                return new ErrorDataResult<CategoryDetailDto>(null, "Kategori bulunamadı.");
            }
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                dbCategory.IsFavorite = dto.IsFavorite;
                dbCategory.Name = dto.Name;
                if(await _categoryDal.Exists(c=>c.Id==dto.SuperCategoryId))
                    dbCategory.SuperCategoryId = dto.SuperCategoryId;
                var detailedCategory = await _categoryDal
                    .Table
                    .Include(x => x.CategoryAttributes)
                    .ThenInclude(x => x.CategoryAttribute)
                    .ThenInclude(x => x.CategoryAttributeValues)
                    .FirstAsync(x => x.Id == dbCategory.Id);
                List<CategoryAttributeCategory> categoryAttributeCategories=new();
                var dbCatAttrManyToManyTable = detailedCategory.CategoryAttributes;
                foreach (var dtoCatAttrs in dto.CategoryAttributes)
                {
                    //many to many tablodaki kayıt, bundan cat attr ulaşılacak.
                    var dbcatAttrMtM = dbCatAttrManyToManyTable.FirstOrDefault(x =>
                        x.CategoryId == detailedCategory.Id && x.CategoryAttributeId == dtoCatAttrs.Id);
                    if(dbcatAttrMtM==null)
                        continue;
                    dbcatAttrMtM.IsRequired = dtoCatAttrs.IsRequired;
                    dbcatAttrMtM.IsVarianter = dtoCatAttrs.IsVarianter;
                    dbcatAttrMtM.IsVarianter = dbcatAttrMtM.IsVarianter;
                    var dbCatAttr = dbcatAttrMtM.CategoryAttribute;
                    dbCatAttr.AllowCustom = dtoCatAttrs.AllowCustom;
                    dbCatAttr.CategoryAttributeHumanized = dtoCatAttrs.CategoryAttributeHumanized;
                    dbCatAttr.CategoryAttributeKey = dbCatAttr.CategoryAttributeKey;
                    List<CategoryAttributeValue> values=new();
                    foreach (var dtoCategoryAttributeValue in dtoCatAttrs.CategoryAttributeValues)
                    {
                        if (dtoCategoryAttributeValue.Id == 0)
                        {
                            values.Add(dtoCategoryAttributeValue);
                            continue;
                        }
                        var dbCatAttrValue =
                            dbCatAttr.CategoryAttributeValues.FirstOrDefault(x => x.Id == dtoCategoryAttributeValue.Id);
                        if (dbCatAttrValue!=null)
                        {
                            dbCatAttrValue.Name = dtoCategoryAttributeValue.Name;
                            values.Add(dbCatAttrValue);
                        }
                    }
                    dbCatAttr.CategoryAttributeValues = values;
                    categoryAttributeCategories.Add(dbcatAttrMtM);
                }
                detailedCategory.CategoryAttributes = categoryAttributeCategories;
                //dbCategory.CategoryAttributes=await _categoryAttributeCategoryManager.UpdateRangeCategoryAttributeCategories(dbCategory.Id,
                //    dto.CategoryAttributes);
                //await _categoryDal.UpdateAsync(dbCategory);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitAsync();
                CategoryDetailDto detail = await _categoryDal.ConvertToCategoryDetail(dbCategory);
                return new SuccessDataResult<CategoryDetailDto>(detail);
            }
            catch
            {
                await _unitOfWork.RollBackAsync();
                throw;
            }

        }

        public async Task DeleteCategory(int categoryId)
        {
            await _applicationLogManager.AddLog("Kategori siliniyor.",LogType.Category,LogAction.Delete,new { categoryId });
            var category = await _categoryDal.GetAsync(x => x.Id == categoryId);
            await _categoryDal.DeleteAsync(category);
            await _applicationLogManager.AddLog("Kategori silindi.",LogType.Category,LogAction.Delete,new { categoryId });
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetCategoryDetailList()
        {
            var orderTuples = new List<(string, string)>
            {
                new ("IsFavorite", "desc"),
                new ("Id", "desc")
            };
            var categoriesDto = await _categoryDal.GetTransformedEntitiesAsync(x => new CategoryDetailDto(
                x.Id,
                x.Products.Sum(p => p.ProductVariants
                    .SelectMany(pv => pv.BranchOfficeStocks)
                    .Sum(bo => bo.CurrentStock)),x.Name,x.SubCategories.Count(),x.IsFavorite,x.CategoryAttributes.Count()),orderTuples: orderTuples);

            return new SuccessDataResult<List<CategoryDetailDto>>(categoriesDto);
        }
        public async Task<IDataResult<Pageable<CategoryDetailDto>>> GetCategoryDetailPageable(int pageIndex = 1,int itemCount = 50,string categoryName = null)
        {
            var orderBy = new List<(string, string)>
            {
                new ("Id", "desc")
            };
            Expression<Func<Category,bool>> filter = !string.IsNullOrEmpty(categoryName)
                ? x => EF.Functions.ILike(x.Name,$"%{categoryName}%")
                : null;

            var categoriesDto = await _categoryDal.GetPaginatedTransformedEntities(pageIndex,itemCount,
                x => new CategoryDetailDto(x.Id,x.Products.Count(),x.Name,x.SubCategories.Count(),x.IsFavorite,x.CategoryAttributes.Count()),orderBy,filter);
            return new SuccessDataResult<Pageable<CategoryDetailDto>>(categoriesDto);
        }

        public async Task<IResult> AddFavorite(int categoryId)
        {
            var category = await _categoryDal.GetAsync(x => x.Id == categoryId);
            if(category == null)
                return new ErrorResult("Böyle bir kategori bulunamadı.");
            category.IsFavorite = true;
            await _categoryDal.UpdateAsync(category);
            return new SuccessResult("Kategori başarı ile favorilere eklendi.");
        }
        public async Task<IResult> AddFavorites(int[] categoryIds)
        {
            var categories = await _categoryDal.GetAllAsync(x => categoryIds.Contains(x.Id));
            if(categories == null || !categories.Any())
                return new ErrorResult("Bulunamayan kategori var.");
            categories.ForEach(x => x.IsFavorite = true);
            await _categoryDal.UpdateRangeAsync(categories);
            return new SuccessResult("Kategoriler başarı ile favorilere eklendi.");
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetFavoriteCategories()
        {
            var orderTuples = new List<(string, string)>
            {
                new("Id", "desc")
            };
            var result = await _categoryDal.GetTransformedEntitiesAsync(x =>
                    new CategoryDetailDto(x.Id,x.Products.Count(),x.Name,x.SubCategories.Count(),x.IsFavorite,x.CategoryAttributes.Count()),orderTuples,x => x.IsFavorite)
                ;
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }
        public async Task<IDataResult<List<CategoryDetailDto>>> GetSubCategories()
        {
            var orderTuples = new List<(string, string)>
            {
                new ("IsFavorite","desc"),
                new("Name","asc"),
            };
            var result = await _categoryDal.GetTransformedEntitiesAsync(x =>
                    new CategoryDetailDto(x.Id,x.Products.Count(),x.Name,x.SubCategories.Count(),x.IsFavorite,x.CategoryAttributes.Count()),orderTuples,
                x => !x.SubCategories.Any());
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetSuperCategories()
        {
            var orderTuples = new List<(string, string)>
            {
                new ("IsFavorite","desc"),
                new("Name","asc"),
            };
            var result = await _categoryDal.GetTransformedEntitiesAsync(x =>
                    new CategoryDetailDto(x.Id,x.Products.Count(),x.Name,x.SubCategories.Count(),x.IsFavorite,x.CategoryAttributes.Count()),orderTuples,
                x => !x.CategoryAttributes.Any());
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<CategoryEditDetailDto>> GetCategoryEditDetail(int id)
        {
            var result = await _categoryDal.GetTransformedEntity(category => new CategoryEditDetailDto(
                category.Id, category.Name,
                category.CategoryAttributes.Select(attributeCategory => new EditCategoryAttributeDto(
                    attributeCategory.CategoryAttribute.Id,
                    attributeCategory.CategoryAttribute.IsRequired,
                    attributeCategory.CategoryAttribute.AllowCustom,
                    attributeCategory.IsVarianter,
                    attributeCategory.CategoryAttribute.CategoryAttributeKey,
                    attributeCategory.IsSlicer,
                    attributeCategory.CategoryAttribute.CategoryAttributeHumanized,
                    attributeCategory.CategoryAttribute.CategoryAttributeValues.ToList(),
                    attributeCategory.CategoryId
                )).ToList()
                , category.SuperCategoryId, category.IsFavorite
            ),x=>x.Id==id);

            return new SuccessDataResult<CategoryEditDetailDto>(result);
        }

     
    }
}
