using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.AdminPanel.Features.AiCredits;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Test;

public class AiCreditsControllerTests : TestBase
{
    private AiCreditsController CreateController()
    {
        var controller = new AiCreditsController(DbContext);
        SetupControllerContext(controller);
        return controller;
    }

    [Fact]
    public async Task Index_ShouldListAllTenantsWithCredits()
    {
        var t1 = CreateTestTenant("Firma A", "a");
        var t2 = CreateTestTenant("Firma B", "b");
        CreateTestCreditAccount(t1.Id, imageCredits: 100, descCredits: 50);

        var result = await CreateController().Index();

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<AiCreditListViewModel>().Subject;
        model.Items.Should().HaveCount(2);

        var firmaA = model.Items.First(i => i.CompanyName == "Firma A");
        firmaA.ImageGenerationCredits.Should().Be(100);
        firmaA.HasAccount.Should().BeTrue();

        var firmaB = model.Items.First(i => i.CompanyName == "Firma B");
        firmaB.HasAccount.Should().BeFalse();
        firmaB.ImageGenerationCredits.Should().Be(0);
    }

    [Fact]
    public async Task Details_ShouldReturnCreditInfo()
    {
        var tenant = CreateTestTenant();
        var account = CreateTestCreditAccount(tenant.Id, 200, 100);

        var result = await CreateController().Details(tenant.Id);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<AiCreditDetailViewModel>().Subject;
        model.ImageGenerationCredits.Should().Be(200);
        model.ProductDescriptionCredits.Should().Be(100);
    }

    [Fact]
    public async Task Details_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var result = await CreateController().Details(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AddCredits_Get_ShouldReturnForm()
    {
        var tenant = CreateTestTenant();

        var result = await CreateController().AddCredits(tenant.Id);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<AddCreditsFormViewModel>().Subject;
        model.TenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task AddCredits_Post_ShouldCreateAccountAndAddCredits_WhenNoAccountExists()
    {
        var tenant = CreateTestTenant();
        var controller = CreateController();
        var model = new AddCreditsFormViewModel
        {
            TenantId = tenant.Id,
            CreditType = "ImageGeneration",
            Amount = 500,
            Description = "İlk yükleme"
        };

        var result = await controller.AddCredits(model);

        result.Should().BeOfType<RedirectToActionResult>();
        var account = DbContext.AiCreditAccounts.First(a => a.TenantId == tenant.Id);
        account.ImageGenerationCredits.Should().Be(500);
        DbContext.AiCreditTransactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddCredits_Post_ShouldAddToExistingBalance()
    {
        var tenant = CreateTestTenant();
        CreateTestCreditAccount(tenant.Id, imageCredits: 100, descCredits: 50);
        var controller = CreateController();
        var model = new AddCreditsFormViewModel
        {
            TenantId = tenant.Id,
            CreditType = "ProductDescription",
            Amount = 25,
            Description = "Ek kredi"
        };

        var result = await controller.AddCredits(model);

        result.Should().BeOfType<RedirectToActionResult>();
        var account = DbContext.AiCreditAccounts.First(a => a.TenantId == tenant.Id);
        account.ProductDescriptionCredits.Should().Be(75);
        account.ImageGenerationCredits.Should().Be(100); // Unchanged
    }

    [Fact]
    public async Task AddCredits_Post_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var model = new AddCreditsFormViewModel
        {
            TenantId = 999,
            CreditType = "ImageGeneration",
            Amount = 100
        };

        var result = await CreateController().AddCredits(model);

        result.Should().BeOfType<NotFoundResult>();
    }
}
