using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Blazor.ViewModels.CategoryAttribute;
using Entegrasyon.Blazor.ViewModels.CategoryImport;
using System.Text.Json;

namespace Entegrasyon.Blazor.Utility.Extensions;

public static class TypeConverterExtension
{
    public static string ToJson<T>(this T t)=> JsonSerializer.Serialize(t);
    public static TagifyValue ToTagifyValue(this CategoryAttributeValueViewModel vm)
        => new TagifyValue(vm.Id ?? 0, vm.Name);
    public static List<TagifyValue> ToTagifyValue(this List<CategoryAttributeValueViewModel> vm)
       => vm.Select(v=>v?.ToTagifyValue()).ToList()!;
    public static JsTreeViewModel ToJsTree(this ImportedTrendyolCategory cat)
        => new()
        {
            Id=cat.Id.ToString(),
            Text=cat.Name,
            Children=cat.SubCategories.Select(sc=>sc.ToJsTree()).ToArray()
        };
    public static IEnumerable<JsTreeViewModel> ToJsTreeList(this IEnumerable<ImportedTrendyolCategory> catList)
        => catList.Select(c => c?.ToJsTree())!;

    public static TrendyolSelectedCategory ToTrendyolSelectedCategory(this TrendyolImportViewModel model)
    {
        return new TrendyolSelectedCategory
        {
            Id = Convert.ToInt32(model.Id),
            Name = model.Text,
            ParentId =string.IsNullOrWhiteSpace(model.Parent) ? null : Convert.ToInt32(model.Parent),
            SubCategories = model.Children.Select(c => c.ToTrendyolSelectedCategory()).ToList()
        };
    }
}
