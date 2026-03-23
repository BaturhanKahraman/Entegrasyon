using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama soru-cevap servisi — mock implementasyon (test/geliştirme ortamı).
/// Gerçek API çağrısı yapmaz; örnek verilerle başarı döner.
/// </summary>
public sealed class MockPazaramaQnAService(
    ILogger<MockPazaramaQnAService> logger) : IPazaramaQnAService
{
    public Task<IDataResult<List<PazaramaQuestionStatusDto>>> GetQuestionStatusesAsync()
    {
        logger.LogInformation("MockPazarama: GetQuestionStatuses");

        var statuses = new List<PazaramaQuestionStatusDto>
        {
            new(0, "Cevap Bekliyor"),
            new(1, "Cevaplandı"),
            new(2, "Onay Bekliyor"),
            new(3, "Reddedildi")
        };

        return Task.FromResult<IDataResult<List<PazaramaQuestionStatusDto>>>(
            new SuccessDataResult<List<PazaramaQuestionStatusDto>>(statuses));
    }

    public Task<IDataResult<List<PazaramaQuestionTopicDto>>> GetQuestionTopicsAsync()
    {
        logger.LogInformation("MockPazarama: GetQuestionTopics");

        var topics = new List<PazaramaQuestionTopicDto>
        {
            new("2884d198-babd-4199-b90f-9a13bbee6ec9", "Renk Beden"),
            new("0ce396f6-9bb2-4a84-9448-ba0b16e3a6b8", "Teslimat ve Kargo"),
            new("f153675f-0da8-4bef-80fb-cb0c291fb172", "İptal ve İade"),
            new("cd6a0496-6b0f-471f-9d6f-eb53c3d0cb20", "Garanti Koşulları"),
            new("c7c20577-f962-4cfa-b171-ff802eeeb161", "Ürün Özellikleri")
        };

        return Task.FromResult<IDataResult<List<PazaramaQuestionTopicDto>>>(
            new SuccessDataResult<List<PazaramaQuestionTopicDto>>(topics));
    }

    public Task<IDataResult<PazaramaQuestionListData>> GetQuestionsAsync()
    {
        logger.LogInformation("MockPazarama: GetQuestions");

        var data = new PazaramaQuestionListData(
            SellerId: "mock-seller-id",
            ApprovalAnswersByMerchant: new List<PazaramaQuestionSummaryDto>(),
            UnAnsweredCount: 0,
            PageResponse: new PazaramaQnAPageResponse(1, 10, 0, 0));

        return Task.FromResult<IDataResult<PazaramaQuestionListData>>(
            new SuccessDataResult<PazaramaQuestionListData>(data));
    }

    public Task<IDataResult<PazaramaQuestionDetailDto>> GetQuestionByIdAsync(string questionId)
    {
        logger.LogInformation("MockPazarama: GetQuestionById QuestionId={QuestionId}", questionId);

        var detail = new PazaramaQuestionDetailDto(
            SellerId: "mock-seller-id",
            QuestionId: questionId,
            ProductName: "Mock Ürün",
            ProductImageUrl: null,
            Barcode: "MOCK-BARCODE",
            Brand: "Mock Marka",
            MaskedUserName: "M**** K****",
            Question: "Mock soru",
            QuestionDate: DateTimeOffset.UtcNow.ToString("o"),
            QuestionStatus: 0);

        return Task.FromResult<IDataResult<PazaramaQuestionDetailDto>>(
            new SuccessDataResult<PazaramaQuestionDetailDto>(detail));
    }

    public Task<IResult> AnswerQuestionAsync(string questionId, string text)
    {
        logger.LogInformation("MockPazarama: AnswerQuestion QuestionId={QuestionId}", questionId);
        return Task.FromResult<IResult>(new SuccessResult());
    }

    public Task<IDataResult<PazaramaQuestionSearchData>> SearchQuestionsAsync(PazaramaQuestionSearchRequest request)
    {
        logger.LogInformation("MockPazarama: SearchQuestions Page={Page}, Size={Size}",
            request.PageIndex, request.PageSize);

        var data = new PazaramaQuestionSearchData(
            SellerId: "mock-seller-id",
            ApprovalAnswersByMerchantSearchs: new List<PazaramaQuestionSearchItemDto>(),
            PageResponse: new PazaramaQnAPageResponse(request.PageIndex, request.PageSize, 0, 0));

        return Task.FromResult<IDataResult<PazaramaQuestionSearchData>>(
            new SuccessDataResult<PazaramaQuestionSearchData>(data));
    }
}
