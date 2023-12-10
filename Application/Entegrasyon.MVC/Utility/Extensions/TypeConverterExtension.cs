using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.ViewModels.CategoryAttribute;
using Entegrasyon.MVC.ViewModels.CategoryImport;
using System.Text.Json;

namespace Entegrasyon.MVC.Utility.Extensions;

public static class TypeConverterExtension
{
    public static string ToJson<T>(this T t)=> JsonSerializer.Serialize(t);
    public static TagifyValue ToTagifyValue(this CategoryAttributeValueViewModel vm)
        => new TagifyValue(vm.Id ?? 0, vm.Name);
    public static List<TagifyValue> ToTagifyValue(this List<CategoryAttributeValueViewModel> vm)
       => vm.Select(v=>v?.ToTagifyValue()).ToList();
    public static JsTreeViewModel ToJsTree(this ImportedTrendyolCategory cat)
        => new()
        {
            Id=cat.Id.ToString(),
            Text=cat.Name,
            Children=cat.SubCategories.Select(sc=>sc.ToJsTree()).ToArray()
        };
    public static IEnumerable<JsTreeViewModel> ToJsTreeList(this IEnumerable<ImportedTrendyolCategory> catList)
        => catList.Select(c => c?.ToJsTree());
}
