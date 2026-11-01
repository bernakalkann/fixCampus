using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

if (args.Contains("--help"))
{
    Console.WriteLine("Kullanım: dotnet run --project tests/FixCampus.LiveChecks -- http://127.0.0.1:5168");
    Console.WriteLine("İki mevcut öğrenci test hesabı istenir. Şifreler/tokenlar kaydedilmez. Yalnızca bu testte oluşturulan ilanlar değiştirilir/silinir.");
    return;
}
var address = args.FirstOrDefault() ?? "http://127.0.0.1:5168";
if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != "http")
    throw new InvalidOperationException("Bu kontrol yalnızca yerel HTTP uygulaması içindir.");
using var http = new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(30) };
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var createdIds = new HashSet<string>();
string? firstToken = null, secondToken = null;
int checks = 0;

async Task<JsonElement> Send(HttpMethod method, string path, HttpStatusCode expected,
    string? token = null, object? body = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (token is not null) request.Headers.Authorization = new("Bearer", token);
    if (body is not null) request.Content = JsonContent.Create(body);
    using var response = await http.SendAsync(request);
    if (response.StatusCode != expected)
        throw new InvalidOperationException($"{method} {path}: beklenen {(int)expected}, gelen {(int)response.StatusCode}. Yanıt gövdesi güvenlik için yazdırılmadı.");
    checks++;
    if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    return default;
}
static void Require(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}
static string Password()
{
    if (Console.IsInputRedirected) throw new InvalidOperationException("Şifre girişi için normal Terminal kullanın.");
    var chars = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); }
        else if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
    }
    Console.WriteLine();
    return new string(chars.ToArray());
}
async Task<(string Token, string Id)> Login(int account)
{
    Console.Write($"{account}. öğrenci test hesabı e-posta: ");
    var email = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Şifre (ekranda görünmez): ");
    var password = Password();
    var login = await Send(HttpMethod.Post, "/api/auth/giris", HttpStatusCode.OK,
        body: new { email, password });
    var token = login.GetProperty("access_token").GetString()!;
    var profile = await Send(HttpMethod.Get, "/api/profil", HttpStatusCode.OK, token);
    Require(profile.GetProperty("rol").GetString() == "Ogrenci", "Bu kontrol iki öğrenci test hesabı ister.");
    return (token, profile.GetProperty("id").GetString()!);
}

