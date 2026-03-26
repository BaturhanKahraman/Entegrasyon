using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontQnAManager
{
    Task<IDataResult<List<StorefrontProductQuestion>>> GetProductQuestionsAsync(int tenantId, Guid productId);
    Task<IResult> AskQuestionAsync(int tenantId, Guid productId, int customerId, string question);
    Task<IResult> AnswerQuestionAsync(int questionId, string answer);
    Task<IDataResult<List<StorefrontProductQuestion>>> GetUnansweredQuestionsAsync(int tenantId);
    Task<IResult> MarkHelpfulAsync(int questionId);
}
