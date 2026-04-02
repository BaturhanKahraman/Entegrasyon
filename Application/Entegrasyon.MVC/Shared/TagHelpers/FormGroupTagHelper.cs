using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.MVC.Shared.TagHelpers;

[HtmlTargetElement("form-group", Attributes = "asp-for")]
public class FormGroupTagHelper : TagHelper
{
    private readonly IHtmlGenerator _generator;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("type")]
    public string InputType { get; set; } = "text";

    [HtmlAttributeName("placeholder")]
    public string? Placeholder { get; set; }

    public FormGroupTagHelper(IHtmlGenerator generator)
    {
        _generator = generator;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "mb-3");

        // Label
        var label = _generator.GenerateLabel(
            ViewContext, For.ModelExplorer, For.Name, labelText: null,
            htmlAttributes: new { @class = "form-label" });

        // Input
        var input = _generator.GenerateTextBox(
            ViewContext, For.ModelExplorer, For.Name, For.Model,
            format: null, htmlAttributes: new { @class = "form-control", type = InputType, placeholder = Placeholder ?? "" });

        // Validation message
        var validation = _generator.GenerateValidationMessage(
            ViewContext, For.ModelExplorer, For.Name, message: null, tag: "span",
            htmlAttributes: new { @class = "text-danger" });

        output.Content.AppendHtml(label);
        output.Content.AppendHtml(input);
        output.Content.AppendHtml(validation);

        await Task.CompletedTask;
    }
}
