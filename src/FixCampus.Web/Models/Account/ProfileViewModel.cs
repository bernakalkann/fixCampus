using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Models.Account;

public class ProfileViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ad ve soyad zorunludur.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Öğrenci Numarası")]
    public string StudentNo { get; set; } = string.Empty;

    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Bölüm / Program")]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Kullanıcı Rolü")]
    public string Role { get; set; } = "Ogrenci";

    public string MemberSince { get; set; } = string.Empty;

    public int ActiveListingsCount { get; set; } = 0;
    public int JoinedMeetupsCount { get; set; } = 0;
    public int SolvedLostFoundCount { get; set; } = 0;

    public string? StatusMessage { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifreniz zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Yeni şifreniz en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
    [Display(Name = "Yeni Şifre Tekrar")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
