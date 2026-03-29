using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class CategoryAutoMatchService(
    IHttpClientFactory httpClientFactory,
    IMarketplaceSearchService searchService,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<CategoryAutoMatchService> logger) : ICategoryAutoMatchService
{
    private const int BatchSize = 50;
    private const string OllamaModel = "entegrasyon-coder";
    private const double Temperature = 0.1;

    public async Task<IDataResult<List<CategoryAutoMatchSuggestionDto>>> GetAutoMatchSuggestionsAsync(
        CategoryAutoMatchRequestDto request, CancellationToken ct = default)
    {
        if (request.Categories.Count == 0)
            return new SuccessDataResult<List<CategoryAutoMatchSuggestionDto>>([], "Kategori listesi bos.");

        // Leaf guard — only suggest matches for leaf categories
        using var dbContext = contextFactory.CreateDbContext();
        var requestedIds = request.Categories.Select(c => c.CategoryId).ToList();
        var nonLeafIdsList = await dbContext.Categories
            .Where(c => !c.IsDeleted && requestedIds.Contains(c.SuperCategoryId ?? 0))
            .Select(c => c.SuperCategoryId!.Value)
            .Distinct()
            .ToListAsync(ct);
        var nonLeafIds = nonLeafIdsList.ToHashSet();

        var filteredCategories = request.Categories
            .Where(c => !nonLeafIds.Contains(c.CategoryId))
            .ToList();

        if (filteredCategories.Count == 0)
            return new SuccessDataResult<List<CategoryAutoMatchSuggestionDto>>([], "Eşleştirilebilecek yaprak kategori bulunamadı.");

        request = request with { Categories = filteredCategories };

        var allSuggestions = new List<CategoryAutoMatchSuggestionDto>();

        // Batch processing (max 50 per request)
        var batches = request.Categories
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / BatchSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            var batchSuggestions = await ProcessBatchAsync(batch, request.MarketPlaceId, ct);
            allSuggestions.AddRange(batchSuggestions);
        }

        return new SuccessDataResult<List<CategoryAutoMatchSuggestionDto>>(allSuggestions);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ollama");
            var response = await client.GetAsync("", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<List<CategoryAutoMatchSuggestionDto>> ProcessBatchAsync(
        List<CategoryAutoMatchItemDto> batch, int marketPlaceId, CancellationToken ct)
    {
        try
        {
            var ollamaResults = await CallOllamaAsync(batch, ct);
            if (ollamaResults.Count == 0)
                return [];

            var suggestions = new List<CategoryAutoMatchSuggestionDto>();

            foreach (var ollamaResult in ollamaResults)
            {
                var category = batch.FirstOrDefault(c => c.CategoryId == ollamaResult.CategoryId);
                if (category is null) continue;

                // Resolve the suggested name to a real marketplace category ID
                var searchResult = await searchService.SearchCategoriesAsync(
                    marketPlaceId, ollamaResult.SuggestedName, ct);

                if (!searchResult.Success || searchResult.Data.Count == 0)
                    continue;

                var bestMatch = searchResult.Data[0];
                suggestions.Add(new CategoryAutoMatchSuggestionDto
                {
                    ApplicationCategoryId = ollamaResult.CategoryId,
                    ApplicationCategoryName = category.CategoryName,
                    SuggestedMarketPlaceCategoryId = bestMatch.Id,
                    SuggestedMarketPlaceCategoryName = bestMatch.FullPath ?? bestMatch.Name,
                    Confidence = ollamaResult.Confidence,
                    Reason = ollamaResult.Reason
                });
            }

            return suggestions;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Ollama servisine baglanilamadi, auto-match devre disi.");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auto-match isleminde beklenmedik hata.");
            return [];
        }
    }

    private async Task<List<OllamaSuggestionResult>> CallOllamaAsync(
        List<CategoryAutoMatchItemDto> categories, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Ollama");

        var categoriesJson = JsonSerializer.Serialize(categories.Select(c => new
        {
            id = c.CategoryId,
            name = c.CategoryName,
            parent = c.ParentCategoryName
        }));

        var prompt = BuildPrompt(categoriesJson);

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

        if (string.IsNullOrWhiteSpace(ollamaResponse?.Response))
            return [];

        try
        {
            var results = JsonSerializer.Deserialize<List<OllamaSuggestionResult>>(
                ollamaResponse.Response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return results ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ollama yaniti JSON olarak parse edilemedi: {Response}",
                ollamaResponse.Response[..Math.Min(200, ollamaResponse.Response.Length)]);
            return [];
        }
    }

    private static string BuildPrompt(string categoriesJson)
    {
        return $$"""
            Sen bir e-ticaret kategori eslestirme uzmanisis. Asagidaki uygulama kategorilerini marketplace kategorileriyle eslestir.

            Her kategori icin en uygun marketplace kategori adini oner. JSON formatinda cevap ver.

            Ornekler:
            - "Cep Telefonu" -> "Elektronik > Cep Telefonu & Aksesuar > Cep Telefonu"
            - "Tisort" -> "Giyim & Aksesuar > Erkek Giyim > Tisort"
            - "Laptop" -> "Elektronik > Bilgisayar & Tablet > Laptop"

            Kategoriler:
            {{categoriesJson}}

            SADECE JSON array dondur, baska bir sey yazma. Format:
            [
              {"categoryId": 1, "suggestedName": "Elektronik > Telefon", "confidence": 0.85, "reason": "Isim benzerligi yuksek"}
            ]

            confidence degeri 0-1 arasi olmali:
            - 0.8-1.0: Cok yuksek guven (direkt eslesen isimler)
            - 0.5-0.8: Orta guven (benzer ama farkli isimler)
            - 0.0-0.5: Dusuk guven (tahmini eslestirme)
            """;
    }

    private record OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; init; } = string.Empty;
    }

    private record OllamaSuggestionResult
    {
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; init; }

        [JsonPropertyName("suggestedName")]
        public string SuggestedName { get; init; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonPropertyName("reason")]
        public string Reason { get; init; } = string.Empty;
    }
}
