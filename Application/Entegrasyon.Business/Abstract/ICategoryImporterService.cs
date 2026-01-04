using Entegrasyon.Entity.Categories;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Kategori import işlemleri için genel interface.
/// Tüm marketplace'ler bu interface'i implement eder.
/// </summary>
public interface ICategoryImporterService
{
    /// <summary>
    /// Import kaynağını döndürür
    /// </summary>
    ImportSource Source { get; }

    /// <summary>
    /// Marketplace'den kategorileri çeker
    /// </summary>
    Task<IDataResult<IEnumerable<ExternalCategoryDto>>> GetExternalCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seçilen kategorileri import eder
    /// </summary>
    Task<IResult> ImportCategoriesAsync(IEnumerable<ExternalCategoryImportRequest> categories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir kategoriyi import eder
    /// </summary>
    Task<IResult> ImportCategoryAsync(ExternalCategoryImportRequest category, CancellationToken cancellationToken = default);
}

/// <summary>
/// Harici sistemden gelen kategori bilgisi
/// </summary>
public record ExternalCategoryDto
{
    public string ExternalId { get; init; }
    public string Name { get; init; }
    public string? ParentExternalId { get; init; }
    public bool HasChildren { get; init; }
    public List<ExternalCategoryDto> Children { get; init; } = new();
}

/// <summary>
/// Import isteği için kullanılan DTO
/// </summary>
public record ExternalCategoryImportRequest
{
    public string ExternalId { get; init; }
    public string Name { get; init; }
    public string? ParentExternalId { get; init; }
    public List<ExternalCategoryImportRequest> Children { get; init; } = new();
    public bool IsLeaf => Children.Count == 0;
}
