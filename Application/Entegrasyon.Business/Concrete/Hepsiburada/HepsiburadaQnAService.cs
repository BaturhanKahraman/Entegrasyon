using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Hepsiburada Q&amp;A servisi. MerchantId header'da gönderilir.
/// </summary>
public sealed class HepsiburadaQnAService(
    IHepsiburadaApiClient apiClient,
    ILogger<HepsiburadaQnAService> logger) : IHepsiburadaQnAService
{
    public async Task<IDataResult<List<HepsiburadaQuestionDto>>> GetQuestionsAsync(
        string? status = null, int page = 0, int size = 50)
    {
        try
        {
            var url = $"/api/v1.0/issues?page={page}&size={size}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={status}";

            var response = await apiClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<List<HepsiburadaQuestionDto>>(null, $"Q&A API hatası: {response.StatusCode}");

            var questions = await response.Content.ReadFromJsonAsync<List<HepsiburadaQuestionDto>>();
            return new SuccessDataResult<List<HepsiburadaQuestionDto>>(questions ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB Q&A list failed");
            return new ErrorDataResult<List<HepsiburadaQuestionDto>>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<HepsiburadaQuestionDto>> GetQuestionAsync(string questionNumber)
    {
        try
        {
            var response = await apiClient.GetAsync($"/api/v1.0/issues/{questionNumber}");
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<HepsiburadaQuestionDto>(null, $"Soru bulunamadı: {response.StatusCode}");

            var question = await response.Content.ReadFromJsonAsync<HepsiburadaQuestionDto>();
            return new SuccessDataResult<HepsiburadaQuestionDto>(question);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB Q&A get failed: {Number}", questionNumber);
            return new ErrorDataResult<HepsiburadaQuestionDto>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> AnswerQuestionAsync(string questionNumber, string answer)
    {
        try
        {
            var request = new HepsiburadaAnswerRequest(answer);
            var response = await apiClient.PostAsync($"/api/v1.0/issues/{questionNumber}/answer", request);

            if (!response.IsSuccessStatusCode)
                return new ErrorResult($"Cevap gönderilemedi: {response.StatusCode}");

            logger.LogInformation("HB question answered: {Number}", questionNumber);
            return new SuccessResult("Soru cevaplandı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB Q&A answer failed: {Number}", questionNumber);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> RejectQuestionAsync(string questionNumber)
    {
        try
        {
            var response = await apiClient.PostAsync($"/api/v1.0/issues/{questionNumber}/reject", new { });

            if (!response.IsSuccessStatusCode)
                return new ErrorResult($"Sorun bildirme hatası: {response.StatusCode}");

            return new SuccessResult("Sorun bildirildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB Q&A reject failed: {Number}", questionNumber);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
