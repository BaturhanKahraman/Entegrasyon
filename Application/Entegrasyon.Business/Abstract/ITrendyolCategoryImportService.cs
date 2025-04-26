using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Shared.Results;
using System.Collections.Immutable;

namespace Entegrasyon.Business.Abstract
{
    public interface ITrendyolCategoryImportService
    {
        Task<IDataResult<IEnumerable<ImportedTrendyolCategory>>> GetTrendyolCategories();
        Task<IResult> Import(ImmutableList<TrendyolSelectedCategory> rootCategories);
        ValueTask QueueImporting(ImmutableList<TrendyolSelectedCategory> rootCategories);
    }
}