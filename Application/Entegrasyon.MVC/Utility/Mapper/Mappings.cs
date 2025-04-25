using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.MVC.ViewModels.Products;
using Shared.Entity;

namespace Entegrasyon.MVC.Utility.Mapper;

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

}
