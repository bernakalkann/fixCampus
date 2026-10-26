using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using FixCampus.Web.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FixCampus.Web.Auth;

public sealed class SupabaseCookieEvents(SupabaseAuthClient auth, IProfileRoleReader roles)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var token = context.Properties.GetTokenValue("access_token");
        if (context.Principal?.Identity is not ClaimsIdentity identity ||
            !Guid.TryParse(context.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            string.IsNullOrWhiteSpace(token))
        {
            context.RejectPrincipal();
            return;
        }

        try
        {
            // Both the Supabase session and the current database role must remain valid.
            using var response = await auth.SendAsync(HttpMethod.Get, "user", token: token,
                ct: context.HttpContext.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                context.RejectPrincipal();
                return;
            }
            var user = await SupabaseAuthClient.ReadAsync(response, context.HttpContext.RequestAborted);
            if (!user.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
                !Guid.TryParse(id.GetString(), out var verifiedId) || verifiedId != userId)
            {
                context.RejectPrincipal();
                return;
            }

            var role = await roles.ReadAsync(userId, context.HttpContext.RequestAborted);
            if (role is not (AppRoles.Student or AppRoles.Administrator))
            {
                context.RejectPrincipal();
                return;
            }

            var oldRoles = identity.FindAll(ClaimTypes.Role).ToArray();
            if (oldRoles.Length != 1 || oldRoles[0].Value != role)
            {
                var updated = new ClaimsIdentity(identity);
                foreach (var claim in updated.FindAll(ClaimTypes.Role).ToArray()) updated.RemoveClaim(claim);
                updated.AddClaim(new Claim(ClaimTypes.Role, role));
                context.ReplacePrincipal(new ClaimsPrincipal(updated));
                context.ShouldRenew = true;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or DbException or TimeoutException or
            InvalidOperationException or JsonException ||
            (ex is OperationCanceledException && !context.HttpContext.RequestAborted.IsCancellationRequested))
        {
            // A temporary failure must not preserve an old administrator permission.
            context.RejectPrincipal();
        }
    }
}
