using System.Net.Http.Json;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti soru-cevap (Q&amp;A) servisi.
/// Soru listeleme, yanıtlama ve branch action listesi işlemlerini kapsar.
/// </summary>
public sealed class CiceksepetiQnAService(
    ICiceksepetiApiClient apiClient,
    IApplicationLogManager applicationLogManager,
    ILogger<CiceksepetiQnAService> logger) : ICiceksepetiQnAService
{
    private const string QuestionsBaseEndpoint = "sellerquestions";
    private const string ActionsEndpoint = "sellerquestions/actions";

    public async Task<IDataResult<CiceksepetiQuestionListResponse>> GetQuestionsAsync(
        string? productCode = null,
        bool? answered = null,
        string? startDate = null,
        string? endDate = null,
        int page = 1,
        CancellationToken ct = default)
    {
        try
        {
            // Build query string — page is 1-based; only include non-null optional params
            var queryBuilder = new StringBuilder($"{QuestionsBaseEndpoint}?Page={page}");

            if (!string.IsNullOrWhiteSpace(productCode))
                queryBuilder.Append($"&ProductCode={productCode}");

            if (answered.HasValue)
                queryBuilder.Append($"&Answered={answered.Value}");

            if (!string.IsNullOrWhiteSpace(startDate))
                queryBuilder.Append($"&CreateStartDate={startDate}");

            if (!string.IsNullOrWhiteSpace(endDate))
                queryBuilder.Append($"&CreateEndDate={endDate}");

            var endpoint = queryBuilder.ToString();

            logger.LogInformation(
                "CiceksepetiQnAService: Sorular isteniyor. Page={Page}, ProductCode={ProductCode}",
                page, productCode);

            var response = await apiClient.GetAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"Soru listesi alınamadı. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiQnAService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Marketplace, LogAction.List, new { page, productCode }, ct);
                return new ErrorDataResult<CiceksepetiQuestionListResponse>(null!, errorMsg);
            }

            var parsed = await response.Content.ReadFromJsonAsync<CiceksepetiQuestionListResponse>(
                cancellationToken: ct);

            if (parsed is null)
            {
                const string parseError = "API yanıtı soru listesi içermiyor.";
                logger.LogError("CiceksepetiQnAService: {Message}", parseError);
                return new ErrorDataResult<CiceksepetiQuestionListResponse>(null!, parseError);
            }

            logger.LogInformation(
                "CiceksepetiQnAService: {Count} soru alındı.",
                parsed.Items.Count);

            return new SuccessDataResult<CiceksepetiQuestionListResponse>(parsed,
                $"{parsed.Items.Count} soru başarıyla alındı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiQnAService: Soru listesi alınamadı.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti soru listesi başarısız: {ex.Message}",
                LogType.Marketplace, LogAction.List, new { page, productCode }, ct);
            return new ErrorDataResult<CiceksepetiQuestionListResponse>(null!, ex.Message);
        }
    }

    public async Task<IResult> AnswerQuestionAsync(
        int questionId,
        CiceksepetiAnswerQuestionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var endpoint = $"{QuestionsBaseEndpoint}/{questionId}";

            logger.LogInformation(
                "CiceksepetiQnAService: Soru yanıtlanıyor. QuestionId={QuestionId}",
                questionId);

            var response = await apiClient.PutAsync(endpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"Soru yanıtlanamadı. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiQnAService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Marketplace, LogAction.Update, new { questionId, request }, ct);
                return new ErrorResult(errorMsg);
            }

            logger.LogInformation("CiceksepetiQnAService: Soru başarıyla yanıtlandı. QuestionId={QuestionId}", questionId);
            return new SuccessResult("Soru başarıyla yanıtlandı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiQnAService: Soru yanıtlanamadı. QuestionId={QuestionId}", questionId);
            await applicationLogManager.AddLog(
                $"Çiçeksepeti soru yanıtlama başarısız: {ex.Message}",
                LogType.Marketplace, LogAction.Update, new { questionId, request }, ct);
            return new ErrorResult(ex.Message);
        }
    }

    public async Task<IDataResult<CiceksepetiActionListResponse>> GetActionsAsync(
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("CiceksepetiQnAService: Branch aksiyonları isteniyor.");

            var response = await apiClient.GetAsync(ActionsEndpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var errorMsg = $"Aksiyon listesi alınamadı. HTTP {(int)response.StatusCode}: {body}";
                logger.LogError("CiceksepetiQnAService: {Message}", errorMsg);
                await applicationLogManager.AddLog(
                    errorMsg, LogType.Marketplace, LogAction.List, new { }, ct);
                return new ErrorDataResult<CiceksepetiActionListResponse>(null!, errorMsg);
            }

            var parsed = await response.Content.ReadFromJsonAsync<CiceksepetiActionListResponse>(
                cancellationToken: ct);

            if (parsed is null)
            {
                const string parseError = "API yanıtı aksiyon listesi içermiyor.";
                logger.LogError("CiceksepetiQnAService: {Message}", parseError);
                return new ErrorDataResult<CiceksepetiActionListResponse>(null!, parseError);
            }

            logger.LogInformation(
                "CiceksepetiQnAService: {Count} aksiyon alındı.",
                parsed.Actions.Count);

            return new SuccessDataResult<CiceksepetiActionListResponse>(parsed,
                $"{parsed.Actions.Count} aksiyon başarıyla alındı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CiceksepetiQnAService: Aksiyon listesi alınamadı.");
            await applicationLogManager.AddLog(
                $"Çiçeksepeti aksiyon listesi başarısız: {ex.Message}",
                LogType.Marketplace, LogAction.List, new { }, ct);
            return new ErrorDataResult<CiceksepetiActionListResponse>(null!, ex.Message);
        }
    }
}
