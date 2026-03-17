using Entegrasyon.Entity.Dtos.Log;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract
{
    public interface IApplicationLogManager
    {
        Task AddLog(string content,LogType type,LogAction action = LogAction.None,object? obj = null,CancellationToken token = default);
        Task<IDataResult<Pageable<ApplicationLogDetailDto>>> GetPaginatedLogs(int pageIndex = 0,int itemCount = 50,LogType? logType = null,LogAction? logAction = null,CancellationToken token = default);
    }
}
