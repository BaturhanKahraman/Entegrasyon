using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.AdminPanel.Features.Dashboard;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Test;

public class DashboardControllerTests : TestBase
{
    private DashboardController CreateController() => new(DbContext);

    [Fact]
    public async Task Index_ShouldReturnCorrectCounts()
    {
        var t1 = CreateTestTenant("Aktif A", "aktif-a", isActive: true);
        var t2 = CreateTestTenant("Aktif B", "aktif-b", isActive: true);
        CreateTestTenant("Pasif", "pasif", isActive: false);

        CreateTestLicense(t1.Id, daysFromNow: 15); // Expiring within 30 days
        CreateTestLog(t1.Id, "Error", "Test error");

        var result = await CreateController().Index();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<DashboardViewModel>().Subject;
        model.TotalTenants.Should().Be(3);
        model.ActiveTenants.Should().Be(2);
        model.ExpiringLicenses.Should().Be(1);
        model.RecentTenants.Should().HaveCount(3);
    }

    [Fact]
    public async Task Index_ShouldReturnEmptyDashboard_WhenNoData()
    {
        var result = await CreateController().Index();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<DashboardViewModel>().Subject;
        model.TotalTenants.Should().Be(0);
        model.ActiveTenants.Should().Be(0);
        model.RecentTenants.Should().BeEmpty();
        model.RecentLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task Index_ShouldLimitRecentTenants_ToFive()
    {
        for (int i = 0; i < 8; i++)
            CreateTestTenant($"Firma {i}", $"firma-{i}");

        var result = await CreateController().Index();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<DashboardViewModel>().Subject;
        model.RecentTenants.Should().HaveCount(5);
    }

    [Fact]
    public async Task Index_ShouldLimitRecentLogs_ToTen()
    {
        var tenant = CreateTestTenant();
        for (int i = 0; i < 15; i++)
            CreateTestLog(tenant.Id, "Information", $"Log {i}");

        var result = await CreateController().Index();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<DashboardViewModel>().Subject;
        model.RecentLogs.Should().HaveCount(10);
    }
}
