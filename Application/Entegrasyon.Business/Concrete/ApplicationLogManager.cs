using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Dtos.Log;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Extensions;
using Shared.Results;
using System.Text.Json;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLogManager
{
    private readonly ILogDal _logDal;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ApplicationLogManager(ILogDal logDal,IHttpContextAccessor httpContextAccessor)
    {
        _logDal = logDal;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task AddLog(string content,LogType type)
    {
        var log = new ApplicationLog()
        {
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type
        };
        await _logDal.AddAsync(log);
    }

    private Guid? GetUserId()
    {
        var canParseId = Guid.TryParse(_httpContextAccessor.HttpContext.GetUserId(),out Guid userId);
        if(canParseId)
            return userId;
        return null;
    }
    public async Task AddLog(string content,LogType type,LogAction action,object obj)
    {
        string seriliazedLogObj = JsonSerializer.Serialize(obj);
        var log = new ApplicationLog()
        {
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type,
            LogAction = action,
            Object = seriliazedLogObj
        };
        await _logDal.AddAsync(log);
    }
    
    public async Task AddLog(string content,LogType type,LogAction logAction)
    {
        var log = new ApplicationLog()
        {
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type,
            LogAction = logAction
        };
        await _logDal.AddAsync(log);
    }

    public async Task<IDataResult<Pageable<ApplicationLogDetailDto>>> GetPaginatedLogs(int pageIndex = 0,int itemCount = 50,
        LogType? logType = null,LogAction? logAction = null)
    {
        var logs = await
            _logDal.Table.OrderByDescending(x => x.Id)
                .WhereIf(logType != null,x => x.LogType == logType)
                .WhereIf(logAction != null,x => x.LogAction == logAction)
                .Include(x => x.ApplicationUser).Skip((pageIndex - 1) * itemCount).Take(itemCount)
                .Select(x => new ApplicationLogDetailDto
                {
                    Id = x.Id,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt.UtcDateTime,
                    IpAddress = x.IpAddress,
                    LogAction = x.LogAction,
                    LogType = x.LogType,
                    UserInfos = x.ApplicationUser.UserName + ' ' + x.ApplicationUser.Name + ' ' + x.ApplicationUser.Surname
                }).ToListAsync();
        int totalItemCount = _logDal.Table.Count();
        var logResult = new Pageable<ApplicationLogDetailDto>(logs, pageIndex, itemCount, totalItemCount);
        return new SuccessDataResult<Pageable<ApplicationLogDetailDto>>(logResult);
    }


}