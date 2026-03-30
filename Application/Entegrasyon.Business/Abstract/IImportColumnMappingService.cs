using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IImportColumnMappingService
{
    Task<List<ImportColumnProfile>> GetProfilesAsync(BulkOperationType type);
    Task<IResult> SaveProfileAsync(string name, BulkOperationType type, Dictionary<string, string> mappings);
    Task<IResult> DeleteProfileAsync(int id);
    Dictionary<string, string?> SuggestMappings(List<string> excelHeaders, BulkOperationType type);
    List<ImportSystemField> GetSystemFields(BulkOperationType type);
}

public record ImportSystemField(string Key, string DisplayName, bool IsRequired);
