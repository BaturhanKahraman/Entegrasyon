using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Entegrasyon.UnitTest;

public class BaseTest
{
    protected Mock<IntegrationDbContext> mockIntegrationDbContext;
    protected Mock<IApplicationLogManager> mockApplicationLogger;
    protected Mock<IFluentValidator> MockValidator;
    protected Mock<IMemoryCache> mockMemoryCache; 
    public BaseTest()
    {
        //mock dbcontextoptions 
        
        DbContextOptionsBuilder<IntegrationDbContext> b = new DbContextOptionsBuilder<IntegrationDbContext>();
        
        mockIntegrationDbContext = new Mock<IntegrationDbContext>(b.Options);

        //application logger mock
        mockApplicationLogger = new Mock<IApplicationLogManager>();
        mockApplicationLogger.Setup(x => x.AddLog(It.IsAny<string>(),It.IsAny<LogType>(),It.IsAny<LogAction>(),It.IsAny<object>(),CancellationToken.None)).Returns(Task.CompletedTask);

        //memory cachemock
        mockMemoryCache = new Mock<IMemoryCache>();
        //MockValidator
    }
}
