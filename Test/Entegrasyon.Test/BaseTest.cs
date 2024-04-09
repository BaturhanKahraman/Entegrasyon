using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Extensions;

namespace Entegrasyon.UnitTest;

public class BaseTest
{
    protected Mock<IntegrationDbContext> integrationDbContextMock;
    protected Mock<IApplicationLogManager> applicationLoggerMock;
    public BaseTest()
    {
        //mock dbcontextoptions 
        DbContextOptionsBuilder<IntegrationDbContext> b = new DbContextOptionsBuilder<IntegrationDbContext>();
        
        integrationDbContextMock = new Mock<IntegrationDbContext>(b.Options);

        //application logger mock
        applicationLoggerMock = new Mock<IApplicationLogManager>();

        applicationLoggerMock.Setup(x => x.AddLog(It.IsAny<string>(),It.IsAny<LogType>(),It.IsAny<LogAction>(),It.IsAny<object>(),CancellationToken.None)).Returns(Task.CompletedTask);

    }
}
