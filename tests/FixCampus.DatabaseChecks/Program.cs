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

static void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
