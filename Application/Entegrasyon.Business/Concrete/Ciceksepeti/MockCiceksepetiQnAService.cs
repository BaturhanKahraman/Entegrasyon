using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti soru-cevap servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; boş başarılı sonuçlar döndürür.
/// </summary>
public sealed class MockCiceksepetiQnAService(
    ILogger<MockCiceksepetiQnAService> logger) : ICiceksepetiQnAService
{
    public Task<IDataResult<CiceksepetiQuestionListResponse>> GetQuestionsAsync(
        string? productCode = null, bool? answered = null, string? startDate = null,
        string? endDate = null, int page = 1, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti get questions: Page={Page}", page);
        var response = new CiceksepetiQuestionListResponse(Items: [], HasNextPage: false);
        return Task.FromResult<IDataResult<CiceksepetiQuestionListResponse>>(
            new SuccessDataResult<CiceksepetiQuestionListResponse>(response));
    }

    public Task<IResult> AnswerQuestionAsync(int questionId, CiceksepetiAnswerQuestionRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti answer question: QuestionId={QuestionId}", questionId);
        return Task.FromResult<IResult>(new SuccessResult("Soru yanıtlandı (MOCK)."));
    }

    public Task<IDataResult<CiceksepetiActionListResponse>> GetActionsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti get actions");
        var response = new CiceksepetiActionListResponse(Actions: []);
        return Task.FromResult<IDataResult<CiceksepetiActionListResponse>>(
            new SuccessDataResult<CiceksepetiActionListResponse>(response));
    }
}
