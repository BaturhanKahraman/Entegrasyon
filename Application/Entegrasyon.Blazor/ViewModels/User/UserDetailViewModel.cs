using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Blazor.ViewModels.User;

public class UserDetailViewModel
{
    [Display(Name = "Kimlik Numarası")]
    public Guid Id { get; set; }

    [Display(Name = "Kullanıcı Adı")]
    public string? UserName { get; set; }

    [Display(Name = "E-posta Adresi")]
    public string? Email { get; set; }

    [Display(Name = "İsim")]
    public string? Name { get; set; }

    [Display(Name = "Soyisim")]
    public string? Surname { get; set; }

    [Display(Name = "Kayıt Tarihi")]
    public DateTimeOffset CreatedAt { get; set; }
    [Display(Name = "İki Boyutlu Doğrulama Durumu")]
    public bool IsTwoFactorAuthActive { get; set; }

    [Display(Name = "Yeni Şifre")]
    public bool NeedsTakeNewPassword { get; set; }
    [Display(Name = "Aktiflik Durumu")]
    public bool IsActive { get; set; }

    [Display(Name = "Ofis/Depo")]
    public string? BranchOfficeName { get; set; }

    [Display(Name = "Rol")]
    public string? RoleName { get; set; }
}
