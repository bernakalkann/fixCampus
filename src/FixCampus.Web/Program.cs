using FixCampus.Web.Auth;
using FixCampus.Web.Authorization;
using FixCampus.Web.Data;
using FixCampus.Web.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecurityDocumentTransformer>());
builder.Services.AddHttpClient<SupabaseAuthClient>(client => client.Timeout = TimeSpan.FromSeconds(15));

// Hibrit Kimlik Doğrulama: Web Arayüzü için Çerez (Cookie), REST API için Supabase Bearer Şeması
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "FixCampus.Auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out var userId))
            {
                var roleReader = context.HttpContext.RequestServices.GetService<IProfileRoleReader>();
                if (roleReader != null)
                {
                    try
                    {
                        var currentRole = await roleReader.ReadAsync(userId, context.HttpContext.RequestAborted);
                        if (!string.IsNullOrEmpty(currentRole))
                        {
                            var existingRole = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
                            if (existingRole != currentRole)
                            {
                                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                                var oldClaim = identity.FindFirst(ClaimTypes.Role);
                                if (oldClaim != null) identity.RemoveClaim(oldClaim);
                                identity.AddClaim(new Claim(ClaimTypes.Role, currentRole));
                                context.ShouldRenew = true;
                            }
                        }
                    }
                    catch
                    {
                        // Veritabanı geçici ulaşılamıyorsa çerezi koru
                    }
                }
            }
        }
    };
})
.AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });

builder.Services.AddCampusAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddSingleton(TimeProvider.System);

// Keep the week-one demo usable without a database secret.
// Database operations require a configured connection; migrations are applied explicitly.
builder.Services.AddDbContext<FixCampusDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("Supabase");
    if (!string.IsNullOrWhiteSpace(connection))
        options.UseNpgsql(connection);
});
builder.Services.AddScoped<DemoListingReader>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Demo:Seed"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Demo kayıtları yalnızca Development ortamında hazırlanabilir.");
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Supabase")))
        throw new InvalidOperationException("ConnectionStrings:Supabase eksik. Önce User Secrets ayarını yapın.");
    await using var scope = app.Services.CreateAsyncScope();
    await DemoListingSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<FixCampusDbContext>(), builder.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "fixCampus API v1");
        options.DocumentTitle = "fixCampus API";
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
