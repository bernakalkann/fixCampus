using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FixCampus.Web.Auth;
using FixCampus.Web.Authorization;
using FixCampus.Web.Controllers.Api;
using FixCampus.Web.Data;
using FixCampus.Web.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Optional SELECT-only check against a configured project's existing profile.
if (args.Length == 2 && args[0] == "--live-role-read" && Guid.TryParse(args[1], out var existingUserId))
{
    var liveConfiguration = new ConfigurationBuilder()
        .AddUserSecrets(typeof(ProfileRoleReader).Assembly).AddEnvironmentVariables().Build();
    var connection = liveConfiguration.GetConnectionString("Supabase")
        ?? throw new InvalidOperationException("ConnectionStrings:Supabase eksik.");
    await using var db = new FixCampusDbContext(new DbContextOptionsBuilder<FixCampusDbContext>().UseNpgsql(connection).Options);
    var role = await new ProfileRoleReader(db).ReadAsync(existingUserId, CancellationToken.None);
    if (role is not (AppRoles.Student or AppRoles.Administrator)) throw new Exception("Existing profile has no valid application role.");
    Console.WriteLine($"PASS: live profile role SELECT ({role}); no writes.");
    return;
}

// Exercise the real HTTP authentication/authorization pipeline; external services are deterministic fakes.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Supabase:Url"] = "https://example.supabase.co",
    ["Supabase:PublishableKey"] = "test-public-key"
});
var roleStore = new TestRoleStore();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecurityDocumentTransformer>());
builder.Services.AddControllers().AddApplicationPart(typeof(AccessController).Assembly);
builder.Services.AddSingleton(new SupabaseAuthClient(new HttpClient(new TestAuthTransport()), builder.Configuration));
// Match the web app: MVC uses cookies; API endpoints must explicitly use Bearer.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });
builder.Services.AddRateLimiter(options => options.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter("auth-checks")));
builder.Services.AddCampusAuthorization();
builder.Services.Replace(ServiceDescriptor.Singleton<IProfileRoleReader>(roleStore));
await using var app = builder.Build();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// Check the real profile endpoint's authorization without executing shared-database SQL.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/profil")
    {
        await context.Response.WriteAsJsonAsync(new { userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) });
        return;
    }
    await next();
});
app.MapControllers();
app.MapOpenApi();
app.MapGet("/auth-only", () => Results.Ok())
    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Supabase" });