Console.WriteLine("CANLI API kontrolü: İki mevcut öğrenci test hesabı kullanılır; şifre/token kaydedilmez.");
Console.WriteLine("Mevcut ilanlar değişmez. Yalnız bu çalıştırmada oluşturulan test ilanları temizlenir.");
try
{
    await Send(HttpMethod.Get, "/api/durum", HttpStatusCode.OK);
    await Send(HttpMethod.Get, "/swagger/index.html", HttpStatusCode.OK);
    var openapi = await Send(HttpMethod.Get, "/openapi/v1.json", HttpStatusCode.OK);
    foreach (var (path, method) in new[] { ("/api/ilanlar", "get"), ("/api/ilanlar", "post"),
        ("/api/ilanlar/{id}", "get"), ("/api/ilanlar/{id}", "put"), ("/api/ilanlar/{id}", "delete"), ("/api/kategoriler", "get") })
        Require(openapi.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("security")[0].TryGetProperty("Bearer", out _), "Swagger Bearer şeması eksik.");
    Console.WriteLine("PASS: çalışan Swagger/OpenAPI ve altı korunan işlem.");
    var first = await Login(1); firstToken = first.Token;
    var second = await Login(2); secondToken = second.Token;
    Require(first.Id != second.Id, "İki farklı hesap girilmelidir.");
    var categories = await Send(HttpMethod.Get, "/api/kategoriler", HttpStatusCode.OK, firstToken);
    var categoryIds = categories.EnumerateArray().GroupBy(x => x.GetProperty("tur").GetString()!)
        .ToDictionary(x => x.Key, x => x.First().GetProperty("id").GetString()!);
    foreach (var type in new[] { "Sosyal", "Kayip", "Buluntu" })
        Require(categoryIds.ContainsKey(type), $"{type} kategorisi eksik. docs/hafta-03-kategoriler.sql uygulanmalıdır.");
    await Send(HttpMethod.Get, "/api/ilanlar", HttpStatusCode.Unauthorized);
    await Send(HttpMethod.Get, "/api/kategoriler", HttpStatusCode.Unauthorized);
    var prefix = "[CRUD TEST " + Guid.NewGuid().ToString("N")[..8] + "]";
    Dictionary<string, object?> Body(string type) => new()
    {
        ["baslik"] = " " + prefix + " " + type + " ", ["aciklama"] = " Geçici canlı API testi ",
        ["yer"] = " Kampüs ", ["kategoriId"] = categoryIds[type], ["tur"] = type,
        ["bulusmaTarihi"] = type == "Sosyal" ? DateTimeOffset.UtcNow.AddDays(1) : null,
        // DTO bu alanları kabul etmemeli; sahip ve durum sunucudan gelmeli.
        ["sahipId"] = second.Id, ["durum"] = "Gizli"
    };
    foreach (var type in new[] { "Sosyal", "Kayip", "Buluntu" })
    {
        var body = Body(type);
        var created = await Send(HttpMethod.Post, "/api/ilanlar", HttpStatusCode.Created, firstToken, body);
        var id = created.GetProperty("id").GetString()!;
        createdIds.Add(id);
        Require(created.GetProperty("sahipId").GetString() == first.Id &&
            created.GetProperty("durum").GetString() == "Aktif", "Sahip/durum sunucudan alınmadı.");
        Require(created.GetProperty("baslik").GetString() == body["baslik"]!.ToString()!.Trim(), "Başlık trim uygulanmadı.");
        var path = "/api/ilanlar/" + id;
        await Send(HttpMethod.Get, path, HttpStatusCode.OK, firstToken);
        var list = await Send(HttpMethod.Get, "/api/ilanlar", HttpStatusCode.OK, firstToken);
        Require(list.EnumerateArray().Any(x => x.GetProperty("id").GetString() == id), "İlan listede bulunamadı.");
        body["baslik"] = prefix + " güncellendi";
        await Send(HttpMethod.Put, path, HttpStatusCode.Forbidden, secondToken, body);
        await Send(HttpMethod.Delete, path, HttpStatusCode.Forbidden, secondToken);
        var updated = await Send(HttpMethod.Put, path, HttpStatusCode.OK, firstToken, body);
        Require(updated.GetProperty("baslik").GetString() == body["baslik"]!.ToString(), "Güncelleme hatalı.");
        var readBack = await Send(HttpMethod.Get, path, HttpStatusCode.OK, firstToken);
        Require(readBack.GetProperty("baslik").GetString() == body["baslik"]!.ToString(), "Güncelleme kalıcı değil.");
        await Send(HttpMethod.Delete, path, HttpStatusCode.NoContent, firstToken);
        createdIds.Remove(id);
        await Send(HttpMethod.Get, path, HttpStatusCode.NotFound, firstToken);
        Console.WriteLine($"PASS: {type} — gerçek oturumlarla CRUD, kalıcılık ve ikinci hesap için 403.");
    }
    var invalid = Body("Kayip"); invalid["kategoriId"] = Guid.NewGuid();
    await Send(HttpMethod.Post, "/api/ilanlar", HttpStatusCode.BadRequest, firstToken, invalid);
    invalid = Body("Kayip"); invalid["kategoriId"] = categoryIds["Sosyal"];
    await Send(HttpMethod.Post, "/api/ilanlar", HttpStatusCode.BadRequest, firstToken, invalid);
    invalid = Body("Sosyal"); invalid["bulusmaTarihi"] = null;
    await Send(HttpMethod.Post, "/api/ilanlar", HttpStatusCode.BadRequest, firstToken, invalid);
    invalid = Body("Kayip"); invalid["baslik"] = " ";
    await Send(HttpMethod.Post, "/api/ilanlar", HttpStatusCode.BadRequest, firstToken, invalid);
    var missing = "/api/ilanlar/" + Guid.NewGuid();
    await Send(HttpMethod.Get, missing, HttpStatusCode.NotFound, firstToken);
    await Send(HttpMethod.Put, missing, HttpStatusCode.NotFound, firstToken, Body("Kayip"));
    await Send(HttpMethod.Delete, missing, HttpStatusCode.NotFound, firstToken);
    Console.WriteLine($"PASS: {checks} canlı HTTP isteği; Swagger, Auth, üç tür CRUD, sahiplik, 400/401/403/404 ve temizlik.");
}
finally
{
    foreach (var id in createdIds)
    {
        try { await Send(HttpMethod.Delete, "/api/ilanlar/" + id, HttpStatusCode.NoContent, firstToken); }
        catch { Console.Error.WriteLine("Test ilanı otomatik temizlenemedi: " + id); }
    }
    foreach (var token in new[] { firstToken, secondToken }.OfType<string>())
    {
        try { await Send(HttpMethod.Post, "/api/auth/cikis", HttpStatusCode.NoContent, token); }
        catch { Console.Error.WriteLine("Test oturumu kapatılamadı; uygulamadan çıkış yapın."); }
    }
}
