namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontCatalogQuery(
    int? CategoryId = null, int? BrandId = null,
    string? SearchQuery = null,
    decimal? MinPrice = null, decimal? MaxPrice = null,
    string? SortBy = null, int Page = 1, int PageSize = 24);
