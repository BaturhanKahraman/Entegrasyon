using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Log;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;
using System.Text.Json;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLogManager(IDbContextFactory<IntegrationDbContext> contextFactory, IHttpContextAccessor httpContextAccessor)
    : IApplicationLogManager
{
    private readonly HttpContext? httpContext = httpContextAccessor.HttpContext;

    public async Task AddLog(string content,LogType type,LogAction action = LogAction.None,object? obj = null,CancellationToken token = default)
    {
        using var context = contextFactory.CreateDbContext();
        var log = new ApplicationLog()
        {
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = httpContext?.GetIPAddress(),
            ApplicationUserId = GetUserId(),
            LogType = type,
            LogAction = action
        };

        if(obj != null)
        {
            log.Object = JsonSerializer.Serialize(obj);
        }

        await context.Logs.AddAsync(log,token);
        await context.SaveChangesAsync(token);
    }

    private Guid? GetUserId()
    {
        var canParseId = Guid.TryParse(httpContext?.GetUserId(),out Guid userId);
        if(canParseId)
            return userId;
        return null;
    }

    public async Task<IDataResult<Pageable<ApplicationLogDetailDto>>> GetPaginatedLogs(int pageIndex = 0,int itemCount = 50,
        LogType? logType = null,LogAction? logAction = null,CancellationToken token=default)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var logs = await dbContext.Logs.OrderByDescending(x => x.Id)
                .Where(x => x.LogType == logType)
                .Where(x => x.LogAction == logAction)
                .Include(x => x.ApplicationUser).Skip((pageIndex - 1) * itemCount).Take(itemCount)
                .Select(x => new ApplicationLogDetailDto
                {
                    Id = x.Id,
                    Content = x.Content!,
                    CreatedAt = x.CreatedAt.UtcDateTime,
                    IpAddress = x.IpAddress!,
                    LogAction = x.LogAction,
                    LogType = x.LogType,
                    UserInfos = x.ApplicationUser != null ? x.ApplicationUser.UserName + ' ' + x.ApplicationUser.Name + ' ' + x.ApplicationUser.Surname : ""
                }).ToListAsync(token);
        int totalItemCount = await dbContext.Logs.CountAsync(token);
        var logResult = new Pageable<ApplicationLogDetailDto>(logs, pageIndex, itemCount, totalItemCount);
        return new SuccessDataResult<Pageable<ApplicationLogDetailDto>>(logResult);
    }


}
