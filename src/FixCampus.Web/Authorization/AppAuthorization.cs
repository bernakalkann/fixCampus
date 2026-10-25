using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace FixCampus.Web.Authorization;

public static class AppAuthorization
{
    public static IServiceCollection AddCampusAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IProfileRoleReader, ProfileRoleReader>();
        services.AddScoped<IAuthorizationHandler, AppRoleHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, RoleAuthorizationResultHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.StudentAccess, policy => policy
                .AddAuthenticationSchemes("Supabase")
                .RequireAuthenticatedUser()
                .AddRequirements(new AppRoleRequirement(AppRoles.Student, AppRoles.Administrator)));
            options.AddPolicy(AppPolicies.AdministratorAccess, policy => policy
                .AddAuthenticationSchemes("Supabase")
                .RequireAuthenticatedUser()
                .AddRequirements(new AppRoleRequirement(AppRoles.Administrator)));
        });
        return services;
    }
}

public sealed class RoleAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context,
        AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (context.Items.ContainsKey(AppRoleHandler.RoleServiceUnavailableKey))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return context.Response.WriteAsJsonAsync(
                new { message = "Yetkiniz doğrulanamadı. Bağlantıyı kontrol edip tekrar deneyin." },
                context.RequestAborted);
        }
        return _defaultHandler.HandleAsync(next, context, policy, result);
    }
}
