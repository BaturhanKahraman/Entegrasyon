using Entegrasyon.Blazor.Utility.Objects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Entegrasyon.Blazor.Utility.Extensions;

public static class ModelStateExtension
{
    public static string SerializeModelState(this ModelStateDictionary modelState)
    {
        var dict = modelState.Select(kvp => new ModelStateTransferValue()
        {
            Key = kvp.Key,
            AttemptedValue = kvp.Value!.AttemptedValue,
            RawValue = kvp.Value.RawValue,
            ErrorMessages = kvp.Value.Errors.Select(err => err.ErrorMessage).ToList(),
        });
        return JsonConvert.SerializeObject(dict);
    }
    public static void DeSerializeModelState(this ModelStateDictionary modelState,string serialisedErrorList)
    {
        var errorList = JsonConvert.DeserializeObject<List<ModelStateTransferValue>>(serialisedErrorList);
        modelState.Clear();
        foreach(var item in errorList!)
        {
            modelState.SetModelValue(item.Key!,item.RawValue,item.AttemptedValue);
            foreach(var error in item.ErrorMessages)
            {
                modelState.AddModelError(item.Key!,error);
            }
        }
    }
}
