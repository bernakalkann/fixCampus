using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.EntityFrameworkCore;

internal static class ExpiryChecks
{
    public static void Run()
    {
        var now=new DateTimeOffset(2030,1,1,12,0,0,TimeSpan.Zero);
        Ilan Social(DateTimeOffset? date)=>new(){Tur="Sosyal",BulusmaTarihi=date,Durum="Aktif"};
        var past=Social(now.AddTicks(-1)); var equal=Social(now);var future=Social(now.AddTicks(1));
        var sameInstant=Social(now.ToOffset(TimeSpan.FromHours(3)));var missing=Social(null);
        var hidden=Social(now.AddDays(1)); hidden.Durum="Gizli";
        var lost=new Ilan{Tur="Kayip",Durum="Aktif"};var found=new Ilan{Tur="Buluntu",Durum="Aktif"};
        var rows=new[]{past,equal,future,sameInstant,missing,hidden,lost,found}.AsQueryable();
        var visible=ListingQueries.Filter(rows,false,null,null,null,visibleAtUtc:now).ToArray();
        if(!visible.SequenceEqual(new[]{future,lost,found}))throw new Exception("Süre sınırı, UTC, tarih yokluğu veya kayıp/buluntu kuralı hatalı.");
        var social=ListingQueries.Filter(rows,false,"Sosyal",null,null,visibleAtUtc:now);
        if(social.Count()!=1||ListingQueries.Page(social,1,1).Single()!=future)throw new Exception("Süre filtresi sayfalamadan önce uygulanmadı.");
        if(rows.Count()!=8||past.Durum!="Aktif")throw new Exception("Süre kontrolü kayıt değiştirdi.");
        using var db=new FixCampusDbContext(new DbContextOptionsBuilder<FixCampusDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=test;Password=test").Options);
        var sql=ListingQueries.Filter(db.Ilanlar,true,null,null,null,visibleAtUtc:now).ToQueryString();
        if(!sql.Contains("BulusmaTarihi")||!sql.Contains(">"))throw new Exception("Süre filtresi PostgreSQL SQL'e çevrilmedi.");
        Console.WriteLine("PASS: süre kuralı geçmiş/eşit/gelecek, UTC eşdeğerliği, tarihsiz sosyal, kayıp/buluntu ve sayfalama.");
    }
}