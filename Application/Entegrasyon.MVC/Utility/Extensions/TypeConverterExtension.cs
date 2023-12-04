using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.ViewModels.CategoryAttribute;
using System.Text.Json;

namespace Entegrasyon.MVC.Utility.Extensions;

public static class TypeConverterExtension
{
    public static string ToJson<T>(this T t)=> JsonSerializer.Serialize(t);
    public static TagifyValue ToTagifyValue(this CategoryAttributeValueViewModel vm)
        => new TagifyValue(vm.Id ?? 0, vm.Name);
    public static List<TagifyValue> ToTagifyValue(this List<CategoryAttributeValueViewModel> vm)
       => vm.Select(v=>new TagifyValue(v.Id ?? 0, v.Name)).ToList();
}
