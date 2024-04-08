using Entegrasyon.Entity.Dtos.Log;
using Entegrasyon.Entity.Logs;
using Shared.Entity;
using Shared.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entegrasyon.Business.Abstract
{
    public interface IApplicationLogManager
    {
        Task AddLog(string content,LogType type,LogAction action = LogAction.None,object obj = null,CancellationToken token = default);
        Task<IDataResult<Pageable<ApplicationLogDetailDto>>> GetPaginatedLogs(int pageIndex = 0,int itemCount = 50,LogType? logType = null,LogAction? logAction = null,CancellationToken token = default);
    }
}
