using Entegrasyon.Blazor.Utility.Constants;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.Blazor.Utility.Helpers;

public static class AlertTypeHelper
{
    private static readonly (string Type, string Class)[] AlertTypes =
     [
        (Type: StringConstant.SuccessAlert, Class: "alert-success"),
        (Type: StringConstant.DangerAlert, Class: "alert-danger"),
        (Type: StringConstant.WarningAlert, Class: "alert-warning")
     ];

    public static IHtmlContent RenderAlerts(this ViewDataDictionary viewData)
    {
        var builder = new HtmlContentBuilder();
        var alerts = AlertTypes
            .Where(alertType => viewData[alertType.Type] != null)
            .Select(alertType =>
            builder.AppendHtmlLine($"<div class=\"alert {alertType.Class}\">{viewData[alertType.Type]}</div>"))
            .ToList();
        return builder;
    }
}
