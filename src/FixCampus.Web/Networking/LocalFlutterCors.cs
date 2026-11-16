namespace FixCampus.Web.Networking;

public static class LocalFlutterCors
{
    private const string Policy = "LocalFlutter";

    public static void AddLocalFlutterCors(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment()) return;

        builder.Services.AddCors(options => options.AddPolicy(Policy, policy => policy
            .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Authorization", "Content-Type", "Accept")));
    }

    public static void UseLocalFlutterCors(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"),
                api => api.UseCors(Policy));
    }
}
