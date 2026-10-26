using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using FixCampus.Web.Auth;
using FixCampus.Web.Authorization;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Controllers;

public class AccountController : Controller
{
    private readonly SupabaseAuthClient _authClient;
    private readonly FixCampusDbContext _db;
    private readonly IProfileRoleReader _roleReader;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;

    public AccountController(
        SupabaseAuthClient authClient,
        FixCampusDbContext db,
        IProfileRoleReader roleReader,
        ILogger<AccountController> logger,
        IConfiguration configuration)
    {
        _authClient = authClient;
        _db = db;
        _roleReader = roleReader;
        _logger = logger;
        _configuration = configuration;
    }

    private bool HasDatabaseConfigured()
    {
        try
        {
            return !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Supabase")) &&
                   _db.Database.ProviderName != null;
        }
        catch
        {
            return false;
        }
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

        var input = model.EmailOrStudentNo.Trim();
        string email;
        string studentNo = "";

        // Öğrenci numarası (8 haneli) girildiyse kurumsal öğrenci e-postasına dönüştür
        if (Regex.IsMatch(input, @"^\d{8}$"))
        {
            studentNo = input;
            email = $"{input}@ogrenci.ankara.edu.tr";
        }
        else if (input.Contains('@'))
        {
            email = input;
            var prefix = input.Split('@')[0];
            if (Regex.IsMatch(prefix, @"^\d{8}$"))
            {
                studentNo = prefix;
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Lütfen geçerli bir üniversite e-posta adresi veya 8 haneli öğrenci numaranızı giriniz.");
            return View(model);
        }

        // Supabase Auth doğrulama (token?grant_type=password)
        HttpResponseMessage response;
        try
        {
            response = await _authClient.SendAsync(
                HttpMethod.Post,
                "token?grant_type=password",
                new { email, password = model.Password },
                ct: HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase kimlik doğrulama servisine bağlanılamadı.");
            ModelState.AddModelError(string.Empty, "Kimlik doğrulama servisine ulaşılamadı. Lütfen bağlantınızı kontrol edip tekrar deneyiniz.");
            return View(model);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = "E-posta veya şifre hatalı. Lütfen bilgilerinizi kontrol ediniz.";
                try
                {
                    var errorJson = await SupabaseAuthClient.ReadAsync(response, HttpContext.RequestAborted);
                    if (errorJson.TryGetProperty("error_description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
                    {
                        var descStr = desc.GetString()!;
                        if (descStr.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
                        {
                            errorMessage = "E-posta adresiniz henüz onaylanmamış. Lütfen e-postanıza gelen doğrulama bağlantısına tıklayın.";
                        }
                    }
                    else if (errorJson.TryGetProperty("msg", out var msg) && !string.IsNullOrWhiteSpace(msg.GetString()))
                    {
                        var msgStr = msg.GetString()!;
                        if (msgStr.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
                        {
                            errorMessage = "E-posta adresiniz henüz onaylanmamış. Lütfen e-postanıza gelen doğrulama bağlantısına tıklayın.";
                        }
                    }
                }
                catch
                {
                    // Fallback to standard error message
                }

                _logger.LogWarning("Başarısız giriş denemesi: {Email} (Status: {StatusCode})", email, (int)response.StatusCode);
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            }

            var authResult = await SupabaseAuthClient.ReadAsync(response, HttpContext.RequestAborted);
            var accessToken = authResult.GetProperty("access_token").GetString() ?? "";
            var refreshToken = authResult.TryGetProperty("refresh_token", out var rf) ? rf.GetString() : null;
            var expiresIn = authResult.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;

            var userElement = authResult.GetProperty("user");
            var userIdStr = userElement.GetProperty("id").GetString();
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                ModelState.AddModelError(string.Empty, "Supabase kullanıcı kimliği geçerli bir UUID olarak okunamadı.");
                return View(model);
            }

            var userEmail = userElement.TryGetProperty("email", out var ue) ? ue.GetString() ?? email : email;
            string fullName = "";
            if (userElement.TryGetProperty("user_metadata", out var meta) &&
                meta.TryGetProperty("ad_soyad", out var asProp) &&
                asProp.ValueKind == JsonValueKind.String)
            {
                fullName = asProp.GetString()?.Trim() ?? "";
            }

            // Gerçek kullanıcı rolünü oku (IProfileRoleReader üzerinden)
            string role = AppRoles.Student;
            try
            {
                var currentRole = await _roleReader.ReadAsync(userId, HttpContext.RequestAborted);
                if (!string.IsNullOrEmpty(currentRole))
                {
                    role = currentRole;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kullanıcı rolü okunamadı, varsayılan rol atanıyor.");
            }

            if (HasDatabaseConfigured())
            {
                try
                {
                    var profile = await _db.Profiller.AsNoTracking().SingleOrDefaultAsync(p => p.Id == userId, HttpContext.RequestAborted);
                    if (profile != null)
                    {
                        if (!string.IsNullOrWhiteSpace(profile.AdSoyad))
                        {
                            fullName = profile.AdSoyad;
                        }
                    }
                    else
                    {
                        // İlk kez giriş yapıyorsa profil tablosunda satır oluştur
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            fullName = userEmail.Split('@')[0];
                        }

                        await _db.Database.ExecuteSqlInterpolatedAsync($"""
                            INSERT INTO "Profiller" ("Id", "AdSoyad", "Rol", "OlusturulmaTarihi")
                            VALUES ({userId}, {fullName}, 'Ogrenci', {DateTimeOffset.UtcNow})
                            ON CONFLICT ("Id") DO NOTHING
                        """, HttpContext.RequestAborted);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Veritabanından profil bilgisi alınamadı veya kaydedilemedi.");
                }
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = userEmail.Split('@')[0];
            }

            // Yönetici sekmesinden giriş yapıldıysa rol kontrolü:
            // Sadece DB'deki rolü 'Yonetici' olan kullanıcılar yönetici sekmesinden giriş yapabilir veya panele erişebilir.
            if (model.UserType == "admin" && role != AppRoles.Administrator)
            {
                _logger.LogWarning("Yetkisiz yönetici girişi denemesi: {Email} (Rol: {Role})", userEmail, role);
                ModelState.AddModelError(string.Empty, "Bu hesabın yönetici yetkisi bulunmamaktadır. Lütfen Öğrenci Girişi sekmesini kullanınız.");
                return View(model);
            }

            // Claims listesi (Gerçek Supabase UUID kullanılır, Guid.NewGuid ASLA kullanılmaz)
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, fullName),
                new(ClaimTypes.Email, userEmail),
                new(ClaimTypes.Role, role)
            };
            if (!string.IsNullOrEmpty(studentNo))
            {
                claims.Add(new Claim("StudentNo", studentNo));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddSeconds(expiresIn > 0 ? expiresIn : 3600)
            };

            // Supabase erişim ve yenileme token'larını korumalı çerez özelliklerinde sakla
            var tokens = new List<AuthenticationToken>
            {
                new() { Name = "access_token", Value = accessToken }
            };
            if (!string.IsNullOrEmpty(refreshToken))
            {
                tokens.Add(new() { Name = "refresh_token", Value = refreshToken });
            }
            authProperties.StoreTokens(tokens);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Kullanıcı Supabase ile başarıyla giriş yaptı: {Email} ({Role})", userEmail, role);
            TempData["SuccessMessage"] = $"Hoş geldiniz, {fullName}! Başarıyla giriş yaptınız.";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl != "/")
            {
                return LocalRedirect(model.ReturnUrl);
            }

            if (role == AppRoles.Administrator)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }
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

        var trimmedName = model.FullName.Trim();
        if (trimmedName.Length < 2)
        {
            ModelState.AddModelError(nameof(model.FullName), "Ad soyad en az 2 karakter olmalıdır.");
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var password = model.Password;

        // Supabase signup çağrısı
        HttpResponseMessage response;
        try
        {
            response = await _authClient.SendAsync(
                HttpMethod.Post,
                "signup",
                new
                {
                    email,
                    password,
                    data = new { ad_soyad = trimmedName }
                },
                ct: HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase kayıt servisine bağlanılamadı.");
            ModelState.AddModelError(string.Empty, "Kayıt servisine ulaşılamadı. Lütfen daha sonra tekrar deneyiniz.");
            return View(model);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var code = "auth_request_failed";
                try
                {
                    var error = await SupabaseAuthClient.ReadAsync(response, HttpContext.RequestAborted);
                    if (error.TryGetProperty("error_code", out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        var candidate = value.GetString();
                        if (candidate is { Length: > 0 and <= 80 } &&
                            candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) code = candidate;
                    }
                    else if (error.TryGetProperty("msg", out var msgVal))
                    {
                        var msgStr = msgVal.GetString();
                        if (msgStr?.Contains("already registered", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            code = "user_already_exists";
                        }
                    }
                }
                catch { }

                var message = code switch
                {
                    "weak_password" => "Parola Supabase parola kurallarını karşılamıyor (en az 6 karakter olmalıdır).",
                    "email_address_invalid" => "E-posta adresi geçersiz.",
                    "email_address_not_authorized" => "Bu e-posta adresiyle kayıt yapılmasına izin verilmiyor.",
                    "signup_disabled" => "Supabase üzerinde yeni kullanıcı kaydı kapalı.",
                    "email_provider_disabled" => "Supabase e-posta ile giriş sağlayıcısı kapalı.",
                    "over_email_send_rate_limit" => "E-posta gönderim sınırına ulaşıldı. Lütfen bir süre sonra tekrar deneyiniz.",
                    "over_request_rate_limit" => "Çok fazla istek gönderildi. Bir süre sonra tekrar deneyin.",
                    "user_already_exists" => "Bu e-posta adresi ile zaten kayıtlı bir hesap bulunmaktadır.",
                    _ => "Kayıt işlemi tamamlanamadı. Bilgilerinizi kontrol edip tekrar deneyiniz."
                };

                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            var result = await SupabaseAuthClient.ReadAsync(response, HttpContext.RequestAborted);

            string? userIdStr = null;
            if (result.TryGetProperty("id", out var idProp))
            {
                userIdStr = idProp.GetString();
            }
            else if (result.TryGetProperty("user", out var userProp) && userProp.TryGetProperty("id", out var uidProp))
            {
                userIdStr = uidProp.GetString();
            }

            Guid? parsedUserId = null;
            if (Guid.TryParse(userIdStr, out var uid))
            {
                parsedUserId = uid;
            }

            string? accessToken = result.TryGetProperty("access_token", out var atProp) ? atProp.GetString() : null;
            string? refreshToken = result.TryGetProperty("refresh_token", out var rtProp) ? rtProp.GetString() : null;
            int expiresIn = result.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 3600;

            // Veritabanına öğrenci profilini ekle
            if (parsedUserId.HasValue && HasDatabaseConfigured())
            {
                try
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO "Profiller" ("Id", "AdSoyad", "Rol", "OlusturulmaTarihi")
                        VALUES ({parsedUserId.Value}, {trimmedName}, 'Ogrenci', {DateTimeOffset.UtcNow})
                        ON CONFLICT ("Id") DO UPDATE SET "AdSoyad" = EXCLUDED."AdSoyad"
                    """, HttpContext.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kayıt sonrası veritabanı profili eklenemedi.");
                }
            }

            // Eğer Supabase otomatik oturum açıp token döndürdüyse doğrudan giriş yaptır
            if (!string.IsNullOrEmpty(accessToken) && parsedUserId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, parsedUserId.Value.ToString()),
                    new(ClaimTypes.Name, trimmedName),
                    new(ClaimTypes.Email, email),
                    new(ClaimTypes.Role, AppRoles.Student)
                };
                if (!string.IsNullOrEmpty(model.StudentNo))
                {
                    claims.Add(new Claim("StudentNo", model.StudentNo.Trim()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn > 0 ? expiresIn : 3600)
                };

                var tokens = new List<AuthenticationToken>
                {
                    new() { Name = "access_token", Value = accessToken }
                };
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    tokens.Add(new() { Name = "refresh_token", Value = refreshToken });
                }
                authProperties.StoreTokens(tokens);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Yeni öğrenci kaydı oluşturuldu ve oturum açıldı: {Email}", email);
                TempData["SuccessMessage"] = $"Tebrikler {trimmedName}! Kampüs hesabınız başarıyla oluşturuldu.";
                return RedirectToAction("Profile");
            }

            _logger.LogInformation("Yeni kullanıcı kaydı oluşturuldu (onay bekleniyor): {Email}", email);
            TempData["SuccessMessage"] = "Kaydınız başarıyla oluşturuldu. E-posta adresinize gönderilen bağlantıyı onayladıktan sonra giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }
    }

    // ==========================================
    // PROFİL SAYFASI (PROFILE)
    // ==========================================

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var userId))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? AppRoles.Student;
        var fullName = User.Identity?.Name ?? "";
        var studentNo = User.FindFirstValue("StudentNo") ?? "";

        if (string.IsNullOrEmpty(studentNo) && email.Contains('@'))
        {
            var prefix = email.Split('@')[0];
            if (prefix.Length == 8 && prefix.All(char.IsDigit))
            {
                studentNo = prefix;
            }
        }

        DateTimeOffset createdAt = DateTimeOffset.UtcNow;

        if (HasDatabaseConfigured())
        {
            try
            {
                var profile = await _db.Profiller.AsNoTracking().SingleOrDefaultAsync(p => p.Id == userId, HttpContext.RequestAborted);
                if (profile != null)
                {
                    fullName = profile.AdSoyad;
                    role = profile.Rol;
                    createdAt = profile.OlusturulmaTarihi;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Profil bilgisi veritabanından çekilemedi.");
            }
        }

        var viewModel = new ProfileViewModel
        {
            Id = userId,
            FullName = fullName,
            Email = email,
            Role = role,
            StudentNo = studentNo,
            MemberSince = createdAt.ToString("MMMM yyyy", new CultureInfo("tr-TR")),
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
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var userId))
        {
            return RedirectToAction("Login");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var trimmedName = model.FullName.Trim();
        if (trimmedName.Length < 2)
        {
            ModelState.AddModelError(nameof(model.FullName), "Ad soyad en az 2 karakter olmalıdır.");
            return View(model);
        }

        // 1. Veritabanına yaz (Atomic upsert, var olan rol asla değişmez)
        if (HasDatabaseConfigured())
        {
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "Profiller" ("Id", "AdSoyad", "Rol", "OlusturulmaTarihi")
                    VALUES ({userId}, {trimmedName}, 'Ogrenci', {DateTimeOffset.UtcNow})
                    ON CONFLICT ("Id") DO UPDATE SET "AdSoyad" = EXCLUDED."AdSoyad"
                """, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanına profil adı kaydedilemedi.");
                ModelState.AddModelError(string.Empty, "Profil veritabanına kaydedilirken bir hata oluştu.");
                return View(model);
            }
        }

        // 2. Supabase Auth user_metadata'yı güncelle (Bearer access token ile)
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            try
            {
                using var response = await _authClient.SendAsync(
                    HttpMethod.Put,
                    "user",
                    new { data = new { ad_soyad = trimmedName } },
                    token: accessToken,
                    ct: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Supabase kullanıcı metadata güncellemesi başarısız oldu.");
            }
        }

        // 3. MVC çerezindeki AdSoyad claim'ini yenile
        var currentEmail = User.FindFirstValue(ClaimTypes.Email) ?? model.Email;
        var currentRole = User.FindFirstValue(ClaimTypes.Role) ?? model.Role;
        var currentStudentNo = User.FindFirstValue("StudentNo") ?? model.StudentNo;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, trimmedName),
            new(ClaimTypes.Email, currentEmail),
            new(ClaimTypes.Role, currentRole)
        };
        if (!string.IsNullOrEmpty(currentStudentNo))
        {
            claims.Add(new Claim("StudentNo", currentStudentNo));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = authResult.Properties ?? new AuthenticationProperties();

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

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
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            using var response = await _authClient.SendAsync(
                HttpMethod.Post,
                "recover",
                new { email = model.Email.Trim() },
                ct: HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Supabase şifre sıfırlama talebi gönderilemedi.");
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
        // Supabase tarafında oturumu kapat (Bearer access token ile)
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            try
            {
                using var response = await _authClient.SendAsync(
                    HttpMethod.Post,
                    "logout?scope=local",
                    token: accessToken,
                    ct: HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Supabase oturum sonlandırma sırasında hata.");
            }
        }

        // MVC çerez oturumunu temizle
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["InfoMessage"] = "Güvenli bir şekilde oturumunuz kapatıldı.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> LogoutGet()
    {
        return await Logout();
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
