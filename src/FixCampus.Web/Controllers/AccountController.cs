using System.Security.Claims;
using FixCampus.Web.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Web.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;

    public AccountController(ILogger<AccountController> logger)
    {
        _logger = logger;
    }

    // ==========================================
    // GİRİŞ YAP (LOGIN)
    // ==========================================

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(GetSafeReturnUrl(returnUrl));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Rol ve kullanıcı bilgileri ayrımı (Öğrenci veya Yönetici)
        string fullName;
        string email;
        string studentNo;
        string role;
        string department;

        var input = model.EmailOrStudentNo.Trim();

        if (model.UserType == "admin")
        {
            role = "Yonetici";
            fullName = "Kampüs Yönetici";
            studentNo = "99990001";
            email = input.Contains('@') ? input : $"{input}@ankara.edu.tr";
            department = "Öğrenci İşleri Daire Başkanlığı";
        }
        else
        {
            role = "Ogrenci";
            if (input == "22290455" || input.StartsWith("22290455@") || input.ToLower() == "berke1227")
            {
                fullName = "Berke Mahmut Acar";
                studentNo = "22290455";
                email = "22290455@ogrenci.ankara.edu.tr";
                department = "Bilgisayar Mühendisliği";
            }
            else if (input.Contains('@'))
            {
                email = input;
                studentNo = input.Split('@')[0].All(char.IsDigit) ? input.Split('@')[0] : "22290123";
                fullName = "Öğrenci " + studentNo;
                department = "Mühendislik Fakültesi";
            }
            else
            {
                studentNo = input;
                email = $"{input}@ogrenci.ankara.edu.tr";
                fullName = "Öğrenci " + studentNo;
                department = "Mühendislik Fakültesi";
            }
        }

        // Basit şifre uzunluk kontrolü (En az 6 karakter)
        if (model.Password.Length < 6)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz şifre! Şifreniz en az 6 karakter olmalıdır.");
            return View(model);
        }

        // Kullanıcı Claims listesi oluşturuluyor
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("StudentNo", studentNo),
            new("Department", department)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(4)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("Kullanıcı giriş yaptı: {Email} ({Role})", email, role);
        TempData["SuccessMessage"] = $"Hoş geldiniz, {fullName}! Başarıyla giriş yaptınız.";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        if (role == "Yonetici")
        {
            return RedirectToAction("Index", "Admin");
        }

        return RedirectToAction("Profile", "Account");
    }

    // ==========================================
    // KAYIT OL (REGISTER)
    // ==========================================

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Profile");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Öğrenci numarası ve e-posta uyum kontrolü (İsteğe bağlı kullanıcı dostu yönlendirme)
        if (model.StudentNo.Length != 8 || !model.StudentNo.All(char.IsDigit))
        {
            ModelState.AddModelError(nameof(model.StudentNo), "Öğrenci numarası 8 haneli bir sayı olmalıdır.");
            return View(model);
        }

        // Oturumu başlat
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, model.FullName.Trim()),
            new(ClaimTypes.Email, model.Email.Trim().ToLower()),
            new(ClaimTypes.Role, "Ogrenci"),
            new("StudentNo", model.StudentNo.Trim()),
            new("Department", model.Department.Trim())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

        _logger.LogInformation("Yeni öğrenci kaydı oluşturuldu: {Email}", model.Email);
        TempData["SuccessMessage"] = $"Tebrikler {model.FullName}! Kampüs hesabınız başarıyla oluşturuldu.";

        return RedirectToAction("Profile");
    }

    // ==========================================
    // PROFİL SAYFASI (PROFILE)
    // ==========================================

    [Authorize]
    [HttpGet]
    public IActionResult Profile()
    {
        var fullName = User.Identity?.Name ?? "Öğrenci";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Ogrenci";
        var studentNo = User.FindFirstValue("StudentNo") ?? "22290455";
        var department = User.FindFirstValue("Department") ?? "Bilgisayar Mühendisliği";

        var viewModel = new ProfileViewModel
        {
            FullName = fullName,
            Email = email,
            Role = role,
            StudentNo = studentNo,
            Department = department,
            MemberSince = "Ekim 2026",
            ActiveListingsCount = 0,
            JoinedMeetupsCount = 0,
            SolvedLostFoundCount = 0,
            StatusMessage = TempData["SuccessMessage"]?.ToString()
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Güncellenen bilgileri Claims üzerinde yenile
        var currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? model.Email;
        var currentRole = User.FindFirstValue(ClaimTypes.Role) ?? model.Role;
        var currentStudentNo = User.FindFirstValue("StudentNo") ?? model.StudentNo;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, model.FullName.Trim()),
            new(ClaimTypes.Email, currentEmail),
            new(ClaimTypes.Role, currentRole),
            new("StudentNo", currentStudentNo),
            new("Department", model.Department?.Trim() ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
        return RedirectToAction("Profile");
    }

    // ==========================================
    // ŞİFREMİ UNUTTUM (FORGOT PASSWORD)
    // ==========================================

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["SuccessMessage"] = $"{model.Email} adresine şifre sıfırlama bağlantısı gönderildi. Lütfen gelen kutunuzu kontrol ediniz.";
        return RedirectToAction("Login");
    }

    // ==========================================
    // ÇIKIŞ YAP (LOGOUT)
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["InfoMessage"] = "Güvenli bir şekilde oturumunuz kapatıldı.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> LogoutGet()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["InfoMessage"] = "Güvenli bir şekilde oturumunuz kapatıldı.";
        return RedirectToAction("Index", "Home");
    }

    // ==========================================
    // YETKİSİZ ERİŞİM (ACCESS DENIED)
    // ==========================================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ==========================================
    // YARDIMCI METODLAR
    // ==========================================

    private string GetSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return Url.Action("Index", "Home") ?? "/";
    }
}
