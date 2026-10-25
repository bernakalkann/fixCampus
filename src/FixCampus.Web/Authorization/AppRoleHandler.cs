using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FixCampus.Web.Authorization;

public sealed class AppRoleRequirement(params string[] allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlyList<string> AllowedRoles { get; } = Array.AsReadOnly(allowedRoles);
}

/// <summary>Application roles come from Profiller, never from caller headers or Auth metadata.</summary>
public sealed class AppRoleHandler(IProfileRoleReader roles, ILogger<AppRoleHandler> logger)
    : AuthorizationHandler<AppRoleRequirement>
{
    internal static readonly object VerifiedRoleKey = new();
    internal static readonly object RoleServiceUnavailableKey = new();

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AppRoleRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            context.Resource is not HttpContext http ||
            !Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return;

        string? role;
        try
        {
            role = await roles.ReadAsync(userId, http.RequestAborted);
        }
        catch (Exception ex) when (ex is DbException or TimeoutException or InvalidOperationException ||
            (ex is OperationCanceledException && !http.RequestAborted.IsCancellationRequested))
        {
            logger.LogWarning("Kullanıcının uygulama rolü doğrulanamadı.");
            http.Items[RoleServiceUnavailableKey] = true;
            context.Fail(new AuthorizationFailureReason(this, "Rol servisine ulaşılamadı."));
            return;
        }

        if (role is not null && requirement.AllowedRoles.Contains(role, StringComparer.Ordinal))
        {
            http.Items[VerifiedRoleKey] = role;
            context.Succeed(requirement);
        }
    }
}
