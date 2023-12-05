using Entegrasyon.MVC.Utility.Constants;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.MVC.Utility.Helpers.TagHelpers;

[HtmlTargetElement(Attributes = "has-access*")]
public class HasAccesTagHelper : TagHelper
{
    private readonly HttpContext _context;

    public HasAccesTagHelper(IHttpContextAccessor accessor)
    {
        _context = accessor.HttpContext ?? throw new ArgumentNullException(nameof(accessor));
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var attr = context.AllAttributes.FirstOrDefault(a => a.Name.StartsWith("has-access"));
        string postFix = attr!.Name.Remove(0, "has-access".Length);
        var permissions = _context.User.Claims.Where(c => c.Type == StringConstant.Permission).Select(c => c.Value);
        string requiredPermission = attr.Value.ToString();
        if (string.IsNullOrEmpty(requiredPermission))
            throw new Exception("Tag Helper kullanımı yanlış");
        if (permissions.Contains(requiredPermission, StringComparer.InvariantCultureIgnoreCase))
            return;
        switch (postFix.Trim('-'))
        {
            case "disabled":
                output.Attributes.Add("disabled", true);
                output.Attributes.Add("data-container", "body");
                output.Attributes.Add("data-placement", "top");
                output.Attributes.Add("data-content", "Yetkiniz yok!");
                output.Attributes.Add("data-original-title", null);
                output.Attributes.Add("title", null);
                output.Attributes.Add("data-trigger", "hover");
                break;

            case "no-content":
                output.SuppressOutput();
                break;
        }
    }
}