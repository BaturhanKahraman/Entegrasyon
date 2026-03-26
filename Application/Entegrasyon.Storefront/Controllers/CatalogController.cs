using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.Storefront.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Storefront.Controllers;

public class CatalogController(
    IStorefrontTenantContext tenant,
    IProductService productService,
    ICategoryService categoryService,
    IBrandService brandService,
    IStorefrontSearchHistoryManager searchHistoryManager,
    ISellerManager sellerManager,
    IDbContextFactory<IntegrationDbContext> contextFactory) : Controller
{
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Categories()
    {
        var treeResult = await categoryService.GetCategoryTreeAsync();
        var tree = treeResult.Success ? treeResult.Data : new List<CategoryTreeDto>();

        ViewBag.SeoTitle = $"Kategoriler - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = $"{tenant.Settings.StoreName} urun kategorileri";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new("Kategoriler", "/kategoriler")
        };

        return View(tree);
    }

    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "Page", "SortBy", "MinPrice", "MaxPrice" })]
    public async Task<IActionResult> Category(string slug, string? parentSlug,
        [FromQuery] StorefrontCatalogQuery query)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var categoryResult = await categoryService.GetCategoryBySeoSlugAsync(slug);
        if (!categoryResult.Success || categoryResult.Data is null)
            return NotFound();

        var category = categoryResult.Data;
        var filteredQuery = query with { CategoryId = category.Id };
        var productsResult = await productService.GetStorefrontProductsAsync(filteredQuery);
        var products = productsResult.Success ? productsResult.Data : EmptyPage();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var pageUrl = parentSlug != null
            ? $"{baseUrl}/kategori/{parentSlug}/{slug}"
            : $"{baseUrl}/kategori/{slug}";
        ViewData["JsonLd"] = JsonLdBuilder.BuildCollectionPage(
            category.Name, category.SeoDescription, pageUrl);

        // Fetch available brands for filter sidebar
        var brandsResult = await productService.GetBrandsForCategoryAsync(category.Id);
        ViewBag.AvailableBrands = brandsResult.Success ? brandsResult.Data : new List<Entity.Dtos.Storefront.BrandFilterDto>();

        ViewBag.Products = products;
        ViewBag.Query = filteredQuery;
        ViewBag.Category = category;
        ViewBag.PageTitle = category.Name;
        ViewBag.SeoTitle = category.SeoTitle ?? $"{category.Name} - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = category.SeoDescription
            ?? $"{category.Name} kategorisindeki urunler - {tenant.Settings.StoreName}";

        var breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new("Kategoriler", "/kategoriler")
        };
        if (category.SuperCategory != null && parentSlug != null)
        {
            breadcrumbs.Add(new BreadcrumbItemDto(
                category.SuperCategory.Name,
                $"/kategori/{parentSlug}"));
        }
        breadcrumbs.Add(new BreadcrumbItemDto(category.Name,
            parentSlug != null ? $"/kategori/{parentSlug}/{slug}" : $"/kategori/{slug}"));
        ViewBag.Breadcrumbs = breadcrumbs;

        return View("Category");
    }

    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "Page", "SortBy", "MinPrice", "MaxPrice" })]
    public async Task<IActionResult> AllProducts([FromQuery] StorefrontCatalogQuery query)
    {
        var productsResult = await productService.GetStorefrontProductsAsync(query);
        var products = productsResult.Success ? productsResult.Data : EmptyPage();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewData["JsonLd"] = JsonLdBuilder.BuildCollectionPage(
            "Tum Urunler", null, $"{baseUrl}/urunler");

        ViewBag.Products = products;
        ViewBag.Query = query;
        ViewBag.PageTitle = "Tum Urunler";
        ViewBag.SeoTitle = $"Tum Urunler - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = $"{tenant.Settings.StoreName} urunleri";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new("Tum Urunler", "/urunler")
        };

        return View("Category");
    }

    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "q", "Page", "SortBy", "MinPrice", "MaxPrice" })]
    public async Task<IActionResult> Search([FromQuery(Name = "q")] string? q,
        [FromQuery] StorefrontCatalogQuery query)
    {
        ViewBag.SearchQuery = q;
        ViewBag.SeoTitle = string.IsNullOrWhiteSpace(q)
            ? $"Arama - {tenant.Settings.StoreName}"
            : $"\"{q}\" arama sonuclari - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = string.IsNullOrWhiteSpace(q)
            ? $"{tenant.Settings.StoreName} urun arama"
            : $"\"{q}\" icin arama sonuclari";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new("Arama", "/arama")
        };

        if (string.IsNullOrWhiteSpace(q))
        {
            ViewBag.Products = EmptyPage();
            ViewBag.Query = query;
            return View();
        }

        var searchQuery = query with { SearchQuery = q };
        var productsResult = await productService.GetStorefrontProductsAsync(searchQuery);
        var products = productsResult.Success ? productsResult.Data : EmptyPage();

        // Record search query (fire-and-forget)
        _ = searchHistoryManager.RecordSearchAsync(tenant.TenantId, q!);

        ViewBag.Products = products;
        ViewBag.Query = searchQuery;

        return View();
    }

    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "Page", "SortBy", "MinPrice", "MaxPrice" })]
    public async Task<IActionResult> Brand(string slug, [FromQuery] StorefrontCatalogQuery query)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var brandResult = await brandService.GetBrandBySeoSlugAsync(slug);
        if (!brandResult.Success || brandResult.Data is null)
            return NotFound();

        var brand = brandResult.Data;
        var filteredQuery = query with { BrandId = brand.Id };
        var productsResult = await productService.GetStorefrontProductsAsync(filteredQuery);
        var products = productsResult.Success ? productsResult.Data : EmptyPage();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewData["JsonLd"] = JsonLdBuilder.BuildCollectionPage(
            brand.Name, null, $"{baseUrl}/marka/{slug}");

        ViewBag.Products = products;
        ViewBag.Query = filteredQuery;
        ViewBag.Brand = brand;
        ViewBag.PageTitle = brand.Name;
        ViewBag.SeoTitle = $"{brand.Name} - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = $"{brand.Name} marka urunleri - {tenant.Settings.StoreName}";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new(brand.Name, $"/marka/{slug}")
        };

        return View("Category");
    }

    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> NewProducts()
    {
        var productsResult = await productService.GetNewProductsAsync(24);
        var items = productsResult.Success ? productsResult.Data : new List<StorefrontProductCardDto>();

        ViewBag.Products = new Entegrasyon.Entity.Pageable<StorefrontProductCardDto>(
            items, 1, items.Count == 0 ? 24 : items.Count, items.Count);
        ViewBag.Query = new StorefrontCatalogQuery();
        ViewBag.PageTitle = "Yeni Urunler";
        ViewBag.SeoTitle = $"Yeni Urunler - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = $"{tenant.Settings.StoreName} yeni eklenen urunler";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new("Yeni Urunler", "/yeni-urunler")
        };

        return View("Category");
    }

    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> BestSellers()
    {
        var productsResult = await productService.GetBestSellersAsync(24);
        var items = productsResult.Success ? productsResult.Data : new List<StorefrontProductCardDto>();

        ViewBag.Products = new Entegrasyon.Entity.Pageable<StorefrontProductCardDto>(
            items, 1, items.Count == 0 ? 24 : items.Count, items.Count);
        ViewBag.Query = new StorefrontCatalogQuery();
        ViewBag.PageTitle = "Cok Satanlar";
        ViewBag.SeoTitle = $"Cok Satanlar - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = $"{tenant.Settings.StoreName} en cok satan urunler";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new("Cok Satanlar", "/cok-satanlar")
        };

        return View("Category");
    }

    public async Task<IActionResult> SearchSuggest([FromQuery(Name = "q")] string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            var popularResult = await searchHistoryManager.GetPopularSearchesAsync(tenant.TenantId, 8);
            var popular = popularResult.Success ? popularResult.Data : [];
            return Json(popular.Select(s => new { text = s, url = $"/arama?q={Uri.EscapeDataString(s)}", type = "popular", imageUrl = (string?)null }));
        }

        var result = await productService.GetSearchSuggestionsAsync(q);
        var suggestions = result.Success ? result.Data : new List<StorefrontSearchSuggestionDto>();

        return Json(suggestions);
    }

    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> PopularSearches()
    {
        var result = await searchHistoryManager.GetPopularSearchesAsync(tenant.TenantId, 10);
        return Json(result.Success ? result.Data : new List<string>());
    }

    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> SellerStore(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var sellerResult = await sellerManager.GetSellerBySlugAsync(tenant.TenantId, slug);
        if (!sellerResult.Success || sellerResult.Data.Status != SellerStatus.Approved)
            return NotFound();

        var seller = sellerResult.Data;

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var sellerProducts = await dbContext.SellerProducts
            .Include(x => x.Product)
            .Where(x => x.SellerId == seller.Id && x.IsActive && x.Status == SellerProductStatus.Approved)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        ViewBag.Seller = seller;
        ViewBag.SellerProducts = sellerProducts;
        ViewBag.SeoTitle = $"{seller.StoreName} - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = seller.StoreDescription ?? $"{seller.StoreName} magaza sayfasi";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new(seller.StoreName, $"/magaza/{slug}")
        };

        return View("SellerStore");
    }

    private static Entegrasyon.Entity.Pageable<StorefrontProductCardDto> EmptyPage()
        => new(Array.Empty<StorefrontProductCardDto>(), 1, 24, 0);
}
