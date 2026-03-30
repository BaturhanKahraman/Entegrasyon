using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class AttributeAutoMatchService(
    IHttpClientFactory httpClientFactory,
    ILogger<AttributeAutoMatchService> logger) : IAttributeAutoMatchService
{
    private const string OllamaModel = "entegrasyon-coder";
    private const double Temperature = 0.1;

    public async Task<IDataResult<List<AttributeMatchSuggestionDto>>> SuggestAttributeMatchesAsync(
        List<AppAttributeForMatchDto> appAttributes,
        List<MarketplaceAttributeDto> marketplaceAttributes,
        CancellationToken ct = default)
    {
        if (appAttributes.Count == 0)
            return new SuccessDataResult<List<AttributeMatchSuggestionDto>>([], "Özellik listesi boş.");

        try
        {
            var ollamaResults = await CallOllamaForAttributesAsync(appAttributes, marketplaceAttributes, ct);

            var suggestions = new List<AttributeMatchSuggestionDto>();

            foreach (var result in ollamaResults)
            {
                var appAttr = appAttributes.FirstOrDefault(a => a.Id == result.AppId);
                if (appAttr is null) continue;

                var mpAttr = marketplaceAttributes.FirstOrDefault(a => a.Id == result.MpId);
                if (mpAttr is null) continue;

                suggestions.Add(new AttributeMatchSuggestionDto(
                    appAttr.Id,
                    appAttr.Name,
                    mpAttr.Id,
                    mpAttr.Name,
                    result.Confidence));
            }

            return new SuccessDataResult<List<AttributeMatchSuggestionDto>>(suggestions);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Ollama servisine bağlanılamadı, attribute auto-match devre dışı.");
            return new SuccessDataResult<List<AttributeMatchSuggestionDto>>([]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Attribute auto-match işleminde beklenmedik hata.");
            return new SuccessDataResult<List<AttributeMatchSuggestionDto>>([]);
        }
    }

    public async Task<IDataResult<List<ValueMatchSuggestionDto>>> SuggestValueMatchesAsync(
        List<AppValueForMatchDto> appValues,
        List<MarketplaceAttributeValueDto> marketplaceValues,
        CancellationToken ct = default)
    {
        if (appValues.Count == 0)
            return new SuccessDataResult<List<ValueMatchSuggestionDto>>([], "Değer listesi boş.");

        try
        {
            var ollamaResults = await CallOllamaForValuesAsync(appValues, marketplaceValues, ct);

            var suggestions = new List<ValueMatchSuggestionDto>();

            foreach (var result in ollamaResults)
            {
                var appVal = appValues.FirstOrDefault(v => v.Id == result.AppId);
                if (appVal is null) continue;

                var mpVal = marketplaceValues.FirstOrDefault(v => v.Id == result.MpId);
                if (mpVal is null) continue;

                suggestions.Add(new ValueMatchSuggestionDto(
                    appVal.Id,
                    appVal.Name,
                    mpVal.Id,
                    mpVal.Name,
                    result.Confidence));
            }

            return new SuccessDataResult<List<ValueMatchSuggestionDto>>(suggestions);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Ollama servisine bağlanılamadı, value auto-match devre dışı.");
            return new SuccessDataResult<List<ValueMatchSuggestionDto>>([]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Value auto-match işleminde beklenmedik hata.");
            return new SuccessDataResult<List<ValueMatchSuggestionDto>>([]);
        }
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

    private async Task<List<OllamaMatchResult>> CallOllamaForAttributesAsync(
        List<AppAttributeForMatchDto> appAttributes,
        List<MarketplaceAttributeDto> marketplaceAttributes,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Ollama");

        var appJson = JsonSerializer.Serialize(appAttributes.Select(a => new { id = a.Id, name = a.Name }));
        var mpJson = JsonSerializer.Serialize(marketplaceAttributes.Select(a => new { id = a.Id, name = a.Name, isRequired = a.IsRequired, allowCustom = a.AllowCustom }));

        var prompt = BuildAttributePrompt(appJson, mpJson);

        return await SendOllamaRequestAsync(client, prompt, ct);
    }

    private async Task<List<OllamaMatchResult>> CallOllamaForValuesAsync(
        List<AppValueForMatchDto> appValues,
        List<MarketplaceAttributeValueDto> marketplaceValues,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Ollama");

        var appJson = JsonSerializer.Serialize(appValues.Select(v => new { id = v.Id, name = v.Name }));
        var mpJson = JsonSerializer.Serialize(marketplaceValues.Select(v => new { id = v.Id, name = v.Name }));

        var prompt = BuildValuePrompt(appJson, mpJson);

        return await SendOllamaRequestAsync(client, prompt, ct);
    }

    private async Task<List<OllamaMatchResult>> SendOllamaRequestAsync(
        HttpClient client, string prompt, CancellationToken ct)
    {
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
            var results = JsonSerializer.Deserialize<List<OllamaMatchResult>>(
                ollamaResponse.Response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return results ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ollama yanıtı JSON olarak parse edilemedi: {Response}",
                ollamaResponse.Response[..Math.Min(200, ollamaResponse.Response.Length)]);
            return [];
        }
    }

    private static string BuildAttributePrompt(string appJson, string mpJson)
    {
        return $$"""
            Sen bir e-ticaret özellik eşleştirme uzmanısın. Uygulama özelliklerini marketplace özellikleriyle isim benzerliğine göre eşleştir.

            Uygulama özellikleri:
            {{appJson}}

            Marketplace özellikleri:
            {{mpJson}}

            SADECE JSON array döndür, başka bir şey yazma. Format:
            [
              {"appId": 1, "mpId": 101, "confidence": 0.9}
            ]

            confidence değeri 0-1 arası olmalı:
            - 0.8-1.0: Çok yüksek güven (tam veya çok benzer isimler)
            - 0.5-0.8: Orta güven (benzer ama farklı isimler)
            - 0.0-0.5: Düşük güven (tahmini eşleştirme)

            Eşleşme bulunamazsa boş array döndür: []
            """;
    }

    private static string BuildValuePrompt(string appJson, string mpJson)
    {
        return $$"""
            Sen bir e-ticaret özellik değeri eşleştirme uzmanısın. Uygulama değerlerini marketplace değerleriyle isim benzerliğine göre eşleştir.

            Uygulama değerleri:
            {{appJson}}

            Marketplace değerleri:
            {{mpJson}}

            SADECE JSON array döndür, başka bir şey yazma. Format:
            [
              {"appId": 10, "mpId": 200, "confidence": 0.85}
            ]

            confidence değeri 0-1 arası olmalı:
            - 0.8-1.0: Çok yüksek güven (tam veya çok benzer isimler)
            - 0.5-0.8: Orta güven (benzer ama farklı isimler)
            - 0.0-0.5: Düşük güven (tahmini eşleştirme)

            Eşleşme bulunamazsa boş array döndür: []
            """;
    }

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
    }
}
