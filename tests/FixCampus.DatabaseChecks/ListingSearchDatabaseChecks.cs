using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers.Api;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

internal static class ListingSearchDatabaseChecks
{
    public static async Task RunAsync(FixCampusDbContext db, Guid owner, Guid other,
        ListingsController api, Kategori social)
    {
        const string marker = "HAFTA4_QUERY";
        var category = new Kategori { Ad = "Elektronik", Tur = "Kayip" };
        db.Kategoriler.Add(category);
        var time = DateTimeOffset.UtcNow;
        Ilan Row(int id, string title, string place = "Kantin", string status = "Aktif") => new()
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{id:D12}"),
            SahipId = id % 2 == 0 ? other : owner, KategoriId = category.Id,
            Baslik = marker + " " + title, Aciklama = "Vize hazırlık", Yer = place,
            Tur = "Kayip", Durum = status, OlusturulmaTarihi = time
        };
        var rows = new[] { Row(1, "İstatistik USB %_", "Kütüphane"),
            Row(2, "HALI SAHA"), Row(3, @"Kablo\USB"), Row(4, "USB %_", status: "Gizli"),
            Row(5, "Sosyal tamamlandı"), Row(6, "Sosyal aktif") };
        foreach (var row in rows.Skip(4))
        {
            row.Tur = "Sosyal"; row.KategoriId = social.Id;
            row.BulusmaTarihi = time.AddDays(2);
        }
        rows[4].Durum = "Cozuldu";
        db.Ilanlar.AddRange(rows);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        async Task<ListingPageResponse> Page(string search, int page = 1, int size = 2,
            bool lost = false, bool activeSocial = false) =>
            (ListingPageResponse)((OkObjectResult)await api.PagedList(q: marker + " " + search,
                sayfa: page, sayfaBoyutu: size, kayipBuluntu: lost,
                aktifSosyal: activeSocial)).Value!;

        var turkish = await Page("istatistik elektronik kütüphane");
        Require(turkish.TotalCount == 1 && turkish.Items.Single().Id == rows[0].Id,
            "Türkçe/çok kelime/kategori/konum araması eşleşmedi.");
        Require((await Page("halı saha")).Items.Single().Id == rows[1].Id,
            "Türkçe I/ı eşleşmesi hatalı.");
        Require((await Page("%_")).Items.Single().Id == rows[0].Id,
            "Literal LIKE joker araması veya gizli kayıt filtresi hatalı.");
        Require((await Page(@"\USB")).Items.Single().Id == rows[2].Id,
            "Ters eğik çizgi kaçışı hatalı.");
        var first = await Page("", lost: true);
        var second = await Page("", 2, lost: true);
        Require(first.TotalCount == 3 && first.TotalPages == 2 && first.Items.Count == 2 && second.Items.Count == 1,
            "Toplam kayıt/sayfa bilgisi hatalı.");
        Require(first.Items.Select(x => x.Id).SequenceEqual(rows.Take(2).Select(x => x.Id)) &&
            second.Items.Single().Id == rows[2].Id, "Eşit tarihli kayıtların sırası/sayfaları hatalı.");
        Require((await Page("", int.MaxValue, 50)).Items.Count == 0, "Büyük sayfa boş değil.");
        Require((await Page("olmayan-kelime")).TotalCount == 0, "Boş arama hatalı.");
        var active = await Page("", size: 50, activeSocial: true);
        Require(active.TotalCount == 4 && active.Items.All(x => x.Id != rows[4].Id),
            "Tamamlanan sosyal ilan aktif akışta gösterildi.");

        var web = new ListingService(db, new ListingValidator(db), NullLogger<ListingService>.Instance);
        var fromWeb = await web.GetListingsAsync(null, category.Id, marker + " istatistik elektronik", 1, 12);
        Require(fromWeb.Items.Single().Id == turkish.Items.Single().Id,
            "Web ve mobil API aynı veritabanı kaydını döndürmüyor.");
        Console.WriteLine("PASS: gerçek PostgreSQL üzerinde Türkçe/çok kelime/kategori arama, joker kaçışları, sayfalama ve web-mobil ortak kayıt.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
