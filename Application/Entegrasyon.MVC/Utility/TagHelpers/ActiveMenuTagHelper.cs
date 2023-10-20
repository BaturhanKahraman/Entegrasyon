using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.MVC.Utility.TagHelpers;

[HtmlTargetElement(Attributes = "active-menu")]
public sealed class ActiveMenuTagHelper : AnchorTagHelper
{
    public ActiveMenuTagHelper(IHtmlGenerator generator) : base(generator)
    {
    }

    public override async Task ProcessAsync(TagHelperContext context,TagHelperOutput output)
    {
        var routeData = ViewContext.RouteData.Values;
        var currentController = routeData["controller"] as string;
        var childContent = (await output.GetChildContentAsync()).GetContent();
        string hrefContent = GetHrefContent(childContent).ToString();
        if(string.IsNullOrEmpty(hrefContent))
            return;
        bool startsWithSlash = hrefContent.StartsWith('\\') || hrefContent.StartsWith('/');
        var hrefParts = hrefContent.Replace('\\','/').Split('/');
        string hrefControllerPart = startsWithSlash ? hrefParts[1] : hrefParts[0];
        var result = string.Equals(hrefControllerPart,currentController,StringComparison.OrdinalIgnoreCase);
        if(!result)
            return;
        var existingClasses = output.Attributes["class"].Value.ToString();
        if(output.Attributes["class"] != null)
            output.Attributes.Remove(output.Attributes["class"]);
        output.Attributes.Add("class",$"{existingClasses} active");
    }

    private static ReadOnlySpan<char> GetHrefContent(string aTag)
    {
        ReadOnlySpan<char> span = aTag.AsSpan();
        ReadOnlySpan<char> hrefPattern = "href=\"".AsSpan();
        int hrefStart = span.IndexOf(hrefPattern);
        if(hrefStart == -1)
            return ReadOnlySpan<char>.Empty;
        ReadOnlySpan<char> contentStart = span[(hrefStart + hrefPattern.Length)..];
        int contentEnd = contentStart.IndexOf('"');
        if(contentEnd != -1)
            return contentStart.Slice(0,contentEnd);
        return ReadOnlySpan<char>.Empty;
    }
}