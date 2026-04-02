using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.MVC.Shared.TagHelpers;

[HtmlTargetElement(Attributes = "require-role")]
public class PermissionGuardTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    [HtmlAttributeName("require-role")]
    public string RequireRole { get; set; } = "";

    public PermissionGuardTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || !user.IsInRole(RequireRole))
        {
            output.SuppressOutput();
        }
    }
}
