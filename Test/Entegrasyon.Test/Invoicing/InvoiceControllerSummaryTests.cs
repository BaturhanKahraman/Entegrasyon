using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.Invoicing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Entegrasyon.UnitTest.Invoicing;

/// <summary>
/// /invoicing header KPI kartlari wiring'i: Index full-page render'da
/// GetInvoiceSummary cagrilip ViewBag.InvoiceDraftCount / InvoiceSentCount /
/// InvoiceMonthGrandTotal set edilmeli (view bunlari null-safe okuyor).
/// HTMX partial render'da aggregate cagrilmamali (gereksiz DB-hit).
/// </summary>
public class InvoiceControllerSummaryTests
{
    private readonly Mock<IEInvoiceManager> _invoiceManager = new();

    private InvoiceController CreateSut(bool isHtmx = false)
    {
        _invoiceManager.Setup(m => m.GetInvoices(It.IsAny<EInvoiceFilterDto>()))
            .ReturnsAsync(new SuccessDataResult<Pageable<EInvoiceListDto>>(
                new Pageable<EInvoiceListDto>([], 0, 20, 0)));

        var controller = new InvoiceController(_invoiceManager.Object);

        var httpContext = new DefaultHttpContext();
        if (isHtmx)
            httpContext.Request.Headers["HX-Request"] = "true";

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.ViewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary());
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    [Fact]
    public async Task Index_full_page_sets_summary_viewbag_from_aggregate()
    {
        _invoiceManager.Setup(m => m.GetInvoiceSummary(It.IsAny<EInvoiceFilterDto>()))
            .ReturnsAsync(new SuccessDataResult<EInvoiceSummaryDto>(
                new EInvoiceSummaryDto(DraftCount: 7, SentCount: 12, MonthGrandTotal: 4530.50m)));

        var sut = CreateSut();

        await sut.Index();

        ((int)sut.ViewBag.InvoiceDraftCount).Should().Be(7);
        ((int)sut.ViewBag.InvoiceSentCount).Should().Be(12);
        ((decimal)sut.ViewBag.InvoiceMonthGrandTotal).Should().Be(4530.50m);
    }

    [Fact]
    public async Task Index_full_page_defaults_to_zero_when_summary_data_null()
    {
        _invoiceManager.Setup(m => m.GetInvoiceSummary(It.IsAny<EInvoiceFilterDto>()))
            .ReturnsAsync(new ErrorDataResult<EInvoiceSummaryDto>(null!, "ozet alinamadi"));

        var sut = CreateSut();

        await sut.Index();

        ((int)sut.ViewBag.InvoiceDraftCount).Should().Be(0);
        ((int)sut.ViewBag.InvoiceSentCount).Should().Be(0);
        ((decimal)sut.ViewBag.InvoiceMonthGrandTotal).Should().Be(0m);
    }

    [Fact]
    public async Task Index_htmx_partial_does_not_call_summary_aggregate()
    {
        var sut = CreateSut(isHtmx: true);

        await sut.Index();

        _invoiceManager.Verify(m => m.GetInvoiceSummary(It.IsAny<EInvoiceFilterDto>()), Times.Never);
    }
}
