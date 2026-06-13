namespace Entegrasyon.Entity.Dtos.Marketplace;

public record MarketplaceCategorySearchResult(int Id, string Name, string? FullPath, bool IsLeaf = false);
public record MarketplaceBrandSearchResult(int Id, string Name);
public record MarketplaceAttributeSearchResult(int Id, string Name);
public record MarketplaceOption(int Id, string Name);
