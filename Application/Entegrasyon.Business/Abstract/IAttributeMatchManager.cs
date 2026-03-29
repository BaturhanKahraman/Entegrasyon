using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAttributeMatchManager
{
    Task<List<CategoryAttributeMarketPlaceMatch>> GetAttributeMatchesAsync(int marketPlaceId, List<int> attributeIds);
    Task<List<CategoryAttributeValueMarketPlaceMatch>> GetValueMatchesAsync(int marketPlaceId, List<int> valueIds);
    Task<IResult> SaveAttributeMatchAsync(int applicationAttributeId, int marketPlaceId, int marketplaceAttributeId, string? externalId = null);
    Task<IResult> RemoveAttributeMatchAsync(int applicationAttributeId, int marketPlaceId);
    Task<IResult> SaveValueMatchAsync(int applicationValueId, int marketPlaceId, int marketplaceValueId, string? externalId = null);
    Task<IResult> RemoveValueMatchAsync(int applicationValueId, int marketPlaceId);
}
