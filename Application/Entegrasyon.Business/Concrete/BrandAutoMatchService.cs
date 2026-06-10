using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class BrandAutoMatchService(
    IBrandMatchService brandMatchService,
    IMarketplaceSearchService searchService,
    IHttpClientFactory httpClientFactory,
    ILogger<BrandAutoMatchService> logger) : IBrandAutoMatchService
{
    private const double AutoSaveThreshold = 0.8;
    private const string OllamaModel = "entegrasyon-coder";
    private const double Temperature = 0.1;

    public async Task<IDataResult<BrandAutoMatchResultDto>> AutoMatchAsync(
        int marketPlaceId, CancellationToken ct = default)
    {
        // 1. Load unmapped brands
        var unmapped = await brandMatchService.GetUnmappedBrandsAsync(marketPlaceId);
        if (unmapped.Count == 0)
            return new SuccessDataResult<BrandAutoMatchResultDto>(new BrandAutoMatchResultDto());

        // 2. Load all marketplace brands (empty query = all)
        var mpBrandsResult = await searchService.SearchBrandsAsync(marketPlaceId, "", ct);
        var mpBrands = mpBrandsResult.Success ? mpBrandsResult.Data ?? [] : [];

        // 3. Round 1: String matching
        var autoMatched = 0;
        var remaining = new List<BrandDto>();

        foreach (var brand in unmapped)
        {
            var match = FindStringMatch(brand.Name, mpBrands);
            if (match is not null)
            {
                var saved = await TrySaveMatchAsync(brand.Id, marketPlaceId, match.Id);
                if (saved) autoMatched++;
                else remaining.Add(brand); // save failed, try Ollama
            }
            else
            {
                remaining.Add(brand);
            }
        }

        // 4. Round 2: Ollama fallback for remaining
        var suggestions = new List<BrandAutoMatchSuggestionDto>();
        var failed = 0;

        if (remaining.Count > 0 && mpBrands.Count > 0)
        {
            var ollamaSuggestions = await CallOllamaAsync(remaining, mpBrands, ct);

            foreach (var suggestion in ollamaSuggestions)
            {
                if (suggestion.Confidence >= AutoSaveThreshold)
                {
                    var saved = await TrySaveMatchAsync(
                        suggestion.ApplicationBrandId, marketPlaceId, suggestion.MarketPlaceBrandId);
                    if (saved)
                    {
                        autoMatched++;
                        remaining.RemoveAll(b => b.Id == suggestion.ApplicationBrandId);
                    }
                    else
                    {
                        suggestions.Add(suggestion);
                    }
                }
                else
                {
                    suggestions.Add(suggestion);
                    remaining.RemoveAll(b => b.Id == suggestion.ApplicationBrandId);
                }
            }

            // Brands still in remaining after Ollama = truly unmatched
            failed = remaining.Count(b =>
                !suggestions.Any(s => s.ApplicationBrandId == b.Id));
        }
        else if (remaining.Count > 0)
        {
            failed = remaining.Count;
        }

        var resultDto = new BrandAutoMatchResultDto
        {
            AutoMatchedCount = autoMatched,
            SuggestionCount = suggestions.Count,
            FailedCount = failed,
            Suggestions = suggestions
        };

        return new SuccessDataResult<BrandAutoMatchResultDto>(resultDto);
    }

    public async Task<bool> IsOllamaAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ollama");
            var response = await client.GetAsync("", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ─── String Matching ───────────────────────────────────────────────────

    private static MarketplaceBrandSearchResult? FindStringMatch(
        string appBrandName, List<MarketplaceBrandSearchResult> mpBrands)
    {
        // Priority 1: Exact match (case-insensitive, trimmed)
        var exact = mpBrands.FirstOrDefault(m =>
            string.Equals(m.Name.Trim(), appBrandName.Trim(),
                StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // Priority 2: Normalized match (Turkish chars stripped)
        var normalizedApp = TurkishStringNormalizer.Normalize(appBrandName.Trim());
        var normalized = mpBrands.FirstOrDefault(m =>
            TurkishStringNormalizer.Normalize(m.Name.Trim()) == normalizedApp);
        if (normalized is not null) return normalized;

        // Priority 3: Contains match (either direction)
        var contains = mpBrands.FirstOrDefault(m =>
            m.Name.Contains(appBrandName, StringComparison.OrdinalIgnoreCase) ||
            appBrandName.Contains(m.Name, StringComparison.OrdinalIgnoreCase));
        return contains;
    }

    private async Task<bool> TrySaveMatchAsync(int appBrandId, int marketPlaceId, int mpBrandId)
    {
        try
        {
            var result = await brandMatchService.CreateBrandMappingAsync(new CreateBrandMarketPlaceMatchDto
            {
                ApplicationBrandId = appBrandId,
                MarketPlaceId = marketPlaceId,
                MarketPlaceBrandId = mpBrandId
            });
            return result.Success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Brand mapping kaydedilemedi: AppBrandId={AppBrandId}", appBrandId);
            return false;
        }
    }

    // ─── Ollama Fallback ───────────────────────────────────────────────────

    private async Task<List<BrandAutoMatchSuggestionDto>> CallOllamaAsync(
        List<BrandDto> appBrands,
        List<MarketplaceBrandSearchResult> mpBrands,
        CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ollama");

            var appJson = JsonSerializer.Serialize(appBrands.Select(b => new { id = b.Id, name = b.Name }));
            var mpJson = JsonSerializer.Serialize(mpBrands.Select(b => new { id = b.Id, name = b.Name }));
            var prompt = BuildPrompt(appJson, mpJson);

            var requestBody = new
            {
                model = OllamaModel,
                prompt,
                stream = false,
                options = new { temperature = Temperature }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("api/generate", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

            if (string.IsNullOrWhiteSpace(ollamaResponse?.Response)) return [];

            var ollamaResults = JsonSerializer.Deserialize<List<OllamaMatchResult>>(
                ollamaResponse.Response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            return ollamaResults
                .Select(r =>
                {
                    var appBrand = appBrands.FirstOrDefault(b => b.Id == r.AppId);
                    var mpBrand = mpBrands.FirstOrDefault(b => b.Id == r.MpId);
                    if (appBrand is null || mpBrand is null) return null;

                    return new BrandAutoMatchSuggestionDto
                    {
                        ApplicationBrandId = appBrand.Id,
                        ApplicationBrandName = appBrand.Name,
                        MarketPlaceBrandId = mpBrand.Id,
                        MarketPlaceBrandName = mpBrand.Name,
                        Confidence = r.Confidence,
                        Reason = r.Reason
                    };
                })
                .Where(s => s is not null)
                .Select(s => s!)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Ollama servisine baglanamadi, brand auto-match Ollama adimi atlaniyor.");
            return [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ollama yaniti parse edilemedi.");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ollama brand auto-match beklenmedik hata.");
            return [];
        }
    }

    private static string BuildPrompt(string appBrandsJson, string mpBrandsJson) => $$"""
        Sen bir e-ticaret marka eslestirme uzmanisis. Uygulama markalarini marketplace markalariyla isim benzerligine gore eslestir.

        Uygulama markalari:
        {{appBrandsJson}}

        Marketplace markalari:
        {{mpBrandsJson}}

        SADECE JSON array dondur, baska bir sey yazma. Format:
        [
          {"appId": 1, "mpId": 100, "confidence": 0.9, "reason": "Ayni marka farki isim varyanti"}
        ]

        confidence degeri 0-1 arasi olmali:
        - 0.8-1.0: Cok yuksek guven (ayni marka, sadece kucuk fark)
        - 0.5-0.8: Orta guven (muhtemelen ayni marka ama emin degilim)
        - 0.0-0.5: Düşük guven (tahmini eslestirme)

        Eslestirme bulunamazsa: []
        Her uygulama markasi icin en fazla 1 eslestirme yap.
        """;

    // ─── Private record types ──────────────────────────────────────────────

    private record OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; init; } = string.Empty;
    }

    private record OllamaMatchResult
    {
        [JsonPropertyName("appId")]
        public int AppId { get; init; }

        [JsonPropertyName("mpId")]
        public int MpId { get; init; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonPropertyName("reason")]
        public string Reason { get; init; } = string.Empty;
    }
}
