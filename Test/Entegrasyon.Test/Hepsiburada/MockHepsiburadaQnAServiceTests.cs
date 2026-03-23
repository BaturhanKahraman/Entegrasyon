using Entegrasyon.Business.Concrete.Hepsiburada;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Hepsiburada;

public class MockHepsiburadaQnAServiceTests
{
    private readonly Mock<ILogger<MockHepsiburadaQnAService>> _loggerMock = new();

    private MockHepsiburadaQnAService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task GetQuestionsAsync_Should_Return_Success_With_Empty_List()
    {
        var sut = CreateSut();
        var result = await sut.GetQuestionsAsync();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuestionAsync_Should_Return_Error_For_Mock()
    {
        var sut = CreateSut();
        var result = await sut.GetQuestionAsync("Q-001");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AnswerQuestionAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var result = await sut.AnswerQuestionAsync("Q-001", "Test cevabı");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RejectQuestionAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var result = await sut.RejectQuestionAsync("Q-001");
        result.Success.Should().BeTrue();
    }
}
