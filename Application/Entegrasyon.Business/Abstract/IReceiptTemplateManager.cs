using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Results;
using Microsoft.AspNetCore.Http;

namespace Entegrasyon.Business.Abstract;

public interface IReceiptTemplateManager
{
    Task<IDataResult<ReceiptTemplateDto>> GetAsync();
    Task<Entegrasyon.Entity.Results.IResult> UpdateAsync(UpdateReceiptTemplateDto dto);
    Task<IDataResult<string>> UploadLogoAsync(IFormFile file);
    Task<Entegrasyon.Entity.Results.IResult> DeleteLogoAsync();
}
