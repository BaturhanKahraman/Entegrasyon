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
    protected Mock<IntegrationDbContext> mockIntegrationDbContext = null!;
    protected Mock<IDbContextFactory<IntegrationDbContext>> mockContextFactory = null!;
    protected Mock<IApplicationLogManager> mockApplicationLogger = null!;
    protected Mock<IFluentValidator> MockValidator = null!;
    protected Mock<IMemoryCache> mockMemoryCache = null!;
    protected Mock<ITenantContext> mockTenantContext = null!;
    public BaseTest()
    {
        //mock dbcontextoptions

        DbContextOptionsBuilder<IntegrationDbContext> b = new DbContextOptionsBuilder<IntegrationDbContext>();

        mockIntegrationDbContext = new Mock<IntegrationDbContext>(b.Options);

        // Mock IDbContextFactory to return the mocked context
        mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        mockContextFactory
            .Setup(f => f.CreateDbContext())
            .Returns(mockIntegrationDbContext.Object);
        mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockIntegrationDbContext.Object);

        //application logger mock
        mockApplicationLogger = new Mock<IApplicationLogManager>();
        mockApplicationLogger.Setup(x => x.AddLog(It.IsAny<string>(),It.IsAny<LogType>(),It.IsAny<LogAction>(),It.IsAny<object>(),CancellationToken.None)).Returns(Task.CompletedTask);

        //memory cachemock
        mockMemoryCache = new Mock<IMemoryCache>();
        //tenant context mock
        mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns(1);
        mockTenantContext.Setup(t => t.IsInitialized).Returns(true);
        //MockValidator
    }
}
