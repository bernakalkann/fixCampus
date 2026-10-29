using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Contracts;

public sealed class RegisterRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 2)] public string AdSoyad { get; set; } = "";
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
}

public sealed class RefreshRequest
{
    [Required, StringLength(4096)] public string RefreshToken { get; set; } = "";
}

public sealed class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string AdSoyad { get; set; } = "";
    [StringLength(200)] public string? Bolum { get; set; }
    [StringLength(30)] public string? Sinif { get; set; }
}
