using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Models.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir üniversite e-posta adresi giriniz.")]
    [Display(Name = "Kayıtlı E-posta Adresi")]
    public string Email { get; set; } = string.Empty;
}
