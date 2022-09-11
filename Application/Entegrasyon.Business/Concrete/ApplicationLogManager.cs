using System.Text.Json;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Http;
using Shared.Extensions;

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
            IpAddress = _httpContextAccessor.HttpContext.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type
        };
        await _logDal.AddAsync(log);
    }

    private Guid? GetUserId()
    {
        var canParseId = Guid.TryParse(_httpContextAccessor.HttpContext.GetUserId(),out Guid userId);
        if (canParseId)
            return userId;
        return null;
    }
    public async Task AddLog(string content,LogType type,LogAction action,object obj)
    {
        string seriliazedLogObj = JsonSerializer.Serialize(obj);
        var log = new ApplicationLog()
        {
            Content = content,
            IpAddress = _httpContextAccessor.HttpContext.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type,
            LogAction=action,
            Object = seriliazedLogObj
        };
        await _logDal.AddAsync(log);
    }
    public async Task AddLog(string content,LogType type,object logObject)
    {
        string seriliazedLogObj = JsonSerializer.Serialize(logObject);
        var log = new ApplicationLog()
        {
            Content = content,
            IpAddress = _httpContextAccessor.HttpContext.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type,
            Object = seriliazedLogObj
        };
        await _logDal.AddAsync(log);
    }
    public async Task AddLog(string content,LogType type,LogAction logAction)
    {
        var log = new ApplicationLog()
        {
            Content = content,
            IpAddress = _httpContextAccessor.HttpContext.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type,
            LogAction = logAction
        };
        await _logDal.AddAsync(log);
    }


}