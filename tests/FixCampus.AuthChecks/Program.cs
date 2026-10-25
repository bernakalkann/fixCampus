using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using FixCampus.Web.Auth;
using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

var transport = new FakeTransport();
var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Supabase:Url"] = "https://example.supabase.co",
    ["Supabase:PublishableKey"] = "test-public-key"
}).Build();
var client = new SupabaseAuthClient(new HttpClient(transport), configuration);
var controller = new AuthController(client);
Require(await controller.Login(new LoginRequest { Email = " student@example.com ", Password = "secret123" }, default) is OkObjectResult,
    "Login response was not successful.");
Require(transport.Path == "/auth/v1/token?grant_type=password", "Incorrect login endpoint.");
Require(JsonDocument.Parse(transport.Body!).RootElement.GetProperty("email").GetString() == "student@example.com", "Email was not trimmed.");
Require(transport.ApiKey == "test-public-key", "Missing API key.");
await controller.Register(new RegisterRequest { Email = "s@example.com", Password = "secret123", AdSoyad = " Berna Kalkan " }, default);
using (var json = JsonDocument.Parse(transport.Body!))
{
    Require(json.RootElement.GetProperty("data").GetProperty("ad_soyad").GetString() == "Berna Kalkan", "Signup name mismatch.");
    Require(!json.RootElement.GetProperty("data").TryGetProperty("role", out _), "Signup must not set privileged roles.");
}
transport.Status = HttpStatusCode.BadRequest;
Require(await controller.Login(new LoginRequest(), default) is ObjectResult { StatusCode: 400 }, "Upstream error not handled.");
transport.Status = HttpStatusCode.TooManyRequests;
Require(await controller.Refresh(new RefreshRequest { RefreshToken = "refresh" }, default) is ObjectResult { StatusCode: 429 }, "Rate limit not preserved.");
transport.Status = HttpStatusCode.NoContent;
controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
controller.Request.Headers.Authorization = "Bearer access-token";
Require(await controller.Logout(default) is NoContentResult, "Logout failed.");
Require(transport.Path == "/auth/v1/logout?scope=local" && transport.Token == "access-token", "Logout token/scope mismatch.");
var missing = new SupabaseAuthClient(new HttpClient(transport), new ConfigurationBuilder().Build());
Require(await new AuthController(missing).Login(new LoginRequest(), default) is ObjectResult { StatusCode: 503 }, "Missing settings must fail safely.");
// Exercise the actual authentication handler, including a forged/invalid token.
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(client);
services.AddAuthentication("Supabase").AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });
await using var provider = services.BuildServiceProvider();
async Task<AuthenticateResult> Authenticate(string? token)
{
    await using var scope = provider.CreateAsyncScope();
    var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    if (token is not null) context.Request.Headers.Authorization = "Bearer " + token;
    return await context.AuthenticateAsync("Supabase");
}
Require((await Authenticate(null)).None, "Anonymous request unexpectedly authenticated.");
transport.Status = HttpStatusCode.Unauthorized;
Require(!(await Authenticate("forged")).Succeeded, "Invalid token accepted.");
transport.Status = HttpStatusCode.OK;
transport.ResponseBody = "{\"id\":\"fdeef191-f6df-4fba-a4e1-d33992eab002\",\"email\":\"s@example.com\",\"user_metadata\":{\"role\":\"Yonetici\"}}";
var verified = await Authenticate("valid-test-token");
Require(verified.Succeeded, "Verified identity rejected.");
Require(!verified.Principal!.IsInRole("Yonetici"), "Untrusted metadata granted administrator role.");
// A real HttpClient timeout must become a controlled service error, not escape.
using var slowHttp = new HttpClient(new SlowTransport()) { Timeout = TimeSpan.FromMilliseconds(50) };
var slowClient = new SupabaseAuthClient(slowHttp, configuration);
var slowController = new AuthController(slowClient)
{ ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
slowController.Request.Headers.Authorization = "Bearer test-token";
Require(await slowController.Login(new LoginRequest(), default) is ObjectResult { StatusCode: 503 }, "Login timeout escaped.");
Require(await slowController.Refresh(new RefreshRequest(), default) is ObjectResult { StatusCode: 503 }, "Refresh timeout escaped.");
Require(await slowController.Logout(default) is ObjectResult { StatusCode: 503 }, "Logout timeout escaped.");
var slowServices = new ServiceCollection();
slowServices.AddLogging();
slowServices.AddSingleton(slowClient);
slowServices.AddAuthentication("Supabase").AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });
await using var slowProvider = slowServices.BuildServiceProvider();
await using var slowScope = slowProvider.CreateAsyncScope();
var slowContext = new DefaultHttpContext { RequestServices = slowScope.ServiceProvider };
slowContext.Request.Headers.Authorization = "Bearer test-token";
slowContext.Response.Body = new MemoryStream();
Require(!(await slowContext.AuthenticateAsync("Supabase")).Succeeded, "Timeout authenticated a user.");
await slowContext.ChallengeAsync("Supabase");
Require(slowContext.Response.StatusCode == 503, "Authentication timeout must return 503, not 401/500.");
Console.WriteLine("PASS: Auth flows, invalid identity and controlled timeout responses.");
static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
sealed class FakeTransport : HttpMessageHandler
{
    public string? Path, Body, ApiKey, Token;
    public string ResponseBody = "{\"access_token\":\"test\"}";
    public HttpStatusCode Status = HttpStatusCode.OK;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Path = request.RequestUri!.PathAndQuery;
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        ApiKey = request.Headers.GetValues("apikey").Single();
        Token = request.Headers.Authorization?.Parameter;
        return new HttpResponseMessage(Status) { Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json") };
    }
}

sealed class SlowTransport : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
