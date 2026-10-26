using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Models.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad ve soyad alanı zorunludur.")]
    [StringLength(70, MinimumLength = 3, ErrorMessage = "Ad ve soyad 3 ile 70 karakter arasında olmalıdır.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Öğrenci numarası zorunludur.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Öğrenci numarası 8 haneli rakamlardan oluşmalıdır (Örn: 22290455).")]
    [Display(Name = "Öğrenci Numarası")]
    public string StudentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "Üniversite E-postası")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifreniz en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre Tekrar")]
    [Compare("Password", ErrorMessage = "Girdiğiniz şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
