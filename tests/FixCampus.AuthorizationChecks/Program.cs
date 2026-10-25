using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FixCampus.Web.Auth;
using FixCampus.Web.Authorization;
using FixCampus.Web.Controllers.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
builder.Services.AddControllers().AddApplicationPart(typeof(AccessController).Assembly);
builder.Services.AddSingleton(new SupabaseAuthClient(new HttpClient(new TestAuthTransport()), builder.Configuration));
builder.Services.AddAuthentication("Supabase")
    .AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });
builder.Services.AddCampusAuthorization();
builder.Services.Replace(ServiceDescriptor.Singleton<IProfileRoleReader>(roleStore));
await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/auth-only", () => Results.Ok()).RequireAuthorization();
await app.StartAsync();
try
{
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var http = new HttpClient { BaseAddress = new Uri(address) };
    var count = 0;
    async Task Check(string path, string? token, HttpStatusCode expected, string? expectedRole = null, bool forgedHeaders = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
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
    Console.WriteLine($"PASS: {count} HTTP authorization checks (401/403/200/503, trusted role, role changes, bootstrap).");
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
