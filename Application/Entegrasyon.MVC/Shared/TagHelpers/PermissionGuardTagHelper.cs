using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.MVC.Shared.TagHelpers;

/// <summary>
/// require-role="Admin" — kullanıcı belirtilen role sahip değilse elementi gizler.
/// </summary>
[HtmlTargetElement(Attributes = "require-role")]
public class RequireRoleTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    [HtmlAttributeName("require-role")]
    public string RequireRole { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || !user.IsInRole(RequireRole))
            output.SuppressOutput();
    }
}

/// <summary>
/// require-permission="Permissions.Products.View" — kullanıcı belirtilen Permission claim'ine
/// sahip değilse elementi gizler. Admin rolü bypass eder.
/// </summary>
[HtmlTargetElement(Attributes = "require-permission")]
public class RequirePermissionTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    [HtmlAttributeName("require-permission")]
    public string RequirePermission { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            output.SuppressOutput();
            return;
        }

        // Admin bypass — tüm permission'lara erişim
        if (user.IsInRole("Admin"))
            return;

        // Permission claim kontrolü
        if (!user.HasClaim("Permission", RequirePermission))
            output.SuppressOutput();
    }
}
