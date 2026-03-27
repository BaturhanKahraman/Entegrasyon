using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryAutoMatchService
{
    /// <summary>
    /// Verilen kategoriler icin LLM tabanli otomatik eslestirme onerileri uretir.
    /// Ollama cevrimdisiysa bos liste doner (graceful degradation).
    /// </summary>
    Task<IDataResult<List<CategoryAutoMatchSuggestionDto>>> GetAutoMatchSuggestionsAsync(
        CategoryAutoMatchRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Ollama servisinin erisilebilir olup olmadigini kontrol eder.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
