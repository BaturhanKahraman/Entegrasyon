using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class CategoryAttributeManagerTests : BaseTest
{
    private readonly CategoryAttributeManager _sut;

    public CategoryAttributeManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<AddCategoryAttributeDto>()))
            .Returns(Task.CompletedTask);
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<EditCategoryAttributeDto>()))
            .Returns(Task.CompletedTask);

        _sut = new CategoryAttributeManager(
            mockApplicationLogger.Object,
            MockValidator.Object,
            new CategoryAttributeMapper(),
            mockContextFactory.Object,
            NullLogger<CategoryAttributeManager>.Instance);
    }

    private static AddCategoryAttributeDto BuildAddDto(
        string key = "Renk",
        string humanized = "Renk",
        params string[] valueNames)
    {
        var values = (valueNames.Length == 0 ? ["Sarı"] : valueNames)
            .Select(n => new CategoryAttributeValue { Name = n })
            .ToList();
        return new AddCategoryAttributeDto(0, false, false, key, false, humanized, values);
    }

    // ──────────────────────────────────────────────────────────────────
    // AddCategoryAttribute
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddCategoryAttribute_ShouldFail_WhenKeyAlreadyExists()
    {
        // Arrange — mevcut "renk" anahtarı; dto " Renk " (trim + case-insensitive çakışma)
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes)
            .ReturnsDbSet([new CategoryAttribute { Id = 5, CategoryAttributeKey = "renk" }]);

        var dto = BuildAddDto(key: " Renk ", humanized: "Renk");

        // Act
        var result = await _sut.AddCategoryAttribute(dto);

        // Assert
        result.Success.Should().BeFalse();
        mockIntegrationDbContext.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCategoryAttribute_ShouldNormalizeAndDedupeValues()
    {
        // Arrange — "Sarı" / "  SARI " kanonik olarak aynı → tek değer kalmalı
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes)
            .ReturnsDbSet(new List<CategoryAttribute>());

        CategoryAttribute? captured = null;
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes.Add(It.IsAny<CategoryAttribute>()))
            .Callback<CategoryAttribute>(a => captured = a);

        var dto = BuildAddDto(valueNames: ["Sarı", "  SARI ", "Kırmızı"]);

        // Act
        var result = await _sut.AddCategoryAttribute(dto);

        // Assert
        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.CategoryAttributeValues.Should().HaveCount(2);
        captured.CategoryAttributeValues.Should().OnlyContain(v => !string.IsNullOrEmpty(v.NormalizedName));
        captured.CategoryAttributeValues.Select(v => v.Name).Should().Contain("Sarı").And.Contain("Kırmızı");
    }

    [Fact]
    public async Task AddCategoryAttribute_ShouldTrimKeyAndHumanized_AndForceIdZero()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes)
            .ReturnsDbSet(new List<CategoryAttribute>());

        CategoryAttribute? captured = null;
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes.Add(It.IsAny<CategoryAttribute>()))
            .Callback<CategoryAttribute>(a => captured = a);

        var dto = new AddCategoryAttributeDto(42, false, false, " Beden ", false, " Beden ",
            [new CategoryAttributeValue { Name = "M" }]);

        // Act
        var result = await _sut.AddCategoryAttribute(dto);

        // Assert
        result.Success.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Id.Should().Be(0);
        captured.CategoryAttributeKey.Should().Be("Beden");
        captured.CategoryAttributeHumanized.Should().Be("Beden");
    }

    [Fact]
    public async Task AddCategoryAttribute_ShouldDropWhitespaceOnlyValues()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes)
            .ReturnsDbSet(new List<CategoryAttribute>());

        CategoryAttribute? captured = null;
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes.Add(It.IsAny<CategoryAttribute>()))
            .Callback<CategoryAttribute>(a => captured = a);

        var dto = BuildAddDto(valueNames: ["Sarı", "   "]);

        // Act
        await _sut.AddCategoryAttribute(dto);

        // Assert
        captured.Should().NotBeNull();
        captured!.CategoryAttributeValues.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddCategoryAttribute_ShouldWriteApplicationLog()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes)
            .ReturnsDbSet(new List<CategoryAttribute>());

        // Act
        var result = await _sut.AddCategoryAttribute(BuildAddDto());

        // Assert
        result.Success.Should().BeTrue();
        mockApplicationLogger.Verify(
            x => x.AddLog(It.IsAny<string>(), It.IsAny<LogType>(), LogAction.Add, It.IsAny<object>(), CancellationToken.None),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // UpdateCategoryAttribute — toAdd yolunda NormalizedName set edilmeli
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategoryAttribute_ShouldPersistRenamedExistingValues()
    {
        // Arrange — mevcut değer "Sari", kullanıcı inline "Sarı" olarak düzeltiyor (Id aynı)
        var attr = new CategoryAttribute
        {
            Id = 1,
            CategoryAttributeKey = "renk",
            CategoryAttributeHumanized = "Renk",
            CategoryAttributeValues =
                [new CategoryAttributeValue { Id = 10, Name = "Sari", NormalizedName = "SARI", CategoryAttributeId = 1 }]
        };
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes)
            .ReturnsDbSet([attr]);
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValues)
            .ReturnsDbSet(new List<CategoryAttributeValue>());

        var dto = new EditCategoryAttributeDto(
            Id: 1,
            IsRequired: false,
            IsVarianter: false,
            CategoryAttributeKey: "renk",
            IsSlicer: false,
            CategoryAttributeHumanized: "Renk",
            CategoryAttributeValues: [new CategoryAttributeValue { Id = 10, Name = "Sarı" }],
            CategoryId: 0);

        // Act
        var result = await _sut.UpdateCategoryAttribute(dto);

        // Assert — rename track edilen entity'ye yazılmalı (sessiz no-op = veri kaybı)
        result.Success.Should().BeTrue();
        var kept = attr.CategoryAttributeValues.Single(v => v.Id == 10);
        kept.Name.Should().Be("Sarı");
        kept.NormalizedName.Should().Be("SARI");
    }

    [Fact]
    public async Task UpdateCategoryAttribute_ShouldSetNormalizedNameOnNewValues()
    {
        // Arrange — mevcut özellik, yeni değer ekleniyor
        var attr = new CategoryAttribute
        {
            Id = 1,
            CategoryAttributeKey = "renk",
            CategoryAttributeHumanized = "Renk",
            CategoryAttributeValues = []
        };
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributes)
            .ReturnsDbSet([attr]);
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValues)
            .ReturnsDbSet(new List<CategoryAttributeValue>());

        var dto = new EditCategoryAttributeDto(
            Id: 1,
            IsRequired: false,
            IsVarianter: false,
            CategoryAttributeKey: "renk",
            IsSlicer: false,
            CategoryAttributeHumanized: "Renk",
            CategoryAttributeValues: [new CategoryAttributeValue { Id = 0, Name = "  lacivert mavi " }],
            CategoryId: 0);

        // Act
        var result = await _sut.UpdateCategoryAttribute(dto);

        // Assert
        result.Success.Should().BeTrue();
        attr.CategoryAttributeValues.Should().ContainSingle()
            .Which.NormalizedName.Should().Be("LACİVERT MAVİ");
    }
}
