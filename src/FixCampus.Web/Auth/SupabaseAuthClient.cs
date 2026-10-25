using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FixCampus.Web.Auth;

public sealed class SupabaseAuthClient(HttpClient http, IConfiguration configuration)
{
    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path,
        object? body = null, string? token = null, CancellationToken ct = default)
    {
        var url = configuration["Supabase:Url"];
        var key = configuration["Supabase:PublishableKey"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Supabase Auth yapılandırması eksik veya geçersiz.");
        using var request = new HttpRequestMessage(method, url.TrimEnd('/') + "/auth/v1/" + path);
        request.Headers.Add("apikey", key);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await http.SendAsync(request, ct);
    }

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
}
