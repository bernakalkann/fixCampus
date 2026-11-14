using FixCampus.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Security.Claims;
using System.Text.Json;
using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;

if (args.Contains("--live-my-listings"))
{
    var configuration=new ConfigurationBuilder().AddUserSecrets(typeof(FixCampusDbContext).Assembly).Build();
    var settings=new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Supabase"))
        { Timeout=10,CommandTimeout=10,Pooling=false,IncludeErrorDetail=false };
    if(settings.Host!="aws-0-ap-southeast-2.pooler.supabase.com"||settings.Database!="postgres")
        throw new InvalidOperationException("Beklenmeyen hedef.");
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(55));var ct=timeout.Token;
    await using var liveDb=new FixCampusDbContext(new DbContextOptionsBuilder<FixCampusDbContext>().UseNpgsql(settings.ConnectionString).Options);
    try
    {
        await liveDb.Database.OpenConnectionAsync(ct);
        var profiles=await liveDb.Profiller.AsNoTracking().Where(x=>x.Rol=="Ogrenci").OrderBy(x=>x.Id).Select(x=>x.Id).Take(2).ToListAsync(ct);
        Require(profiles.Count==2,"İki öğrenci profili gerekli.");
        await using var tx=await liveDb.Database.BeginTransactionAsync(ct);
        try { await MyListingsDatabaseChecks.RunAsync(liveDb,profiles[0],profiles[1],ct); }
        finally { await tx.RollbackAsync(CancellationToken.None); }
        Console.WriteLine("PASS: canlı Supabase işlemi geri alındı; kalıcı test kaydı yok. Doğrudan sorgu/servis testi, şifreyle oturum testi değildir.");
    }
    catch(Exception e) { Console.Error.WriteLine("FAIL live İlanlarım: "+e.GetType().Name+" / "+e.InnerException?.GetType().Name);Environment.ExitCode=1; }
    return;
}
if (args.Contains("--live-read-only"))
{
    var configuration = new ConfigurationBuilder().AddUserSecrets(typeof(FixCampusDbContext).Assembly)
        .AddEnvironmentVariables().Build();
    var configured = configuration.GetConnectionString("Supabase")
        ?? throw new InvalidOperationException("Supabase bağlantısı tanımlı değil.");
    var settings = new NpgsqlConnectionStringBuilder(configured) { Timeout = 10, CommandTimeout = 10 };
    try
    {
        await using var liveDb = new FixCampusDbContext(new DbContextOptionsBuilder<FixCampusDbContext>()
            .UseNpgsql(settings.ConnectionString).Options);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await liveDb.Database.OpenConnectionAsync(timeout.Token);
        Console.WriteLine($"PASS: canlı Supabase bağlantısı; {await liveDb.Profiller.CountAsync(timeout.Token)} profil, " +
            $"{await liveDb.Kategoriler.CountAsync(timeout.Token)} kategori, {await liveDb.Ilanlar.CountAsync(timeout.Token)} ilan.");
        Console.WriteLine("Migration: " + string.Join(", ", await liveDb.Database.GetAppliedMigrationsAsync(timeout.Token)));
        Require(!liveDb.Database.HasPendingModelChanges(), "Model/snapshot uyuşmazlığı.");
        Console.WriteLine("PASS: model ve snapshot uyumlu; veritabanında değişiklik yapılmadı.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Canlı bağlantı başarısız: " + ex.GetType().Name +
            (ex.InnerException is null ? "" : " / " + ex.InnerException.GetType().Name));
        Environment.ExitCode = 1;
    }
    return;
}

SaveListingRequest ValidListing() => new()
{
    Baslik = " Test ilanı ", Aciklama = " Açıklama ", Yer = " Kampüs ",
    KategoriId = Guid.NewGuid(), Tur = "Kayip"
};
bool Valid(SaveListingRequest request) => Validator.TryValidateObject(request,
    new ValidationContext(request), new List<ValidationResult>(), true);
