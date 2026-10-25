using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FixCampus.Web.Auth;

public sealed class SupabaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, SupabaseAuthClient auth)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private bool _serviceUnavailable;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();
        var token = header[7..].Trim();
        if (token.Length == 0) return AuthenticateResult.Fail("Token eksik.");
        try
        {
            using var response = await auth.SendAsync(HttpMethod.Get, "user", token: token, ct: Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                _serviceUnavailable = (int)response.StatusCode >= 500 || (int)response.StatusCode == 429;
                return AuthenticateResult.Fail("Oturum geçersiz veya doğrulanamıyor.");
            }
            var user = await SupabaseAuthClient.ReadAsync(response, Context.RequestAborted);
            if (!user.TryGetProperty("id", out var id) || !Guid.TryParse(id.GetString(), out var userId))
                return AuthenticateResult.Fail("Kullanıcı kimliği geçersiz.");
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            if (user.TryGetProperty("email", out var email) && email.GetString() is { } value)
                claims.Add(new(ClaimTypes.Email, value));
            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or System.Text.Json.JsonException ||
            (ex is OperationCanceledException && !Context.RequestAborted.IsCancellationRequested))
        {
            _serviceUnavailable = true;
            Logger.LogWarning("Supabase kullanıcı doğrulaması tamamlanamadı.");
            return AuthenticateResult.Fail("Kimlik doğrulama servisine ulaşılamadı.");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = _serviceUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status401Unauthorized;
        return Response.WriteAsJsonAsync(new
        {
            message = _serviceUnavailable
                ? "Kimlik doğrulama servisine ulaşılamadı. Tekrar deneyin."
                : "Bu işlem için giriş yapmalısınız."
        }, Context.RequestAborted);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Response.WriteAsJsonAsync(new { message = "Bu işlem için yetkiniz yok." }, Context.RequestAborted);
    }

}
