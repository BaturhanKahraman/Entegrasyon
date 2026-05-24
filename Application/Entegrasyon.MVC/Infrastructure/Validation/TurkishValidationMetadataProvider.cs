using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Entegrasyon.MVC.Infrastructure.Validation;

/// <summary>
/// DataAnnotation doğrulama hatalarını Türkçeleştirir. Açıkça ErrorMessage verilmemiş
/// (implicit non-nullable required dahil) tüm doğrulama attribute'larına Türkçe varsayılan
/// mesaj atar. {0} alan adını, {1}/{2} sınır değerlerini temsil eder.
/// Not: Alan adlarının da Türkçe görünmesi için ilgili property'lere [Display(Name="...")]
/// eklenmelidir; aksi halde {0} property adını (ör. "Description") kullanır.
/// </summary>
public sealed class TurkishValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var metadata in context.ValidationMetadata.ValidatorMetadata)
        {
            if (metadata is not ValidationAttribute attr)
                continue;

            if (!string.IsNullOrEmpty(attr.ErrorMessage) || !string.IsNullOrEmpty(attr.ErrorMessageResourceName))
                continue;

            attr.ErrorMessage = attr switch
            {
                RequiredAttribute => "{0} alanı zorunludur.",
                StringLengthAttribute => "{0} alanı en fazla {1} karakter olabilir.",
                MaxLengthAttribute => "{0} alanı en fazla {1} karakter olabilir.",
                MinLengthAttribute => "{0} alanı en az {1} karakter olmalıdır.",
                RangeAttribute => "{0} alanı {1} ile {2} arasında olmalıdır.",
                EmailAddressAttribute => "{0} geçerli bir e-posta adresi olmalıdır.",
                PhoneAttribute => "{0} geçerli bir telefon numarası olmalıdır.",
                UrlAttribute => "{0} geçerli bir adres (URL) olmalıdır.",
                CompareAttribute => "{0} ile {1} eşleşmiyor.",
                RegularExpressionAttribute => "{0} alanı istenen biçimde değil.",
                CreditCardAttribute => "{0} geçerli bir kart numarası olmalıdır.",
                _ => attr.ErrorMessage
            };
        }
    }
}
