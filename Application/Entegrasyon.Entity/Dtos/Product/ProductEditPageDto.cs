using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductEditPageDto(
    ProductEditDetailDto Product,
    List<BrandListDetailDto> Brands,
    List<CategorySelectDto> LeafCategories,
    List<BranchSelectDto> BranchOffices,
    MarketplaceSyncStatusDto SyncStatus
);
