using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Test.Hepsiburada;

/// <summary>
/// HepsiburadaQnAService unit testleri.
/// Soru listeleme, detay, cevaplama ve reddetme islemleri test edilir.
/// </summary>
public class HepsiburadaQnAServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHepsiburadaApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<HepsiburadaQnAService>> _mockLogger = new();

    private HepsiburadaQnAService CreateSut() => new(
        _mockApiClient.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("", System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request", System.Text.Encoding.UTF8, "application/json") };

    // ── Test 1: GetQuestionsAsync — success ─────────────────────────────────

    [Fact]
    public async Task GetQuestionsAsync_Success_ReturnsParsedQuestions()
    {
        var questions = new List<HepsiburadaQuestionDto>
        {
            new("Q-001", "Urun ne zaman gelir?", "Test Urun", "HB-SKU-1", "WAITING", "2024-01-10"),
            new("Q-002", "Renk secenegi var mi?", "Test Urun 2", "HB-SKU-2", "ANSWERED", "2024-01-11")
        };

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("/api/v1.0/issues"))))
            .ReturnsAsync(CreateJsonResponse(questions));

        var sut = CreateSut();
        var result = await sut.GetQuestionsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Number.Should().Be("Q-001");
        result.Data[1].Status.Should().Be("ANSWERED");
    }

    // ── Test 2: GetQuestionsAsync — with status and pagination params ───────

    [Fact]
    public async Task GetQuestionsAsync_WithParams_BuildsCorrectUrl()
    {
        string? capturedUrl = null;

        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(new List<HepsiburadaQuestionDto>()));

        var sut = CreateSut();
        await sut.GetQuestionsAsync(status: "WAITING", page: 2, size: 25);

        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("page=2");
        capturedUrl.Should().Contain("size=25");
        capturedUrl.Should().Contain("status=WAITING");
    }

    // ── Test 3: GetQuestionsAsync — API error ───────────────────────────────

    [Fact]
    public async Task GetQuestionsAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.GetQuestionsAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Q&A API hatası");
    }

    // ── Test 4: GetQuestionAsync — success ──────────────────────────────────

    [Fact]
    public async Task GetQuestionAsync_Success_ReturnsQuestion()
    {
        var question = new HepsiburadaQuestionDto(
            "Q-001", "Urun ne zaman gelir?", "Test Urun", "HB-SKU-1", "WAITING", "2024-01-10");

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("/api/v1.0/issues/Q-001"))))
            .ReturnsAsync(CreateJsonResponse(question));

        var sut = CreateSut();
        var result = await sut.GetQuestionAsync("Q-001");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Number.Should().Be("Q-001");
        result.Data.Question.Should().Be("Urun ne zaman gelir?");
    }

    // ── Test 5: GetQuestionAsync — API error ────────────────────────────────

    [Fact]
    public async Task GetQuestionAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("")
            });

        var sut = CreateSut();
        var result = await sut.GetQuestionAsync("NONEXISTENT");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Soru bulunamadı");
    }

    // ── Test 6: AnswerQuestionAsync — success ───────────────────────────────

    [Fact]
    public async Task AnswerQuestionAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/api/v1.0/issues/Q-001/answer")),
                It.IsAny<HepsiburadaAnswerRequest>()))
            .ReturnsAsync(OkResponse());

        var sut = CreateSut();
        var result = await sut.AnswerQuestionAsync("Q-001", "Urun 3-5 is gunu icerisinde gonderilir.");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Soru cevaplandı");
    }

    // ── Test 7: AnswerQuestionAsync — API error ─────────────────────────────

    [Fact]
    public async Task AnswerQuestionAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HepsiburadaAnswerRequest>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.AnswerQuestionAsync("Q-999", "Cevap");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cevap gönderilemedi");
    }

    // ── Test 8: RejectQuestionAsync — success ───────────────────────────────

    [Fact]
    public async Task RejectQuestionAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/api/v1.0/issues/Q-001/reject")),
                It.IsAny<object>()))
            .ReturnsAsync(OkResponse());

        var sut = CreateSut();
        var result = await sut.RejectQuestionAsync("Q-001");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Sorun bildirildi");
    }

    // ── Test 9: RejectQuestionAsync — API error ─────────────────────────────

    [Fact]
    public async Task RejectQuestionAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.RejectQuestionAsync("Q-999");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Sorun bildirme hatası");
    }

    // ── Test 10: GetQuestionsAsync — exception handling ─────────────────────

    [Fact]
    public async Task GetQuestionsAsync_Exception_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("DNS resolution failed"));

        var sut = CreateSut();
        var result = await sut.GetQuestionsAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("DNS resolution failed");
    }

    // ── Test 11: AnswerQuestionAsync — exception handling ───────────────────

    [Fact]
    public async Task AnswerQuestionAsync_Exception_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HepsiburadaAnswerRequest>()))
            .ThrowsAsync(new TimeoutException("Request timed out"));

        var sut = CreateSut();
        var result = await sut.AnswerQuestionAsync("Q-001", "Cevap");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Request timed out");
    }
}
