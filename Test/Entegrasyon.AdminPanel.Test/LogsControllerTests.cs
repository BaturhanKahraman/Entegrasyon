using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.AdminPanel.Features.Logs;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Test;

public class LogsControllerTests : TestBase
{
    private LogsController CreateController() => new(DbContext);

    [Fact]
    public async Task Index_ShouldReturnPaginatedLogs()
    {
        var tenant = CreateTestTenant();
        for (int i = 0; i < 30; i++)
            CreateTestLog(tenant.Id, "Information", $"Log mesajı {i}");

        var result = await CreateController().Index(null, null, null, null, 1);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<LogListViewModel>().Subject;
        model.Logs.Should().HaveCount(25); // PageSize = 25
        model.TotalCount.Should().Be(30);
        model.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Index_ShouldFilterByTenant()
    {
        var t1 = CreateTestTenant("Firma A", "a");
        var t2 = CreateTestTenant("Firma B", "b");
        CreateTestLog(t1.Id, "Information", "Log A");
        CreateTestLog(t2.Id, "Error", "Log B");

        var result = await CreateController().Index(t1.Id, null, null, null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<LogListViewModel>().Subject;
        model.Logs.Should().HaveCount(1);
        model.Logs[0].TenantName.Should().Be("Firma A");
    }

    [Fact]
    public async Task Index_ShouldFilterByLevel()
    {
        var tenant = CreateTestTenant();
        CreateTestLog(tenant.Id, "Information", "Info log");
        CreateTestLog(tenant.Id, "Error", "Error log");
        CreateTestLog(tenant.Id, "Error", "Another error");

        var result = await CreateController().Index(null, "Error", null, null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<LogListViewModel>().Subject;
        model.Logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task Details_ShouldReturnLog()
    {
        var tenant = CreateTestTenant();
        var log = CreateTestLog(tenant.Id, "Error", "Detaylı hata");

        var result = await CreateController().Details(log.Id);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<LogDetailViewModel>().Subject;
        model.Message.Should().Be("Detaylı hata");
        model.Level.Should().Be("Error");
    }

    [Fact]
    public async Task Details_ShouldReturnNotFound_WhenLogDoesNotExist()
    {
        var result = await CreateController().Details(999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