app.MapGet("/web-only", () => Results.Ok()).RequireAuthorization();
await app.StartAsync();
try
{
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = new Uri(address) };
    var cookieOptions = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
        .Get(CookieAuthenticationDefaults.AuthenticationScheme);
    var cookieTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, TestRoleStore.StudentId.ToString()) },
        CookieAuthenticationDefaults.AuthenticationScheme)), CookieAuthenticationDefaults.AuthenticationScheme);
    var webCookie = cookieOptions.Cookie.Name + "=" + cookieOptions.TicketDataFormat.Protect(cookieTicket);
    var count = 0;
    async Task Check(string path, string? token, HttpStatusCode expected, string? expectedRole = null, bool forgedHeaders = false, string? cookie = null, HttpMethod? method = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        if (token is not null) request.Headers.Authorization = new("Bearer", token);
        if (forgedHeaders)
        {
            request.Headers.Add("X-Role", AppRoles.Administrator);
            request.Headers.Add("X-User-Id", TestRoleStore.AdminId.ToString());
        }
        using var response = await http.SendAsync(request);
        if (response.StatusCode != expected)
            throw new Exception($"{path}: expected {(int)expected}, received {(int)response.StatusCode}.");
        if (expectedRole is not null)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (body.RootElement.GetProperty("role").GetString() != expectedRole ||
                !body.RootElement.GetProperty("allowed").GetBoolean())
                throw new Exception("Access response did not use the trusted profile role.");
        }
        if (expected is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!body.RootElement.TryGetProperty("message", out _)) throw new Exception("Expected a JSON access error.");
        }
        count++;
    }
    await Check("/api/durum", null, HttpStatusCode.OK);
    await Check("/api/yetki/ogrenci", null, HttpStatusCode.Unauthorized);
    await Check("/api/yetki/yonetici", null, HttpStatusCode.Unauthorized);
    await Check("/api/yetki/yonetici", "invalid", HttpStatusCode.Unauthorized);
    await Check("/api/yetki/ogrenci", "student", HttpStatusCode.OK, AppRoles.Student);
    await Check("/api/yetki/yonetici", "student", HttpStatusCode.Forbidden);
    await Check("/api/yetki/yonetici", "student", HttpStatusCode.Forbidden, forgedHeaders: true);
    await Check("/api/yetki/ogrenci", "admin", HttpStatusCode.OK, AppRoles.Administrator);
    await Check("/api/yetki/yonetici", "admin", HttpStatusCode.OK, AppRoles.Administrator);
    await Check("/api/yetki/ogrenci", "missing-profile", HttpStatusCode.Forbidden);
    await Check("/api/yetki/yonetici", "missing-profile", HttpStatusCode.Forbidden);
    await Check("/api/yetki/ogrenci", "unknown-role", HttpStatusCode.Forbidden);
    await Check("/auth-only", "missing-profile", HttpStatusCode.OK);
    roleStore.AdminRole = AppRoles.Student;
    await Check("/api/yetki/yonetici", "admin", HttpStatusCode.Forbidden);
    roleStore.AdminRole = AppRoles.Administrator;
    roleStore.Unavailable = true;
    await Check("/api/yetki/ogrenci", "student", HttpStatusCode.ServiceUnavailable);
    roleStore.Unavailable = false;
    await Check("/api/yetki/yonetici", "auth-unavailable", HttpStatusCode.ServiceUnavailable);
    await Check("/api/profil", "student", HttpStatusCode.OK);
    await Check("/api/profil", "student", HttpStatusCode.OK, method: HttpMethod.Put);
    await Check("/api/profil", null, HttpStatusCode.Unauthorized);
    await Check("/api/profil", "invalid", HttpStatusCode.Unauthorized);
    await Check("/api/profil", null, HttpStatusCode.Unauthorized, cookie: webCookie);
    await Check("/api/profil", null, HttpStatusCode.Unauthorized, cookie: webCookie, method: HttpMethod.Put);
    await Check("/api/profil", "auth-unavailable", HttpStatusCode.ServiceUnavailable);
    await Check("/api/auth/cikis", null, HttpStatusCode.Unauthorized, method: HttpMethod.Post);
    await Check("/api/auth/cikis", null, HttpStatusCode.Unauthorized, cookie: webCookie, method: HttpMethod.Post);
    await Check("/api/auth/cikis", "student", HttpStatusCode.NoContent, method: HttpMethod.Post);
    await Check("/web-only", null, HttpStatusCode.Redirect);
    await Check("/web-only", null, HttpStatusCode.OK, cookie: webCookie);
    using var document = JsonDocument.Parse(await http.GetStringAsync("/openapi/v1.json"));
    var root = document.RootElement;
    if (root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString() != "bearer")
        throw new Exception("Swagger Bearer scheme missing.");
    var paths = root.GetProperty("paths");
    foreach (var (path, method) in new[] { ("/api/yetki/ogrenci", "get"), ("/api/yetki/yonetici", "get"), ("/api/profil", "get"), ("/api/profil", "put"), ("/api/auth/cikis", "post") })
    {
        if (!paths.GetProperty(path).GetProperty(method).GetProperty("security")[0].TryGetProperty("Bearer", out _))
            throw new Exception("Protected operation missing Swagger authorization.");
        count++;
    }
    foreach (var (path, method) in new[] { ("/api/durum", "get"), ("/api/ornek/ilanlar", "get"), ("/api/auth/giris", "post"), ("/api/auth/kayit", "post"), ("/api/auth/yenile", "post") })
    {
        if (paths.GetProperty(path).GetProperty(method).TryGetProperty("security", out var security) && security.GetArrayLength() > 0)
            throw new Exception("Public operation unexpectedly requires Swagger authorization.");
        count++;
    }
    Console.WriteLine($"PASS: {count} HTTP authorization/OpenAPI checks (401/403/200/503, role changes, bootstrap, Cookie/Bearer separation, Bearer security).");
}
finally { await app.StopAsync(); }

sealed class TestRoleStore : IProfileRoleReader
{
    public static readonly Guid StudentId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid AdminId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public bool Unavailable;
    public string AdminRole = AppRoles.Administrator;
    public Task<string?> ReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (Unavailable) throw new TimeoutException("Test role store unavailable.");
        string? role = userId == StudentId ? AppRoles.Student : userId == AdminId ? AdminRole :
            userId == Guid.Parse("44444444-4444-4444-8444-444444444444") ? "Root" : null;
        return Task.FromResult(role);
    }
}
sealed class TestAuthTransport : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = request.Headers.Authorization?.Parameter;
        if (token == "auth-unavailable") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var id = token switch
        {
            "student" => TestRoleStore.StudentId,
            "admin" => TestRoleStore.AdminId,
            "missing-profile" => Guid.Parse("33333333-3333-4333-8333-333333333333"),
            "unknown-role" => Guid.Parse("44444444-4444-4444-8444-444444444444"),
            _ => Guid.Empty
        };
        if (id == Guid.Empty) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        // Even a claimed administrator role in Supabase metadata cannot override Profiller.Rol.
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            { id, email = "test@example.com", user_metadata = new { role = AppRoles.Administrator } }), Encoding.UTF8, "application/json")
        });
    }
}
