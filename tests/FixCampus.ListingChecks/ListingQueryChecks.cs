using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.EntityFrameworkCore;

internal static class ListingQueryChecks
{
    public static void Run()
    {
        var category = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        var rows = new[]
        {
            new Ilan { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), KategoriId = category,
                Tur = "Kayip", Baslik = "USB %_ kablo", Yer = "Kantin", OlusturulmaTarihi = time },
            new Ilan { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), KategoriId = category,
                Tur = "Kayip", Aciklama = "USB %_ adaptör", OlusturulmaTarihi = time },
            new Ilan { KategoriId = category, Tur = "Kayip", Baslik = "USB %_", Durum = "Gizli" },
            new Ilan { KategoriId = Guid.NewGuid(), Tur = "Kayip", Baslik = "USB %_" },
            new Ilan { KategoriId = category, Tur = "Sosyal", Baslik = "USB %_", BulusmaTarihi = time.AddDays(1) }
        }.AsQueryable();
        var filtered = ListingQueries.Filter(rows, false, " Kayip ", category, " USB %_ ");
        Require(filtered.Count() == 2, "Birleşik filtreler/gizli ilan/literal arama hatalı.");
        var first = ListingQueries.Page(filtered, 1, 1).Single();
        var second = ListingQueries.Page(filtered, 2, 1).Single();
        Require(first.Id.CompareTo(second.Id) < 0, "Eşit tarihlerde sıralama kararsız.");
        Require(!ListingQueries.Page(filtered, int.MaxValue, 50).Any(), "Büyük sayfa taşması oluştu.");
        Require(ListingQueries.Normalize(null, null) == (1, 12), "Varsayılan sayfalama hatalı.");
        Require(ListingQueries.Normalize(-2, 0) == (1, 12), "Alt sınırlar hatalı.");
        Require(ListingQueries.Normalize(2, 999) == (2, 50), "Üst sınır hatalı.");
        Require(ListingQueries.Filter(rows, false, null, Guid.Empty, " ").Count() == 4,
            "Boş filtreler tüm görünür ilanları döndürmeli.");

        using var db = new FixCampusDbContext(new DbContextOptionsBuilder<FixCampusDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=unused;Username=test;Password=test").Options);
        var sql = ListingQueries.Page(ListingQueries.Filter(db.Ilanlar, true,
            "Kayip", category, @"USB %_\"), 2, 3).ToQueryString();
        Require(sql.Contains("ILIKE") && sql.Contains("LIMIT") && sql.Contains("OFFSET"),
            "PostgreSQL filtre/sayfalama SQL'e çevrilemedi.");
        Require(sql.Contains(@"\%") && sql.Contains(@"\_"), "LIKE jokerleri kaçırılmadı.");
        var multiWordSql = ListingQueries.Filter(db.Ilanlar, true, null, null,
            "istatistik elektronik kütüphane istatistik").ToQueryString();
        Require(multiWordSql.Contains("ILIKE ALL"), "Arama kelimeleri tek ALL koşuluna çevrilmedi.");
        Require(!multiWordSql.Contains("lower("), "PostgreSQL ILIKE sorgusunda gereksiz LOWER var.");
        Console.WriteLine("PASS: Berna hafta 4 ortak sorgular, kararlı sayfalama, taşma ve PostgreSQL SQL çevirisi.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