Require(Valid(ValidListing()), "Geçerli kayıp ilanı reddedildi.");
foreach (var change in new Action<SaveListingRequest>[]
{
    x => x.Baslik = "  ", x => x.Baslik = new string('a', 151),
    x => x.Aciklama = " ", x => x.Aciklama = new string('a', 3001),
    x => x.Yer = " ", x => x.Yer = new string('a', 201),
    x => x.KategoriId = Guid.Empty, x => x.Tur = "Root", x => x.Tur = "Sosyal",
    x => x.BulusmaTarihi = DateTimeOffset.UtcNow
})
{
    var request = ValidListing();
    change(request);
    Require(!Valid(request), "Geçersiz ilan alanı kabul edildi.");
}
var socialRequest = ValidListing();
socialRequest.Tur = "Sosyal";
socialRequest.BulusmaTarihi = DateTimeOffset.UtcNow.AddDays(1);
Require(Valid(socialRequest), "Tarihli sosyal ilan reddedildi.");
var massAssignment = JsonSerializer.Deserialize<SaveListingRequest>("""
    {"baslik":"Test","sahipId":"11111111-1111-4111-8111-111111111111","durum":"Gizli"}
    """, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
Require(typeof(SaveListingRequest).GetProperty("SahipId") is null &&
    typeof(SaveListingRequest).GetProperty("Durum") is null && massAssignment.Baslik == "Test",
    "İstemci sahiplik/durum alanlarına erişebiliyor.");
Console.WriteLine("PASS: ilan DTO alan/tür/tarih doğrulamaları ve sahiplik/durum sözleşmesi.");

// Validate snapshot and generated PostgreSQL migration SQL without a database connection.
if (args.Contains("--model-only"))
{
    await using var modelDb = new FixCampusDbContext(new DbContextOptionsBuilder<FixCampusDbContext>()
        .UseNpgsql("Host=localhost;Database=fixcampus_test;Username=test;Password=test").Options);
    Require(!modelDb.Database.HasPendingModelChanges(), "Snapshot/model uyumsuz.");
    var migrator = modelDb.GetService<IMigrator>();
    var sql = migrator.GenerateScript("20261023000000_InitialCreate", "20261029150000_AddProfileEducation");
    Require(sql.Contains("ADD \"Bolum\" character varying(200)") &&
        sql.Contains("ADD \"Sinif\" character varying(30)"), "Migration alanları eksik.");
    Require(!sql.Contains("DROP TABLE") && !sql.Contains("DELETE FROM \"Profiller\""), "Migration mevcut verileri siliyor.");
    var rollback = migrator.GenerateScript("20261029150000_AddProfileEducation", "20261023000000_InitialCreate");
    Require(rollback.Contains("DROP COLUMN \"Bolum\"") && rollback.Contains("DROP COLUMN \"Sinif\""), "Rollback eksik.");
    Console.WriteLine("PASS: profil modeli, snapshot, PostgreSQL migration ve rollback SQL.");
    return;
}

var connection = Environment.GetEnvironmentVariable("FIXCAMPUS_TEST_DATABASE")
    ?? throw new InvalidOperationException("FIXCAMPUS_TEST_DATABASE gerekli.");
// Refuse shared/production databases. This creates records in a disposable test database.
var parsed = new NpgsqlConnectionStringBuilder(connection);
if (parsed.Database != "fixcampus_test")
    throw new InvalidOperationException("Yalnızca fixcampus_test adlı boş deneme veritabanı kullanılabilir.");
await using var db = new FixCampusDbContext(
    new DbContextOptionsBuilder<FixCampusDbContext>().UseNpgsql(connection).Options);
await using var conn = new NpgsqlConnection(connection);
await conn.OpenAsync();
await using (var check = new NpgsqlCommand(
    "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema IN ('public', 'auth') AND table_type = 'BASE TABLE')", conn))
{
    if ((bool)(await check.ExecuteScalarAsync())!)
        throw new InvalidOperationException("Test veritabanı boş olmalıdır.");
}
var userId = Guid.Parse("fdeef191-f6df-4fba-a4e1-d33992eab002");
// Minimal Auth identity fixture for local PostgreSQL, never used in Supabase.
await using (var command = new NpgsqlCommand(
    "CREATE SCHEMA auth; CREATE TABLE auth.users (id uuid PRIMARY KEY); INSERT INTO auth.users (id) VALUES (@id);", conn))
{
    command.Parameters.AddWithValue("id", userId);
    await command.ExecuteNonQueryAsync();
}
await db.Database.MigrateAsync();
Require(!db.Database.HasPendingModelChanges(), "Snapshot/model uyumsuz.");
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Demo:ProfileId"] = userId.ToString()
}).Build();
await DemoListingSeeder.SeedAsync(db, config);
await DemoListingSeeder.SeedAsync(db, config);
Require(await db.Ilanlar.CountAsync() == 1, "Seed tekrarında ilan çoğaldı.");
Require(await db.Profiller.CountAsync() == 1, "Seed tekrarında profil çoğaldı.");
Require(await db.Kategoriler.CountAsync() == 1, "Seed tekrarında kategori çoğaldı.");
db.ChangeTracker.Clear();
var read = await new DemoListingReader(db).ReadAsync();
Require(read is { IsPersisted: true, DataSource: "database" }, "Veritabanı ilanı okunamadı.");
Require(read!.Id == DemoListingIds.Listing && read.Kategori == "Genel", "İlan/kategori ilişkisi hatalı.");
await db.Ilanlar.Where(x => x.Id == DemoListingIds.Listing)
    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Baslik, "Veritabanında değiştirildi"));
