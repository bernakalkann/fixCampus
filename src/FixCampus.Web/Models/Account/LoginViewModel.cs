using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Models.Account;

public class LoginViewModel
{
    [Display(Name = "Giriş Türü")]
    public string UserType { get; set; } = "student"; // "student" veya "admin"

    [Required(ErrorMessage = "E-posta adresi veya öğrenci numarası zorunludur.")]
    [Display(Name = "E-posta veya Öğrenci No")]
    public string EmailOrStudentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni Hatırla")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
