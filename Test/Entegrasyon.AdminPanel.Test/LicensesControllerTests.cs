using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.AdminPanel.Features.Licenses;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Test;

public class LicensesControllerTests : TestBase
{
    private LicensesController CreateController()
    {
        var controller = new LicensesController(DbContext);
        SetupControllerContext(controller);
        return controller;
    }

    [Fact]
    public async Task Index_ShouldGroupLicensesByStatus()
    {
        var tenant = CreateTestTenant();
        // Active license
        CreateTestLicense(tenant.Id, LicenseType.Standard, daysFromNow: 60, daysAgo: -30);
        // Expired license
        CreateTestLicense(tenant.Id, LicenseType.Trial, daysFromNow: -1, daysAgo: -60);

        var result = await CreateController().Index();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<LicenseListViewModel>().Subject;
        model.ActiveLicenses.Should().HaveCount(1);
        model.ExpiredLicenses.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_Post_ShouldAddLicense()
    {
        var tenant = CreateTestTenant();
        var controller = CreateController();
        var model = new LicenseFormViewModel
        {
            TenantId = tenant.Id,
            Type = LicenseType.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            Notes = "Yıllık lisans"
        };

        var result = await controller.Create(model);

        result.Should().BeOfType<RedirectToActionResult>();
        DbContext.TenantLicenses.Should().HaveCount(1);
        DbContext.TenantLicenses.First().Type.Should().Be(LicenseType.Premium);
    }

    [Fact]
    public async Task Create_Post_ShouldRejectInvalidDateRange()
    {
        var tenant = CreateTestTenant();
        var controller = CreateController();
        var model = new LicenseFormViewModel
        {
            TenantId = tenant.Id,
            Type = LicenseType.Standard,
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow // End before start
        };

        var result = await controller.Create(model);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Create_Post_ShouldRejectNonExistentTenant()
    {
        var controller = CreateController();
        var model = new LicenseFormViewModel
        {
            TenantId = 999,
            Type = LicenseType.Standard,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1)
        };

        var result = await controller.Create(model);

        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Edit_Post_ShouldUpdateLicense()
    {
        var tenant = CreateTestTenant();
        var license = CreateTestLicense(tenant.Id, LicenseType.Trial);
        var controller = CreateController();
        var model = new LicenseFormViewModel
        {
            Id = license.Id,
            TenantId = tenant.Id,
            Type = LicenseType.Enterprise,
            StartDate = license.StartDate,
            EndDate = license.EndDate,
            Notes = "Upgrade edildi"
        };

        var result = await controller.Edit(license.Id, model);

        result.Should().BeOfType<RedirectToActionResult>();
        DbContext.TenantLicenses.Find(license.Id)!.Type.Should().Be(LicenseType.Enterprise);
    }

    [Fact]
    public async Task Delete_ShouldRemoveLicense()
    {
        var tenant = CreateTestTenant();
        var license = CreateTestLicense(tenant.Id);

        await CreateController().Delete(license.Id);

        DbContext.TenantLicenses.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenLicenseDoesNotExist()
    {
        var result = await CreateController().Delete(999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
