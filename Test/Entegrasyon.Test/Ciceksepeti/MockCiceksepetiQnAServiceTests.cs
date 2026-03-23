using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiQnAServiceTests
{
    private readonly Mock<ILogger<MockCiceksepetiQnAService>> _loggerMock = new();

    private MockCiceksepetiQnAService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task GetQuestionsAsync_Should_Return_Success_With_Empty_Items()
    {
        var sut = CreateSut();
        var result = await sut.GetQuestionsAsync();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AnswerQuestionAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiAnswerQuestionRequest("Test answer", 1, null, null);
        var result = await sut.AnswerQuestionAsync(42, request);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetActionsAsync_Should_Return_Success_With_Empty_Actions()
    {
        var sut = CreateSut();
        var result = await sut.GetActionsAsync();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Actions.Should().BeEmpty();
    }
}