read = await new DemoListingReader(db).ReadAsync();
Require(read!.Baslik == "Veritabanında değiştirildi", "Okuma canlı veriyi yansıtmıyor.");
await db.Ilanlar.Where(x => x.Id == DemoListingIds.Listing)
    .ExecuteUpdateAsync(x => x.SetProperty(y => y.Durum, "Gizli"));
Require(await new DemoListingReader(db).ReadAsync() is null, "Gizli ilan gösterildi.");

var controller = new ProfileController(db)
{
    ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "student@example.com")
            }, "test"))
        }
    }
};
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
async Task<JsonElement> UpdateProfile(string json)
{
    var request = JsonSerializer.Deserialize<UpdateProfileRequest>(json, jsonOptions)!;
    var result = await controller.Update(request, default) as OkObjectResult;
    Require(result is not null, "Profil güncellemesi başarısız.");
    return JsonSerializer.SerializeToElement(result!.Value, jsonOptions);
}
var profile = await UpdateProfile("""{"adSoyad":" Berna Kalkan ","bolum":" Bilgisayar Mühendisliği ","sinif":"4. Sınıf"}""");
Require(profile.GetProperty("bolum").GetString() == "Bilgisayar Mühendisliği", "Bölüm kaydedilmedi/trim uygulanmadı.");
Require(profile.GetProperty("sinif").GetString() == "4. Sınıf", "Sınıf kaydedilmedi.");
profile = await UpdateProfile("""{"adSoyad":"Berna Yeni"}""");
Require(profile.GetProperty("adSoyad").GetString() == "Berna Yeni", "Eski istemci ad güncellemesi bozuldu.");
Require(profile.GetProperty("bolum").GetString() == "Bilgisayar Mühendisliği" &&
    profile.GetProperty("sinif").GetString() == "4. Sınıf", "Gönderilmeyen alanlar silindi.");
profile = await UpdateProfile("""{"adSoyad":"Berna Yeni","sinif":"3. Sınıf"}""");
Require(profile.GetProperty("sinif").GetString() == "3. Sınıf" &&
    profile.GetProperty("bolum").GetString() == "Bilgisayar Mühendisliği", "Tek alan güncellemesi diğerini değiştirdi.");
