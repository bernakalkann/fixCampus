using FixCampus.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

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
Console.WriteLine("PASS: migration, snapshot, tekrar edilebilir seed, ilişkiler ve canlı veritabanı okuma.");

static void Require(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
