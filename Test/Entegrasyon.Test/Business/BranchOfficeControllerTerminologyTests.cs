using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.BranchOffices;
using Entegrasyon.MVC.Infrastructure.BranchOffices;
using Entegrasyon.MVC.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// /branch-offices sayfası sidebar'da "Depolar" olarak görünür (ti-building-warehouse).
/// Esnafın kafası karışmasın diye sayfa başlığı ve breadcrumb da "Depo" terminolojisini
/// kullanmalı — "Şube/Sube" DEĞİL. (Kullanıcı şikayeti: "depo eklemeyi göremedim".)
/// </summary>
public class BranchOfficeControllerTerminologyTests
{
    private readonly Mock<IBranchOfficeManager> _branchOfficeManager = new();
    private readonly Mock<IOfficeStockManager> _officeStockManager = new();
    private readonly Mock<IActiveBranchOfficeAccessor> _activeBranchOfficeAccessor = new();

    private BranchOfficeController CreateSut()
    {
        var controller = new BranchOfficeController(
            _branchOfficeManager.Object,
            _officeStockManager.Object,
            _activeBranchOfficeAccessor.Object);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.ViewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary());
        controller.TempData = new TempDataDictionary(
            httpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    [Fact]
    public async Task Index_uses_depo_terminology_in_page_title()
    {
        _branchOfficeManager.Setup(m => m.GetPageBranchListAsync())
            .ReturnsAsync(new SuccessDataResult<List<BranchOfficePageListDto>>([]));

        var sut = CreateSut();

        await sut.Index();

        sut.ViewData.GetActiveNav().Should().Be("branch-offices");
        sut.ViewData.GetPageTitle().Should().Be("Depolar",
            "sidebar'da 'Depolar' yazıyor; sayfa başlığı da Depo terminolojisini kullanmalı");
    }

    [Fact]
    public void CreateGet_uses_depo_terminology_in_page_title()
    {
        var sut = CreateSut();

        sut.Create();

        sut.ViewData.GetActiveNav().Should().Be("branch-offices");
        sut.ViewData.GetPageTitle().Should().Be("Yeni Depo",
            "yeni kayıt formu başlığı 'Yeni Depo' olmalı, 'Yeni Sube' değil");
    }
}
