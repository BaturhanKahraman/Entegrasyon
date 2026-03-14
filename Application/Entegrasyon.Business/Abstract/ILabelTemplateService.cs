using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Labels;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ILabelTemplateService
{
    Task<IDataResult<List<LabelTemplateDto>>> GetAllAsync();
    Task<IDataResult<LabelTemplateDto>> GetByIdAsync(Guid id);
    Task<IDataResult<LabelTemplateDto>> GetDefaultAsync(LabelType type);
    Task<IDataResult<LabelTemplateDto>> SaveAsync(SaveLabelTemplateDto dto);
    Task<IResult> DeleteAsync(Guid id);
    Task<IResult> SetAsDefaultAsync(Guid id);
}
