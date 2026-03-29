using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class TrendyolCategoryAttributeProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<TrendyolCategoryAttributeProvider> logger) : IMarketplaceCategoryAttributeProvider
{
    private const string AttributeUrlTemplate = "product/product-categories/{0}/attributes";

    public int MarketPlaceId => 1;

    public async Task<IDataResult<List<MarketplaceAttributeDto>>> GetAttributesForCategoryAsync(
        int marketplaceCategoryId, CancellationToken ct = default)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient(StringConstants.TrendyolApi);
            var url = string.Format(AttributeUrlTemplate, marketplaceCategoryId);

            var response = await httpClient.GetFromJsonAsync<TrendyolCategory>(url, ct);

            if (response is null)
            {
                logger.LogWarning("Trendyol kategori özellikleri boş döndü. CategoryId={CategoryId}", marketplaceCategoryId);
                return new ErrorDataResult<List<MarketplaceAttributeDto>>([], "Trendyol kategori özellikleri alınamadı.");
            }

            var attributes = response.categoryAttributes
                .Select(a => new MarketplaceAttributeDto(
                    Id: a.Attribute.Id,
                    Name: a.Attribute.Name,
                    IsRequired: a.Required,
                    AllowCustom: a.AllowCustom,
                    Values: a.AttributeValues
                        .Select(v => new MarketplaceAttributeValueDto(v.Id, v.Name))
                        .ToList()))
                .ToList();

            return new SuccessDataResult<List<MarketplaceAttributeDto>>(attributes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol kategori özellikleri alınırken hata oluştu. CategoryId={CategoryId}", marketplaceCategoryId);
            return new ErrorDataResult<List<MarketplaceAttributeDto>>([], "Trendyol kategori özellikleri alınırken hata oluştu.");
        }
    }
}
