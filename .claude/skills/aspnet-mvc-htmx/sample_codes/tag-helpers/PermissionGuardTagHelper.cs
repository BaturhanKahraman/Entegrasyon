// Shared/TagHelpers/PermissionGuardTagHelper.cs
// <button require-role="Admin">...</button>
// <a require-permission="Permissions.Products.View">...</a>
//
// Kullanıcı yetkisi yoksa element render edilmez (SuppressOutput).
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Entegrasyon.MVC.Shared.TagHelpers;

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

[HtmlTargetElement(Attributes = "require-permission")]
public class RequirePermissionTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    [HtmlAttributeName("require-permission")]
    public string RequirePermission { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null) { output.SuppressOutput(); return; }

        // Admin tüm permission'lara erişir
        if (user.IsInRole("Admin")) return;

        if (!user.HasClaim("Permission", RequirePermission))
            output.SuppressOutput();
    }
}

// ── ActiveNavTagHelper: <a nav-active="products"> ───────────────────────
// ViewData.GetActiveNav() ile match olursa class'a "active" ekler.
//
// using Microsoft.AspNetCore.Mvc.Rendering;
// using Microsoft.AspNetCore.Mvc.ViewFeatures;
// using Microsoft.AspNetCore.Razor.TagHelpers;
// using Entegrasyon.MVC.Infrastructure.Extensions;
//
// [HtmlTargetElement("a", Attributes = "nav-active")]
// public class ActiveNavTagHelper : TagHelper
// {
//     [HtmlAttributeNotBound][ViewContext] public ViewContext ViewContext { get; set; } = null!;
//     [HtmlAttributeName("nav-active")] public string NavActive { get; set; } = "";
//
//     public override void Process(TagHelperContext context, TagHelperOutput output)
//     {
//         output.Attributes.RemoveAll("nav-active");
//         if (string.Equals(ViewContext.ViewData.GetActiveNav(), NavActive,
//                 StringComparison.OrdinalIgnoreCase))
//         {
//             var existing = output.Attributes["class"]?.Value?.ToString() ?? "";
//             output.Attributes.SetAttribute("class",
//                 string.IsNullOrEmpty(existing) ? "active" : $"{existing} active");
//         }
//     }
// }