profile = await UpdateProfile("""{"adSoyad":"Berna Yeni","bolum":null,"sinif":null}""");
Require(profile.GetProperty("bolum").GetString() == "Bilgisayar Mühendisliği" &&
    profile.GetProperty("sinif").GetString() == "3. Sınıf", "Null alanlar mevcut değerleri sildi.");
profile = await UpdateProfile("""{"adSoyad":"Berna Yeni","bolum":"","sinif":""}""");
Require(profile.GetProperty("bolum").GetString() == "" && profile.GetProperty("sinif").GetString() == "",
    "Boş metin ile temizleme başarısız.");
// Exercise first-profile INSERT as well as ON CONFLICT. Other users must stay untouched.
var otherId = Guid.NewGuid();
await using (var command = new NpgsqlCommand("INSERT INTO auth.users (id) VALUES (@id)", conn))
{
    command.Parameters.AddWithValue("id", otherId);
    await command.ExecuteNonQueryAsync();
}
controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
{
    new Claim(ClaimTypes.NameIdentifier, otherId.ToString())
}, "test"));
profile = await UpdateProfile("""{"adSoyad":"Diğer Öğrenci"}""");
Require(profile.GetProperty("bolum").ValueKind == JsonValueKind.Null &&
    profile.GetProperty("sinif").ValueKind == JsonValueKind.Null, "Yeni profilde alanlar opsiyonel değil.");
profile = await UpdateProfile("""{"adSoyad":"Diğer Öğrenci","bolum":"Matematik","sinif":"Hazırlık"}""");
Require(profile.GetProperty("sinif").GetString() == "Hazırlık", "Sınıf metin değeri kabul edilmedi.");
Require(await db.Profiller.AsNoTracking().Where(x => x.Id == userId).Select(x => x.Bolum).SingleAsync() == "",
    "Başka kullanıcının profili değiştirildi.");
Console.WriteLine("PASS: migration, snapshot, seed, ilişkiler, canlı okuma ve profil GET/PUT geriye uyumluluğu.");

// Full CRUD and ownership checks use only the disposable fixcampus_test fixture above.
using var mvcServices = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
ListingsController Listings(Guid owner) => new(db)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
    {
        RequestServices = mvcServices,
        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        { new Claim(ClaimTypes.NameIdentifier, owner.ToString()) }, "test"))
    } }
};
var lostCategory = new Kategori { Ad = "Test kayıp", Tur = "Kayip" };
var socialCategory = new Kategori { Ad = "Test sosyal", Tur = "Sosyal" };
db.Kategoriler.AddRange(lostCategory, socialCategory);
await db.SaveChangesAsync();
var saveRequest = ValidListing();
saveRequest.KategoriId = lostCategory.Id;
var created = await Listings(userId).Create(saveRequest, default) as CreatedAtActionResult;
Require(created?.Value is ListingResponse, "İlan oluşturulmadı/201 dönmedi.");
var saved = (ListingResponse)created!.Value!;
Require(saved.SahipId == userId && saved.Durum == "Aktif" && saved.Baslik == "Test ilanı" &&
    saved.Aciklama == "Açıklama" && saved.Yer == "Kampüs", "Sahiplik/durum/trim hatalı.");
Require(created.ActionName == "Get" && (Guid)created.RouteValues!["id"]! == saved.Id,
    "201 Location yönlendirmesi hatalı.");
