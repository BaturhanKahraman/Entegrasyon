using Shared.Results;

namespace Entegrasyon.Business.Abstract
{
    public interface ITrendyolBrandImporterService
    {
        Task<IResult> ImportAll();
        Task<IResult> QueueImporting();
    }
}