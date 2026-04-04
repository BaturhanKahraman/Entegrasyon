using Entegrasyon.MVC.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.MVC.Test.Infrastructure;

public class ViewDataExtensionsTests
{
    private ViewDataDictionary CreateViewData()
        => new(new EmptyModelMetadataProvider(), new ModelStateDictionary());

    [Theory]
    [InlineData("products", "yonetim")]
    [InlineData("categories", "yonetim")]
    [InlineData("attributes", "yonetim")]
    [InlineData("brands", "yonetim")]
    [InlineData("pos", "yonetim")]
    [InlineData("sales", "yonetim")]
    [InlineData("orders", "yonetim")]
    [InlineData("shipping", "yonetim")]
    [InlineData("bulk-operations", "yonetim")]
    [InlineData("branch-offices", "yonetim")]
    [InlineData("marketplace-sync", "pazaryeri")]
    [InlineData("marketplace-matching", "pazaryeri")]
    [InlineData("marketplace-orders", "pazaryeri")]
    [InlineData("commission-rates", "pazaryeri")]
    [InlineData("matched-entities", "pazaryeri")]
    [InlineData("customers", "musteriler")]
    [InlineData("invoicing", "musteriler")]
    [InlineData("reports", "raporlar")]
    [InlineData("storefront", "magaza")]
    [InlineData("settings", "ayarlar")]
    [InlineData("users", "ayarlar")]
    [InlineData("roles", "ayarlar")]
    [InlineData("admin-notifications", "ayarlar")]
    [InlineData("dashboard", "")]
    [InlineData("chat", "")]
    [InlineData("", "")]
    public void GetActiveNavGroup_ShouldReturnCorrectGroup(string activeNav, string expectedGroup)
    {
        var viewData = CreateViewData();
        viewData.SetActiveNav(activeNav);

        var group = viewData.GetActiveNavGroup();

        group.Should().Be(expectedGroup);
    }

    [Fact]
    public void GetActiveNavGroup_WhenActiveNavNotSet_ShouldReturnEmpty()
    {
        var viewData = CreateViewData();

        var group = viewData.GetActiveNavGroup();

        group.Should().Be("");
    }
}
