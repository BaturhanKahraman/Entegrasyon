using System.Net;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

/// <summary>
/// PazaramaQnAService ve MockPazaramaQnAService birim testleri.
/// Soru-cevap işlemlerinin doğru endpoint'lere gönderildiğini doğrular.
/// </summary>
public class PazaramaQnAServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HttpResponseMessage BuildSuccessResponse<T>(T data)
    {
        var body = JsonSerializer.Serialize(new { data, success = true });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildSuccessEmptyResponse()
    {
        var body = JsonSerializer.Serialize(new { data = (object?)null, success = true });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildErrorHttpResponse(HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent("{\"success\":false,\"message\":\"hata\"}", Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildApiFailureResponse(string message = "İşlem başarısız")
    {
        var body = JsonSerializer.Serialize(new { data = (object?)null, success = false, message });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    // -----------------------------------------------------------------------
    // Real service setup
    // -----------------------------------------------------------------------

    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaQnAService>> _loggerMock = new();

    private PazaramaQnAService CreateSut() => new(_apiClientMock.Object, _loggerMock.Object);

    // -----------------------------------------------------------------------
    // GetQuestionStatusesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetQuestionStatusesAsync_WhenApiSucceeds_ShouldReturnStatuses()
    {
        var statuses = new List<PazaramaQuestionStatusDto>
        {
            new(0, "Cevap Bekliyor"),
            new(1, "Cevaplandı"),
            new(2, "Onay Bekliyor"),
            new(3, "Reddedildi")
        };

        _apiClientMock
            .Setup(a => a.GetAsync("QuestionAnswer/getQuestionStatus"))
            .ReturnsAsync(BuildSuccessResponse(statuses));

        var sut = CreateSut();
        var result = await sut.GetQuestionStatusesAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(4);
        result.Data![0].Id.Should().Be(0);
        result.Data![0].Value.Should().Be("Cevap Bekliyor");
    }

    [Fact]
    public async Task GetQuestionStatusesAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.GetAsync("QuestionAnswer/getQuestionStatus"))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.InternalServerError));

        var sut = CreateSut();
        var result = await sut.GetQuestionStatusesAsync();

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // GetQuestionTopicsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetQuestionTopicsAsync_WhenApiSucceeds_ShouldReturnTopics()
    {
        var topics = new List<PazaramaQuestionTopicDto>
        {
            new("2884d198-babd-4199-b90f-9a13bbee6ec9", "Renk Beden"),
            new("0ce396f6-9bb2-4a84-9448-ba0b16e3a6b8", "Teslimat ve Kargo")
        };

        _apiClientMock
            .Setup(a => a.PostAsync("QuestionAnswer/questionTopics", It.IsAny<object>()))
            .ReturnsAsync(BuildSuccessResponse(topics));

        var sut = CreateSut();
        var result = await sut.GetQuestionTopicsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Topic.Should().Be("Renk Beden");
    }

    [Fact]
    public async Task GetQuestionTopicsAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("QuestionAnswer/questionTopics", It.IsAny<object>()))
            .ReturnsAsync(BuildErrorHttpResponse());

        var sut = CreateSut();
        var result = await sut.GetQuestionTopicsAsync();

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // GetQuestionsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetQuestionsAsync_WhenApiSucceeds_ShouldReturnQuestionList()
    {
        var data = new PazaramaQuestionListData(
            SellerId: "SELLER-1",
            ApprovalAnswersByMerchant: new List<PazaramaQuestionSummaryDto>
            {
                new("Tükenmez Kalem", "Q-1", "2023-12-28T09:12:16.333", 0)
            },
            UnAnsweredCount: 1,
            PageResponse: new PazaramaQnAPageResponse(1, 10, 1, 1));

        _apiClientMock
            .Setup(a => a.GetAsync("QuestionAnswer/getApprovalAnswersByMerchant"))
            .ReturnsAsync(BuildSuccessResponse(data));

        var sut = CreateSut();
        var result = await sut.GetQuestionsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ApprovalAnswersByMerchant.Should().HaveCount(1);
        result.Data.UnAnsweredCount.Should().Be(1);
    }

    [Fact]
    public async Task GetQuestionsAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.GetAsync("QuestionAnswer/getApprovalAnswersByMerchant"))
            .ReturnsAsync(BuildErrorHttpResponse());

        var sut = CreateSut();
        var result = await sut.GetQuestionsAsync();

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // GetQuestionByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetQuestionByIdAsync_WhenApiSucceeds_ShouldReturnQuestionDetail()
    {
        var detail = new PazaramaQuestionDetailDto(
            SellerId: "SELLER-1",
            QuestionId: "Q-1",
            ProductName: "Tükenmez Kalem",
            ProductImageUrl: "https://cdn.pazarama.com/image.jpeg",
            Barcode: "MSTK55550",
            Brand: "4 Element Yayınları",
            MaskedUserName: "M**** S****",
            Question: "garanti var mı",
            QuestionDate: "2023-12-28T09:12:16.333",
            QuestionStatus: 0);

        _apiClientMock
            .Setup(a => a.GetAsync("QuestionAnswer/getApprovalAnswerById?questionId=Q-1"))
            .ReturnsAsync(BuildSuccessResponse(detail));

        var sut = CreateSut();
        var result = await sut.GetQuestionByIdAsync("Q-1");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.QuestionId.Should().Be("Q-1");
        result.Data.ProductName.Should().Be("Tükenmez Kalem");
        result.Data.Question.Should().Be("garanti var mı");
    }

    [Fact]
    public async Task GetQuestionByIdAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.GetAsync("QuestionAnswer/getApprovalAnswerById?questionId=Q-NOT-FOUND"))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.NotFound));

        var sut = CreateSut();
        var result = await sut.GetQuestionByIdAsync("Q-NOT-FOUND");

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // AnswerQuestionAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AnswerQuestionAsync_WhenApiSucceeds_ShouldReturnSuccess()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("QuestionAnswer/sellerAnswer", It.IsAny<PazaramaAnswerRequest>()))
            .ReturnsAsync(BuildSuccessEmptyResponse());

        var sut = CreateSut();
        var result = await sut.AnswerQuestionAsync("Q-1", "Evet, garanti kapsamındadır.");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AnswerQuestionAsync_ShouldCallPutWithCorrectBody()
    {
        PazaramaAnswerRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PutAsync("QuestionAnswer/sellerAnswer", It.IsAny<PazaramaAnswerRequest>()))
            .Callback<string, PazaramaAnswerRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessEmptyResponse());

        var sut = CreateSut();
        await sut.AnswerQuestionAsync("Q-123", "Cevap metni");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.QuestionId.Should().Be("Q-123");
        capturedRequest.Text.Should().Be("Cevap metni");
    }

    [Fact]
    public async Task AnswerQuestionAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("QuestionAnswer/sellerAnswer", It.IsAny<PazaramaAnswerRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse());

        var sut = CreateSut();
        var result = await sut.AnswerQuestionAsync("Q-1", "Cevap");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AnswerQuestionAsync_WhenApiReturnsSuccessFalse_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PutAsync("QuestionAnswer/sellerAnswer", It.IsAny<PazaramaAnswerRequest>()))
            .ReturnsAsync(BuildApiFailureResponse("Soru yanıtlanamadı"));

        var sut = CreateSut();
        var result = await sut.AnswerQuestionAsync("Q-1", "Cevap");

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Soru yanıtlanamadı");
    }

    // -----------------------------------------------------------------------
    // SearchQuestionsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SearchQuestionsAsync_WhenApiSucceeds_ShouldReturnSearchResults()
    {
        var data = new PazaramaQuestionSearchData(
            SellerId: "SELLER-1",
            ApprovalAnswersByMerchantSearchs: new List<PazaramaQuestionSearchItemDto>
            {
                new(
                    QuestionId: "Q-1",
                    ProductName: "Tükenmez Kalem",
                    ProductImageUrl: "https://cdn.pazarama.com/image.jpeg",
                    Barcode: "MSTK55550",
                    Brand: "4 Element Yayınları",
                    MaskedUserName: "M**** S****",
                    Question: "garanti var mı",
                    QuestionDate: "2023-12-28T09:12:16.333",
                    Answer: "Evet, garanti kapsamındadır.",
                    AnswerDate: "2023-12-28T10:17:13.023",
                    QuestionStatus: 2,
                    TopicId: "cd6a0496-6b0f-471f-9d6f-eb53c3d0cb20")
            },
            PageResponse: new PazaramaQnAPageResponse(1, 10, 1, 1));

        _apiClientMock
            .Setup(a => a.PostAsync("QuestionAnswer/getApprovalAnswersByMerchantSearch",
                It.IsAny<PazaramaQuestionSearchRequest>()))
            .ReturnsAsync(BuildSuccessResponse(data));

        var sut = CreateSut();
        var request = new PazaramaQuestionSearchRequest(Barcode: "MSTK55550", PageIndex: 1, PageSize: 10);

        var result = await sut.SearchQuestionsAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ApprovalAnswersByMerchantSearchs.Should().HaveCount(1);
        result.Data.ApprovalAnswersByMerchantSearchs![0].Barcode.Should().Be("MSTK55550");
    }

    [Fact]
    public async Task SearchQuestionsAsync_ShouldPassRequestBodyToApi()
    {
        PazaramaQuestionSearchRequest? capturedRequest = null;

        _apiClientMock
            .Setup(a => a.PostAsync("QuestionAnswer/getApprovalAnswersByMerchantSearch",
                It.IsAny<PazaramaQuestionSearchRequest>()))
            .Callback<string, PazaramaQuestionSearchRequest>((_, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse(new PazaramaQuestionSearchData(null, null, null)));

        var sut = CreateSut();
        var request = new PazaramaQuestionSearchRequest(
            Barcode: "TEST123",
            TopicId: "TOPIC-1",
            QuestionStatus: 0,
            PageIndex: 2,
            PageSize: 20);

        await sut.SearchQuestionsAsync(request);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Barcode.Should().Be("TEST123");
        capturedRequest.TopicId.Should().Be("TOPIC-1");
        capturedRequest.QuestionStatus.Should().Be(0);
        capturedRequest.PageIndex.Should().Be(2);
        capturedRequest.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task SearchQuestionsAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        _apiClientMock
            .Setup(a => a.PostAsync("QuestionAnswer/getApprovalAnswersByMerchantSearch",
                It.IsAny<PazaramaQuestionSearchRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse());

        var sut = CreateSut();
        var request = new PazaramaQuestionSearchRequest();

        var result = await sut.SearchQuestionsAsync(request);

        result.Success.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Mock service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<ILogger<MockPazaramaQnAService>> _mockLoggerMock = new();

    private MockPazaramaQnAService CreateMockSut() => new(_mockLoggerMock.Object);

    [Fact]
    public async Task MockService_GetQuestionStatusesAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var result = await sut.GetQuestionStatusesAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task MockService_GetQuestionTopicsAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var result = await sut.GetQuestionTopicsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task MockService_GetQuestionsAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var result = await sut.GetQuestionsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task MockService_GetQuestionByIdAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var result = await sut.GetQuestionByIdAsync("Q-1");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task MockService_AnswerQuestionAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var result = await sut.AnswerQuestionAsync("Q-1", "Cevap metni");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task MockService_SearchQuestionsAsync_ShouldReturnSuccess()
    {
        var sut = CreateMockSut();
        var request = new PazaramaQuestionSearchRequest();
        var result = await sut.SearchQuestionsAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }
}
