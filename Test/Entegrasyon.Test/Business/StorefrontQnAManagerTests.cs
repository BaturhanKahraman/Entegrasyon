using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.Entity.Storefront;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class StorefrontQnAManagerTests : BaseTest
{
    private readonly StorefrontQnAManager _sut;

    public StorefrontQnAManagerTests()
    {
        _sut = new StorefrontQnAManager(mockContextFactory.Object);
    }

    [Fact]
    public async Task AskQuestionAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        IList<StorefrontProductQuestion> questions = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontProductQuestions).ReturnsDbSet(questions);

        // Act
        var result = await _sut.AskQuestionAsync(1, Guid.NewGuid(), 10, "Bu urun kac cm?");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("basariyla");
    }

    [Fact]
    public async Task AskQuestionAsync_WithEmptyQuestion_ReturnsError()
    {
        // Act
        var result = await _sut.AskQuestionAsync(1, Guid.NewGuid(), 10, "");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task AskQuestionAsync_WithTooLongQuestion_ReturnsError()
    {
        // Arrange
        var longQuestion = new string('A', 1001);

        // Act
        var result = await _sut.AskQuestionAsync(1, Guid.NewGuid(), 10, longQuestion);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("1000");
    }

    [Fact]
    public async Task AnswerQuestionAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var question = new StorefrontProductQuestion
        {
            Id = 1, TenantId = 1, ProductId = Guid.NewGuid(),
            CustomerId = 10, QuestionText = "Test soru"
        };
        IList<StorefrontProductQuestion> questions = [question];
        mockIntegrationDbContext.Setup(c => c.StorefrontProductQuestions).ReturnsDbSet(questions);

        // Act
        var result = await _sut.AnswerQuestionAsync(1, "Cevap burada");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AnswerQuestionAsync_WithEmptyAnswer_ReturnsError()
    {
        // Act
        var result = await _sut.AnswerQuestionAsync(1, "");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AnswerQuestionAsync_WithNonExistentQuestion_ReturnsError()
    {
        // Arrange
        IList<StorefrontProductQuestion> questions = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontProductQuestions).ReturnsDbSet(questions);

        // Act
        var result = await _sut.AnswerQuestionAsync(999, "Cevap");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task GetProductQuestionsAsync_ReturnsPublishedQuestions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        IList<StorefrontProductQuestion> questions =
        [
            new() { Id = 1, TenantId = 1, ProductId = productId, QuestionText = "S1", IsPublished = true },
            new() { Id = 2, TenantId = 1, ProductId = productId, QuestionText = "S2", IsPublished = false },
            new() { Id = 3, TenantId = 1, ProductId = productId, QuestionText = "S3", IsPublished = true }
        ];
        mockIntegrationDbContext.Setup(c => c.StorefrontProductQuestions).ReturnsDbSet(questions);

        // Act
        var result = await _sut.GetProductQuestionsAsync(1, productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnansweredQuestionsAsync_ReturnsOnlyUnanswered()
    {
        // Arrange
        IList<StorefrontProductQuestion> questions =
        [
            new() { Id = 1, TenantId = 1, ProductId = Guid.NewGuid(), QuestionText = "S1", AnswerText = null },
            new() { Id = 2, TenantId = 1, ProductId = Guid.NewGuid(), QuestionText = "S2", AnswerText = "Cevap" },
            new() { Id = 3, TenantId = 1, ProductId = Guid.NewGuid(), QuestionText = "S3", AnswerText = null }
        ];
        mockIntegrationDbContext.Setup(c => c.StorefrontProductQuestions).ReturnsDbSet(questions);

        // Act
        var result = await _sut.GetUnansweredQuestionsAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task MarkHelpfulAsync_IncrementsCount()
    {
        // Arrange
        var question = new StorefrontProductQuestion
        {
            Id = 1, TenantId = 1, ProductId = Guid.NewGuid(),
            CustomerId = 10, QuestionText = "Test", HelpfulCount = 5
        };
        IList<StorefrontProductQuestion> questions = [question];
        mockIntegrationDbContext.Setup(c => c.StorefrontProductQuestions).ReturnsDbSet(questions);

        // Act
        var result = await _sut.MarkHelpfulAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        question.HelpfulCount.Should().Be(6);
    }
}
