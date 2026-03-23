using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaQnAService
{
    /// <summary>
    /// Soru statülerini getirir.
    /// GET /QuestionAnswer/getQuestionStatus
    /// </summary>
    Task<IDataResult<List<PazaramaQuestionStatusDto>>> GetQuestionStatusesAsync();

    /// <summary>
    /// Soru konularını getirir.
    /// POST /QuestionAnswer/questionTopics
    /// </summary>
    Task<IDataResult<List<PazaramaQuestionTopicDto>>> GetQuestionTopicsAsync();

    /// <summary>
    /// Soruları özet olarak listeler.
    /// GET /QuestionAnswer/getApprovalAnswersByMerchant
    /// </summary>
    Task<IDataResult<PazaramaQuestionListData>> GetQuestionsAsync();

    /// <summary>
    /// Soru detayını getirir.
    /// GET /QuestionAnswer/getApprovalAnswerById?questionId={id}
    /// </summary>
    Task<IDataResult<PazaramaQuestionDetailDto>> GetQuestionByIdAsync(string questionId);

    /// <summary>
    /// Soruya cevap verir.
    /// PUT /QuestionAnswer/sellerAnswer
    /// </summary>
    Task<IResult> AnswerQuestionAsync(string questionId, string text);

    /// <summary>
    /// Soruları filtreler.
    /// POST /QuestionAnswer/getApprovalAnswersByMerchantSearch
    /// </summary>
    Task<IDataResult<PazaramaQuestionSearchData>> SearchQuestionsAsync(PazaramaQuestionSearchRequest request);
}
