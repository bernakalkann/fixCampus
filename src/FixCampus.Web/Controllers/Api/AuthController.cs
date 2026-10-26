using FixCampus.Web.Auth;
using FixCampus.Web.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FixCampus.Web.Controllers.Api;

[ApiController, Route("api/auth"), EnableRateLimiting("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(SupabaseAuthClient auth) : ControllerBase
{
    [HttpPost("kayit")]
    public Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (request.AdSoyad.Trim().Length < 2)
            return Task.FromResult<IActionResult>(BadRequest(new { message = "Ad soyad en az 2 karakter olmalıdır." }));
        return Forward("signup", new { email = request.Email.Trim(), password = request.Password,
            data = new { ad_soyad = request.AdSoyad.Trim() } }, ct);
    }

    [HttpPost("giris")]
    public Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        Forward("token?grant_type=password", new { email = request.Email.Trim(), password = request.Password }, ct);

    [HttpPost("yenile")]
    public Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct) =>
        Forward("token?grant_type=refresh_token", new { refresh_token = request.RefreshToken }, ct);

    [Authorize(AuthenticationSchemes = "Supabase"), HttpPost("cikis")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            using var response = await auth.SendAsync(HttpMethod.Post, "logout?scope=local",
                token: Request.Headers.Authorization.ToString()[7..].Trim(), ct: ct);
            return response.IsSuccessStatusCode ? NoContent() : Unauthorized(new { message = "Oturum sonlandırılamadı." });
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException ||
            (ex is OperationCanceledException && !ct.IsCancellationRequested))
        { return StatusCode(503, new { message = "Kimlik doğrulama servisine ulaşılamadı." }); }
    }

    private async Task<IActionResult> Forward(string path, object body, CancellationToken ct)
    {
        try
        {
            using var response = await auth.SendAsync(HttpMethod.Post, path, body, ct: ct);
            if (!response.IsSuccessStatusCode)
            {
                var code = "auth_request_failed";
                try
                {
                    var error = await SupabaseAuthClient.ReadAsync(response, ct);
                    if (error.TryGetProperty("error_code", out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var candidate = value.GetString();
                        if (candidate is { Length: > 0 and <= 80 } &&
                            candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) code = candidate;
                    }
                }
                catch (System.Text.Json.JsonException) { }
                var message = code switch
                {
                    "weak_password" => "Parola Supabase parola kurallarını karşılamıyor.",
                    "email_address_invalid" => "E-posta adresi geçersiz.",
                    "email_address_not_authorized" => "Bu e-posta adresiyle kayıt yapılmasına izin verilmiyor.",
                    "signup_disabled" => "Supabase üzerinde yeni kullanıcı kaydı kapalı.",
                    "email_provider_disabled" => "Supabase e-posta ile giriş sağlayıcısı kapalı.",
                    "over_email_send_rate_limit" => "E-posta gönderim sınırına ulaşıldı. Gösterim için Confirm email ayarını kontrol edin.",
                    "over_request_rate_limit" => "Çok fazla istek gönderildi. Bir süre sonra tekrar deneyin.",
                    _ => "İşlem tamamlanamadı. Bilgilerinizi ve Supabase ayarlarını kontrol edin."
                };
                return StatusCode((int)response.StatusCode == 429 ? 429 : 400,
                    new { message, code, providerStatus = (int)response.StatusCode });
            }
            var result = await SupabaseAuthClient.ReadAsync(response, ct);
            return Ok(result);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException ||
            (ex is OperationCanceledException && !ct.IsCancellationRequested))
        { return StatusCode(503, new { message = "Kimlik doğrulama servisine ulaşılamadı." }); }
    }
}
