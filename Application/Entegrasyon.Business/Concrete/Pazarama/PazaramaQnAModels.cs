using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

// -----------------------------------------------------------------------
// QnA — Soru-Cevap modelleri
// -----------------------------------------------------------------------

/// <summary>Soru statüsü.</summary>
public sealed record PazaramaQuestionStatusDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("value")] string Value);

/// <summary>Soru konusu (topic).</summary>
public sealed record PazaramaQuestionTopicDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("topic")] string Topic);

/// <summary>
/// Soru listesi özet item (getApprovalAnswersByMerchant).
/// </summary>
public sealed record PazaramaQuestionSummaryDto(
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("questionId")] string QuestionId,
    [property: JsonPropertyName("questionDate")] string? QuestionDate,
    [property: JsonPropertyName("questionStatus")] int QuestionStatus);

/// <summary>
/// Soru listesi özet data wrapper.
/// </summary>
public sealed record PazaramaQuestionListData(
    [property: JsonPropertyName("sellerId")] string? SellerId,
    [property: JsonPropertyName("approvalAnswersByMerchant")] List<PazaramaQuestionSummaryDto>? ApprovalAnswersByMerchant,
    [property: JsonPropertyName("unAnsweredCount")] int UnAnsweredCount,
    [property: JsonPropertyName("pageResponse")] PazaramaQnAPageResponse? PageResponse);

/// <summary>
/// Soru detayı (getApprovalAnswerById).
/// </summary>
public sealed record PazaramaQuestionDetailDto(
    [property: JsonPropertyName("sellerId")] string? SellerId,
    [property: JsonPropertyName("questionId")] string QuestionId,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("productImageUrl")] string? ProductImageUrl,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("brand")] string? Brand,
    [property: JsonPropertyName("maskedUserName")] string? MaskedUserName,
    [property: JsonPropertyName("question")] string? Question,
    [property: JsonPropertyName("questionDate")] string? QuestionDate,
    [property: JsonPropertyName("questionStatus")] int QuestionStatus);

/// <summary>
/// Soruya cevap verme isteği.
/// PUT /QuestionAnswer/sellerAnswer
/// </summary>
public sealed record PazaramaAnswerRequest(
    [property: JsonPropertyName("questionId")] string QuestionId,
    [property: JsonPropertyName("text")] string Text);

/// <summary>
/// Soru filtreleme isteği.
/// POST /QuestionAnswer/getApprovalAnswersByMerchantSearch
/// </summary>
public sealed record PazaramaQuestionSearchRequest(
    [property: JsonPropertyName("barcode")] string? Barcode = null,
    [property: JsonPropertyName("topicId")] string? TopicId = null,
    [property: JsonPropertyName("questionStartDate")] string? QuestionStartDate = null,
    [property: JsonPropertyName("questionEndDate")] string? QuestionEndDate = null,
    [property: JsonPropertyName("questionStatus")] int? QuestionStatus = null,
    [property: JsonPropertyName("pageIndex")] int PageIndex = 1,
    [property: JsonPropertyName("pageSize")] int PageSize = 10);

/// <summary>
/// Soru arama sonucu detaylı item.
/// </summary>
public sealed record PazaramaQuestionSearchItemDto(
    [property: JsonPropertyName("questionId")] string QuestionId,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("productImageUrl")] string? ProductImageUrl,
    [property: JsonPropertyName("barcode")] string? Barcode,
    [property: JsonPropertyName("brand")] string? Brand,
    [property: JsonPropertyName("maskedUserName")] string? MaskedUserName,
    [property: JsonPropertyName("question")] string? Question,
    [property: JsonPropertyName("questionDate")] string? QuestionDate,
    [property: JsonPropertyName("answer")] string? Answer,
    [property: JsonPropertyName("answerDate")] string? AnswerDate,
    [property: JsonPropertyName("questionStatus")] int QuestionStatus,
    [property: JsonPropertyName("topicId")] string? TopicId);

/// <summary>
/// Soru arama data wrapper.
/// </summary>
public sealed record PazaramaQuestionSearchData(
    [property: JsonPropertyName("sellerId")] string? SellerId,
    [property: JsonPropertyName("approvalAnswersByMerchantSearchs")] List<PazaramaQuestionSearchItemDto>? ApprovalAnswersByMerchantSearchs,
    [property: JsonPropertyName("pageResponse")] PazaramaQnAPageResponse? PageResponse);

/// <summary>
/// QnA sayfalama bilgisi.
/// </summary>
public sealed record PazaramaQnAPageResponse(
    [property: JsonPropertyName("pageIndex")] int PageIndex,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("totalPages")] int TotalPages);
