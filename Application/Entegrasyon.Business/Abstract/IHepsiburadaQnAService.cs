using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Hepsiburada Q&amp;A (müşteri soruları) servisi.
/// Base URL: api-asktoseller-merchant[-sit].hepsiburada.com
/// </summary>
public interface IHepsiburadaQnAService
{
    Task<IDataResult<List<HepsiburadaQuestionDto>>> GetQuestionsAsync(string? status = null, int page = 0, int size = 50);
    Task<IDataResult<HepsiburadaQuestionDto>> GetQuestionAsync(string questionNumber);
    Task<IResult> AnswerQuestionAsync(string questionNumber, string answer);
    Task<IResult> RejectQuestionAsync(string questionNumber);
}
