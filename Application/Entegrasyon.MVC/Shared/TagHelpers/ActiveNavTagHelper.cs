using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Shared.TagHelpers;

[HtmlTargetElement("a", Attributes = "nav-active")]
public class ActiveNavTagHelper : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("nav-active")]
    public string NavActive { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("nav-active");

        var activeNav = ViewContext.ViewData.GetActiveNav();
        if (string.Equals(activeNav, NavActive, StringComparison.OrdinalIgnoreCase))
        {
            var existingClass = output.Attributes["class"]?.Value?.ToString() ?? "";
            var newClass = string.IsNullOrEmpty(existingClass) ? "active" : $"{existingClass} active";
            output.Attributes.SetAttribute("class", newClass);
        }
    }
}
