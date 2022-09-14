using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationLogManager _logManager;

        public LogsController(ApplicationLogManager logManager)
        {
            _logManager = logManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogDetails(int page=1,int itemCount=50,LogType? logType=null,LogAction? logAction=null)
        {
            if(logType == 0)
                logType = null;
            if(logAction==0)
                logAction = null;
            var result =await _logManager.GetPaginatedLogs(page,itemCount,logType,logAction);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
