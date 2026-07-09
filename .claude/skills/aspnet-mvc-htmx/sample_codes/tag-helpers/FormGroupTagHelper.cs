// Shared/TagHelpers/FormGroupTagHelper.cs
// <form-group asp-for="Title" type="text" placeholder="Ürün adı" />
//
// Output:
// <div class="mb-3">
//   <label class="form-label" for="Title">Başlık</label>
//   <input class="form-control" id="Title" name="Title" type="text" placeholder="Ürün adı" />
//   <span class="text-danger" data-valmsg-for="Title" data-valmsg-replace="true"></span>
// </div>
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.MVC.Shared.TagHelpers;

[HtmlTargetElement("form-group", Attributes = "asp-for")]
public class FormGroupTagHelper(IHtmlGenerator generator) : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("type")]
    public string InputType { get; set; } = "text";

    [HtmlAttributeName("placeholder")]
    public string? Placeholder { get; set; }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "mb-3");

        var label = generator.GenerateLabel(
            ViewContext, For.ModelExplorer, For.Name, labelText: null,
            htmlAttributes: new { @class = "form-label" });

        var input = generator.GenerateTextBox(
            ViewContext, For.ModelExplorer, For.Name, For.Model, format: null,
            htmlAttributes: new
            {
                @class = "form-control",
                type = InputType,
                placeholder = Placeholder ?? ""
            });

        var validation = generator.GenerateValidationMessage(
            ViewContext, For.ModelExplorer, For.Name, message: null, tag: "span",
            htmlAttributes: new { @class = "text-danger" });

        output.Content.AppendHtml(label);
        output.Content.AppendHtml(input);
        output.Content.AppendHtml(validation);

        return Task.CompletedTask;
    }
}

// ── _ViewImports.cshtml ─────────────────────────────────────────────────
// @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
// @addTagHelper *, Entegrasyon.MVC
