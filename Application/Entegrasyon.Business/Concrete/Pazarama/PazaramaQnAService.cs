using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama soru-cevap (Q&amp;A) servisi (gerçek API çağrıları).
/// GET /QuestionAnswer/getQuestionStatus
/// POST /QuestionAnswer/questionTopics
/// GET /QuestionAnswer/getApprovalAnswersByMerchant
/// GET /QuestionAnswer/getApprovalAnswerById?questionId={id}
/// PUT /QuestionAnswer/sellerAnswer
/// POST /QuestionAnswer/getApprovalAnswersByMerchantSearch
/// </summary>
public sealed class PazaramaQnAService(
    IPazaramaApiClient apiClient,
    ILogger<PazaramaQnAService> logger) : IPazaramaQnAService
{
    public async Task<IDataResult<List<PazaramaQuestionStatusDto>>> GetQuestionStatusesAsync()
    {
        try
        {
            logger.LogInformation("Pazarama GetQuestionStatuses başlatıldı.");

            var response = await apiClient.GetAsync("QuestionAnswer/getQuestionStatus");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama GetQuestionStatuses başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PazaramaQuestionStatusDto>>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<List<PazaramaQuestionStatusDto>>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Soru statüleri alınamadı";
                logger.LogWarning("Pazarama GetQuestionStatuses yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<List<PazaramaQuestionStatusDto>>(null!, msg);
            }

            var statuses = parsed.Data ?? new List<PazaramaQuestionStatusDto>();
            logger.LogInformation("Pazarama GetQuestionStatuses başarılı. Statü sayısı: {Count}", statuses.Count);
            return new SuccessDataResult<List<PazaramaQuestionStatusDto>>(statuses);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama GetQuestionStatuses exception.");
            return new ErrorDataResult<List<PazaramaQuestionStatusDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<List<PazaramaQuestionTopicDto>>> GetQuestionTopicsAsync()
    {
        try
        {
            logger.LogInformation("Pazarama GetQuestionTopics başlatıldı.");

            var response = await apiClient.PostAsync("QuestionAnswer/questionTopics", new { });

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama GetQuestionTopics başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<List<PazaramaQuestionTopicDto>>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<List<PazaramaQuestionTopicDto>>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Soru konuları alınamadı";
                logger.LogWarning("Pazarama GetQuestionTopics yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<List<PazaramaQuestionTopicDto>>(null!, msg);
            }

            var topics = parsed.Data ?? new List<PazaramaQuestionTopicDto>();
            logger.LogInformation("Pazarama GetQuestionTopics başarılı. Konu sayısı: {Count}", topics.Count);
            return new SuccessDataResult<List<PazaramaQuestionTopicDto>>(topics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama GetQuestionTopics exception.");
            return new ErrorDataResult<List<PazaramaQuestionTopicDto>>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<PazaramaQuestionListData>> GetQuestionsAsync()
    {
        try
        {
            logger.LogInformation("Pazarama GetQuestions başlatıldı.");

            var response = await apiClient.GetAsync("QuestionAnswer/getApprovalAnswersByMerchant");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama GetQuestions başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PazaramaQuestionListData>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<PazaramaQuestionListData>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Sorular alınamadı";
                logger.LogWarning("Pazarama GetQuestions yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<PazaramaQuestionListData>(null!, msg);
            }

            var data = parsed.Data ?? new PazaramaQuestionListData(null, null, 0, null);
            logger.LogInformation("Pazarama GetQuestions başarılı. Soru sayısı: {Count}",
                data.ApprovalAnswersByMerchant?.Count ?? 0);
            return new SuccessDataResult<PazaramaQuestionListData>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama GetQuestions exception.");
            return new ErrorDataResult<PazaramaQuestionListData>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<PazaramaQuestionDetailDto>> GetQuestionByIdAsync(string questionId)
    {
        try
        {
            logger.LogInformation("Pazarama GetQuestionById başlatıldı. QuestionId: {QuestionId}", questionId);

            var response = await apiClient.GetAsync($"QuestionAnswer/getApprovalAnswerById?questionId={questionId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama GetQuestionById başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PazaramaQuestionDetailDto>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<PazaramaQuestionDetailDto>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Soru detayı alınamadı";
                logger.LogWarning("Pazarama GetQuestionById yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<PazaramaQuestionDetailDto>(null!, msg);
            }

            logger.LogInformation("Pazarama GetQuestionById başarılı. QuestionId: {QuestionId}", questionId);
            return new SuccessDataResult<PazaramaQuestionDetailDto>(parsed.Data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama GetQuestionById exception. QuestionId: {QuestionId}", questionId);
            return new ErrorDataResult<PazaramaQuestionDetailDto>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> AnswerQuestionAsync(string questionId, string text)
    {
        try
        {
            logger.LogInformation("Pazarama AnswerQuestion başlatıldı. QuestionId: {QuestionId}", questionId);

            var request = new PazaramaAnswerRequest(questionId, text);
            var response = await apiClient.PutAsync("QuestionAnswer/sellerAnswer", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama AnswerQuestion başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<object>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Soru yanıtlanamadı";
                logger.LogWarning("Pazarama AnswerQuestion yanıt başarısız: {Message}", msg);
                return new ErrorResult(msg);
            }

            logger.LogInformation("Pazarama AnswerQuestion başarılı. QuestionId: {QuestionId}", questionId);
            return new SuccessResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama AnswerQuestion exception. QuestionId: {QuestionId}", questionId);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<PazaramaQuestionSearchData>> SearchQuestionsAsync(PazaramaQuestionSearchRequest request)
    {
        try
        {
            logger.LogInformation("Pazarama SearchQuestions başlatıldı. Page: {Page}, Size: {Size}",
                request.PageIndex, request.PageSize);

            var response = await apiClient.PostAsync("QuestionAnswer/getApprovalAnswersByMerchantSearch", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Pazarama SearchQuestions başarısız: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<PazaramaQuestionSearchData>(null!, $"API hatası: {response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<PazaramaResponse<PazaramaQuestionSearchData>>();

            if (parsed?.Success != true)
            {
                var msg = parsed?.Message ?? "Soru araması başarısız";
                logger.LogWarning("Pazarama SearchQuestions yanıt başarısız: {Message}", msg);
                return new ErrorDataResult<PazaramaQuestionSearchData>(null!, msg);
            }

            var data = parsed.Data ?? new PazaramaQuestionSearchData(null, null, null);
            logger.LogInformation("Pazarama SearchQuestions başarılı. Sonuç sayısı: {Count}",
                data.ApprovalAnswersByMerchantSearchs?.Count ?? 0);
            return new SuccessDataResult<PazaramaQuestionSearchData>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazarama SearchQuestions exception.");
            return new ErrorDataResult<PazaramaQuestionSearchData>(null!, $"Hata: {ex.Message}");
        }
    }
}
