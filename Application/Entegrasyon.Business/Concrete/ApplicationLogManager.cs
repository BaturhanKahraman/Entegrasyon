using Azure.Storage.Blobs.Models;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Logs;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLogManager
{
    private readonly ILogDal _logDal;

    public ApplicationLogManager(ILogDal logDal)
    {
        _logDal = logDal;
    }
    public async Task AddLog(string content,LogType type,string ipAddress)
    {
        var log = new ApplicationLog()
        {
            Content = content,
            IpAddress = ipAddress,
            LogType = type
        };
        await _logDal.AddAsync(log);
    }
    public async Task AddLog(string content,Guid userId,string ipAddress)
    {
        var log = new ApplicationLog()
        {
            Content = content,
            ApplicationUserId = userId,
            IpAddress = ipAddress
        };
        await _logDal.AddAsync(log);
    }
    public async Task AddLog(string content,  Guid userId, string ipAddress,LogType logType)
    {
        var log = new ApplicationLog()
        {
            Content = content, ApplicationUserId = userId, LogType = logType,IpAddress = ipAddress
        };
        await _logDal.AddAsync(log);
    }
    public async Task AddLog(string content,Guid userId,string ipAddress,LogType logType,LogAction logAction)
    {
        var log = new ApplicationLog()
        {
            Content = content,
            ApplicationUserId = userId,
            LogType = logType,
            IpAddress = ipAddress,
            LogAction = logAction
        };
        await _logDal.AddAsync(log);
    }
    


}