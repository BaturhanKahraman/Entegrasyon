using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiQnAService unit testleri.
/// </summary>
public class CiceksepetiQnAServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ICiceksepetiApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<CiceksepetiQnAService>> _mockLogger = new();

    private CiceksepetiQnAService CreateSut() => new(
        _mockApiClient.Object,
        mockApplicationLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateQuestionListResponse(CiceksepetiQuestionListResponse payload, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateActionListResponse(CiceksepetiActionListResponse payload, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("", System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request", System.Text.Encoding.UTF8, "application/json") };

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestionsAsync_Uses1BasedPagination()
    {
        // Arrange — page=1 is the default (1-based)
        string? capturedUrl = null;

        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(CreateQuestionListResponse(new CiceksepetiQuestionListResponse(
                Items: new List<CiceksepetiQuestionDto>(),
                HasNextPage: false)));

        var sut = CreateSut();

        // Act — call with default page=1
        await sut.GetQuestionsAsync(page: 1);

        // Assert — URL must contain Page=1
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("Page=1");
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetQuestionsAsync_BuildsQueryStringCorrectly()
    {
        // Arrange
        string? capturedUrl = null;

        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(CreateQuestionListResponse(new CiceksepetiQuestionListResponse(
                Items: new List<CiceksepetiQuestionDto>(),
                HasNextPage: false)));

        var sut = CreateSut();

        // Act — call with all optional params
        await sut.GetQuestionsAsync(
            productCode: "FLOWER-001",
            answered: false,
            startDate: "2024-01-01",
            endDate: "2024-01-31",
            page: 2);

        // Assert — all provided params must appear in query string
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("Page=2");
        capturedUrl.Should().Contain("ProductCode=FLOWER-001");
        capturedUrl.Should().Contain("Answered=False");
        capturedUrl.Should().Contain("CreateStartDate=2024-01-01");
        capturedUrl.Should().Contain("CreateEndDate=2024-01-31");
    }

    // ── Test 3 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnswerQuestionAsync_CallsPutWithCorrectPath()
    {
        // Arrange
        var questionId = 42;

        _mockApiClient
            .Setup(x => x.PutAsync(
                It.Is<string>(u => u.Contains($"sellerquestions/{questionId}")),
                It.IsAny<CiceksepetiAnswerQuestionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse());

        var request = new CiceksepetiAnswerQuestionRequest(
            Answer: "Ürünümüz kargo ile gönderilmektedir.",
            BranchActionId: 1,
            BranchActionDetailId: null,
            BranchDescription: null);

        var sut = CreateSut();

        // Act
        var result = await sut.AnswerQuestionAsync(questionId, request);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(
            x => x.PutAsync(
                It.Is<string>(u => u.Contains($"sellerquestions/{questionId}")),
                It.IsAny<CiceksepetiAnswerQuestionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 4 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActionsAsync_ReturnsParsedActions()
    {
        // Arrange
        var apiResponse = new CiceksepetiActionListResponse(
            Actions: new List<CiceksepetiActionDto>
            {
                new(Id: 1, Name: "Yanıtla", Details: new List<CiceksepetiActionDetailDto>
                {
                    new(Id: 10, Name: "Standart Yanıt"),
                    new(Id: 11, Name: "Özel Yanıt")
                }),
                new(Id: 2, Name: "Yoksay", Details: new List<CiceksepetiActionDetailDto>())
            });

        _mockApiClient
            .Setup(x => x.GetAsync(
                It.Is<string>(u => u.Contains("sellerquestions/actions")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActionListResponse(apiResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetActionsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Actions.Should().HaveCount(2);
        result.Data.Actions[0].Name.Should().Be("Yanıtla");
        result.Data.Actions[0].Details.Should().HaveCount(2);
        result.Data.Actions[1].Name.Should().Be("Yoksay");
    }
}