db.ChangeTracker.Clear();
Require(await Listings(userId).Get(saved.Id, default) is OkObjectResult { Value: ListingResponse }, "Detay okunamadı.");
saveRequest.Baslik = "Düzenlenen ilan";
Require(await Listings(otherId).Update(saved.Id, saveRequest, default) is ForbidResult, "Başkasının ilanı güncellendi.");
Require(await Listings(otherId).Delete(saved.Id, default) is ForbidResult, "Başkasının ilanı silindi.");
Require((await db.Ilanlar.AsNoTracking().SingleAsync(x => x.Id == saved.Id)).Baslik == "Test ilanı", "Yetkisiz işlem veriyi değiştirdi.");
Require(await Listings(userId).Update(saved.Id, saveRequest, default) is OkObjectResult, "Sahibi ilanı güncelleyemedi.");
db.ChangeTracker.Clear();
Require((await db.Ilanlar.AsNoTracking().SingleAsync(x => x.Id == saved.Id)).Baslik == "Düzenlenen ilan", "Güncelleme kalıcı değil.");
saveRequest.KategoriId = Guid.NewGuid();
var categoryResult = await Listings(userId).Create(saveRequest, default);
Require(categoryResult is ObjectResult { Value: ValidationProblemDetails { Status: 400 } categoryErrors } && categoryErrors.Errors.ContainsKey("KategoriId"), "Olmayan kategori kabul edildi.");
saveRequest.KategoriId = socialCategory.Id;
Require(await Listings(userId).Update(saved.Id, saveRequest, default) is ObjectResult { Value: ValidationProblemDetails { Status: 400 } typeErrors } && typeErrors.Errors.ContainsKey("KategoriId"), "Kategori/tür uyuşmazlığı kabul edildi.");
var missingId = Guid.NewGuid();
Require(await Listings(userId).Get(missingId, default) is NotFoundObjectResult &&
    await Listings(userId).Update(missingId, saveRequest, default) is NotFoundObjectResult &&
    await Listings(userId).Delete(missingId, default) is NotFoundObjectResult, "Olmayan ilan 404 dönmedi.");
saveRequest.Tur = "Sosyal";
saveRequest.BulusmaTarihi = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.FromHours(3));
var socialCreated = (CreatedAtActionResult)await Listings(userId).Create(saveRequest, default);
var socialSaved = (ListingResponse)socialCreated.Value!;
db.ChangeTracker.Clear();
Require((await db.Ilanlar.SingleAsync(x => x.Id == socialSaved.Id)).BulusmaTarihi ==
    saveRequest.BulusmaTarihi.Value.ToUniversalTime(), "Tarih UTC olarak kaydedilmedi.");
await MyListingsDatabaseChecks.RunAsync(db, userId, otherId, CancellationToken.None);
await ExpiryDatabaseChecks.RunAsync(db, userId);
await ListingSearchDatabaseChecks.RunAsync(db, userId, otherId, Listings(userId), socialCategory);

db.Katilimlar.Add(new Katilim { IlanId = saved.Id, ProfilId = otherId });
db.IlanFotograflari.Add(new IlanFotografi { IlanId = saved.Id, DosyaYolu = "test/photo.jpg" });
db.Sikayetler.Add(new Sikayet { IlanId = saved.Id, BildirenId = otherId, Neden = "Test" });
await db.SaveChangesAsync();
db.ChangeTracker.Clear();
Require(await Listings(userId).Delete(saved.Id, default) is NoContentResult, "Silme 204 dönmedi.");
Require(!await db.Ilanlar.AnyAsync(x => x.Id == saved.Id) &&
    !await db.Katilimlar.AnyAsync(x => x.IlanId == saved.Id) &&
    !await db.IlanFotograflari.AnyAsync(x => x.IlanId == saved.Id) &&
    !await db.Sikayetler.AnyAsync(x => x.IlanId == saved.Id), "İlan veya bağlı kayıtlar silinmedi.");
var socialEntity = await db.Ilanlar.SingleAsync(x => x.Id == socialSaved.Id);
socialEntity.Durum = "Gizli";
await db.SaveChangesAsync();
Require(await Listings(userId).Get(socialSaved.Id, default) is NotFoundObjectResult, "Gizli ilan detayda gösterildi.");
var list = (OkObjectResult)await Listings(userId).List(default);
Require(((List<ListingResponse>)list.Value!).All(x => x.Durum != "Gizli"), "Gizli ilan listede gösterildi.");
Require(await new CategoriesController(db).List(default) is OkObjectResult, "Kategori listesi alınamadı.");
Console.WriteLine("PASS: PostgreSQL ilan CRUD, kalıcılık, kategori, sahiplik, UTC, gizlilik ve bağlı kayıt silme.");

static void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

