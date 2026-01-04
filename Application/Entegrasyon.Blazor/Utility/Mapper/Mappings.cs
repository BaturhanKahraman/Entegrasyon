using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Blazor.ViewModels.Category;
using Entegrasyon.Blazor.ViewModels.Products;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Entity;

namespace Entegrasyon.Blazor.Utility.Mapper;

public static class Mappings
{
    public static Pageable<TDestination> MapItems<TSource, TDestination>(
       this Pageable<TSource> source,
       Func<TSource,TDestination> mapFunc)
    {
        return new Pageable<TDestination>(
            [.. source.Items.Select(mapFunc)],
            source.CurrentPageIndex + 1,
            source.PagingItemCount,
            source.TotalItemCount
        );
    }

    public static Func<ProductsDetailDto,ProductDetailListViewModel> ToListViewModel =>
    item => new ProductDetailListViewModel(
            item.Id,
            item.Title,
            item.Description,
            item.StockCode,
            item.BrandName,
            item.CategoryName,
            item.TotalQuantity,
            item.TotalSoldQuantity,
            item.VariantCount
        );
    public static Func<CategoryDetailDto,CategoryDetailListViewModel> ToCategoryListViewModel =>
        dto=>new CategoryDetailListViewModel(
            dto.Id,
            dto.TotalProductCount,
            dto.Name,
            dto.SuperCategoryName,
            dto.SubCategoryCount,
            dto.IsFavorited,
            dto.AttributeCount
        );
    public static Func<CategoryUpsertViewModel,AddCategoryDto> ToAddCategoryDto =>
        vm => new AddCategoryDto(vm.Name,Enumerable.Empty<AddCategoryAttributeDto>(),vm.SuperCategoryId,vm.IsFavorite);
    public static Func<Category,CategoryUpsertViewModel> ToCategoryUpsertViewModel =>
        category => new CategoryUpsertViewModel
        {
            Id = category.Id,
            Name = category.Name,
            IsFavorite = category.IsFavorite,
            SuperCategoryId = category.SuperCategoryId,
            SuperCategories = new List<SelectListItem>(),
        };
    public static Func<CategoryUpsertViewModel,EditCategoryDto> ToEditCategoryDto =>
        vm => new EditCategoryDto(vm.Id,vm.Name,vm.SuperCategoryId,vm.IsFavorite,vm.IsImported);

}
