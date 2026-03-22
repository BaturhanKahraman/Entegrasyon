using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.AdminPanel.Features.Tenants;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Test;

public class TenantsControllerTests : TestBase
{
    private TenantsController CreateController()
    {
        var controller = new TenantsController(DbContext);
        SetupControllerContext(controller);
        return controller;
    }

    [Fact]
    public async Task Index_ShouldReturnAllTenants()
    {
        CreateTestTenant("Firma A", "firma-a");
        CreateTestTenant("Firma B", "firma-b");

        var result = await CreateController().Index(null);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<TenantListViewModel>>().Subject;
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task Index_ShouldFilterBySearch()
    {
        CreateTestTenant("Alfa Yazılım", "alfa");
        CreateTestTenant("Beta Tekstil", "beta");

        var result = await CreateController().Index("Alfa");

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<TenantListViewModel>>().Subject;
        model.Should().HaveCount(1);
        model[0].CompanyName.Should().Be("Alfa Yazılım");
    }

    [Fact]
    public async Task Details_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var result = await CreateController().Details(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ShouldReturnTenantWithRelatedData()
    {
        var tenant = CreateTestTenant("Detay Firma", "detay");
        CreateTestLicense(tenant.Id);
        CreateTestLog(tenant.Id);

        var result = await CreateController().Details(tenant.Id);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<TenantDetailViewModel>().Subject;
        model.CompanyName.Should().Be("Detay Firma");
        model.TotalLicenseCount.Should().Be(1);
        model.LogCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_Post_ShouldAddTenant()
    {
        var controller = CreateController();
        var model = new TenantFormViewModel
        {
            CompanyName = "Yeni Firma",
            Subdomain = "yeni",
            ConnectionString = "Host=localhost;Database=yeni_db",
            DatabaseType = "PostgreSQL"
        };

        var result = await controller.Create(model);

        result.Should().BeOfType<RedirectToActionResult>();
        DbContext.Tenants.Should().HaveCount(1);
        DbContext.Tenants.First().CompanyName.Should().Be("Yeni Firma");
    }

    [Fact]
    public async Task Create_Post_ShouldRejectDuplicateSubdomain()
    {
        CreateTestTenant("Mevcut Firma", "mevcut");
        var controller = CreateController();
        var model = new TenantFormViewModel
        {
            CompanyName = "Başka Firma",
            Subdomain = "mevcut",
            ConnectionString = "Host=localhost;Database=baska_db",
            DatabaseType = "PostgreSQL"
        };

        var result = await controller.Create(model);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState[nameof(TenantFormViewModel.Subdomain)]!.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Edit_Get_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var result = await CreateController().Edit(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_ShouldUpdateTenant()
    {
        var tenant = CreateTestTenant("Eski İsim", "eski");
        var controller = CreateController();
        var model = new TenantFormViewModel
        {
            Id = tenant.Id,
            CompanyName = "Yeni İsim",
            Subdomain = "eski",
            ConnectionString = tenant.ConnectionString,
            DatabaseType = tenant.DatabaseType
        };

        var result = await controller.Edit(tenant.Id, model);

        result.Should().BeOfType<RedirectToActionResult>();
        var updated = DbContext.Tenants.Find(tenant.Id)!;
        updated.CompanyName.Should().Be("Yeni İsim");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ToggleStatus_ShouldFlipIsActive()
    {
        var tenant = CreateTestTenant("Aktif Firma", "aktif", isActive: true);
        var controller = CreateController();

        await controller.ToggleStatus(tenant.Id);

        DbContext.Tenants.Find(tenant.Id)!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldSoftDelete()
    {
        var tenant = CreateTestTenant("Silinecek Firma", "sil");

        await CreateController().Delete(tenant.Id);

        DbContext.Tenants.Find(tenant.Id)!.IsActive.Should().BeFalse();
    }
}
