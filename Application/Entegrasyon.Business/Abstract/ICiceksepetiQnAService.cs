using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;
namespace Entegrasyon.Business.Abstract;
public interface ICiceksepetiQnAService
{
    Task<IDataResult<CiceksepetiQuestionListResponse>> GetQuestionsAsync(string? productCode = null, bool? answered = null, string? startDate = null, string? endDate = null, int page = 1, CancellationToken ct = default);
    Task<IResult> AnswerQuestionAsync(int questionId, CiceksepetiAnswerQuestionRequest request, CancellationToken ct = default);
    Task<IDataResult<CiceksepetiActionListResponse>> GetActionsAsync(CancellationToken ct = default);
}
