using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Mock Hepsiburada soru-cevap servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; başarılı sonuçlar döndürür.
/// </summary>
public sealed class MockHepsiburadaQnAService(
    ILogger<MockHepsiburadaQnAService> logger) : IHepsiburadaQnAService
{
    public Task<IDataResult<List<HepsiburadaQuestionDto>>> GetQuestionsAsync(string? status = null, int page = 0, int size = 50)
    {
        logger.LogInformation("[MOCK] HB get questions: Status={Status}, Page={Page}", status, page);
        return Task.FromResult<IDataResult<List<HepsiburadaQuestionDto>>>(
            new SuccessDataResult<List<HepsiburadaQuestionDto>>([]));
    }

    public Task<IDataResult<HepsiburadaQuestionDto>> GetQuestionAsync(string questionNumber)
    {
        logger.LogInformation("[MOCK] HB get question: {QuestionNumber}", questionNumber);
        return Task.FromResult<IDataResult<HepsiburadaQuestionDto>>(
            new ErrorDataResult<HepsiburadaQuestionDto>(null!, "Mock: soru bulunamadı."));
    }

    public Task<IResult> AnswerQuestionAsync(string questionNumber, string answer)
    {
        logger.LogInformation("[MOCK] HB answer question: {QuestionNumber}", questionNumber);
        return Task.FromResult<IResult>(new SuccessResult("Soru yanıtlandı (MOCK)."));
    }

    public Task<IResult> RejectQuestionAsync(string questionNumber)
    {
        logger.LogInformation("[MOCK] HB reject question: {QuestionNumber}", questionNumber);
        return Task.FromResult<IResult>(new SuccessResult("Soru reddedildi (MOCK)."));
    }
}
