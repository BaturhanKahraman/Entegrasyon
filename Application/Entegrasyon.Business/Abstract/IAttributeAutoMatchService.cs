using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAttributeAutoMatchService
{
    Task<IDataResult<List<AttributeMatchSuggestionDto>>> SuggestAttributeMatchesAsync(
        List<AppAttributeForMatchDto> appAttributes,
        List<MarketplaceAttributeDto> marketplaceAttributes,
        CancellationToken ct = default);

    Task<IDataResult<List<ValueMatchSuggestionDto>>> SuggestValueMatchesAsync(
        List<AppValueForMatchDto> appValues,
        List<MarketplaceAttributeValueDto> marketplaceValues,
        CancellationToken ct = default);

    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

public record AppAttributeForMatchDto(int Id, string Name);
public record AppValueForMatchDto(int Id, string Name);
