using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Test;

public abstract class TestBase : IDisposable
{
    protected AdminPanelDbContext DbContext { get; }

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<AdminPanelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new AdminPanelDbContext(options);
    }

    protected static void SetupControllerContext(Controller controller)
    {
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
    }

    protected Tenant CreateTestTenant(string name = "Test Firma", string subdomain = "test", bool isActive = true)
    {
        var tenant = new Tenant
        {
            CompanyName = name,
            Subdomain = subdomain,
            ContactEmail = $"{subdomain}@test.com",
            ConnectionString = $"Host=localhost;Database={subdomain}_db",
            DatabaseType = "PostgreSQL",
            IsActive = isActive,
            UserCount = 5
        };
        DbContext.Tenants.Add(tenant);
        DbContext.SaveChanges();
        return tenant;
    }

    protected TenantLicense CreateTestLicense(int tenantId, LicenseType type = LicenseType.Standard,
        int daysFromNow = 30, int daysAgo = -30)
    {
        var license = new TenantLicense
        {
            TenantId = tenantId,
            Type = type,
            StartDate = DateTime.UtcNow.AddDays(daysAgo),
            EndDate = DateTime.UtcNow.AddDays(daysFromNow)
        };
        DbContext.TenantLicenses.Add(license);
        DbContext.SaveChanges();
        return license;
    }

    protected ApplicationLog CreateTestLog(int tenantId, string level = "Information", string message = "Test log")
    {
        var log = new ApplicationLog
        {
            TenantId = tenantId,
            Level = level,
            Message = message,
            Source = "TestSource"
        };
        DbContext.ApplicationLogs.Add(log);
        DbContext.SaveChanges();
        return log;
    }

    protected AiCreditAccount CreateTestCreditAccount(int tenantId, int imageCredits = 100, int descCredits = 50)
    {
        var account = new AiCreditAccount
        {
            TenantId = tenantId,
            ImageGenerationCredits = imageCredits,
            ProductDescriptionCredits = descCredits
        };
        DbContext.AiCreditAccounts.Add(account);
        DbContext.SaveChanges();
        return account;
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
