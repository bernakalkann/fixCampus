using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers.Api;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

internal static class ExpiryDatabaseChecks
{
    public static async Task RunAsync(FixCampusDbContext db,Guid owner)
    {
        var now=DateTimeOffset.UtcNow;var clock=new FixedClock(now);
        var category=await db.Kategoriler.FirstAsync(x=>x.Tur=="Sosyal");
        Ilan Row(DateTimeOffset date)=>new(){SahipId=owner,KategoriId=category.Id,Tur="Sosyal",
            Baslik="Süre testi",Aciklama="Test",Yer="Kampüs",BulusmaTarihi=date,Durum="Aktif"};
        var past=Row(now.AddSeconds(-1));var equal=Row(now);var future=Row(now.AddDays(1));
        db.Ilanlar.AddRange(past,equal,future);await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var api=new ListingsController(db,clock);
        var list=(List<ListingResponse>)((OkObjectResult)await api.List()).Value!;
        var web=new ListingService(db,new ListingValidator(db,clock),NullLogger<ListingService>.Instance,clock);
        var webList=await web.GetListingsAsync(null,null,null,1,50);
        Check(list.Any(x=>x.Id==future.Id)&&!list.Any(x=>x.Id==past.Id||x.Id==equal.Id),"API süre filtresi hatalı.");
        Check(webList.Items.Any(x=>x.Id==future.Id)&&!webList.Items.Any(x=>x.Id==past.Id||x.Id==equal.Id),"Web süre filtresi hatalı.");
        var paged=(ListingPageResponse)((OkObjectResult)await api.PagedList(sayfa:1,sayfaBoyutu:50)).Value!;
        Check(paged.TotalCount==list.Count,"API sayfa toplamı süre filtresini kullanmadı.");
        var mine=await web.GetUserListingsAsync(owner);
        Check(mine.Any(x=>x.Id==past.Id)&&mine.Any(x=>x.Id==equal.Id),"Sahibinin süresi geçen ilanına erişimi kayboldu.");
        Check(await api.Get(past.Id,default) is OkObjectResult,"Süresi geçen kayıt silinmiş gibi gösterildi.");
        Check(await db.Ilanlar.CountAsync(x=>(x.Id==past.Id||x.Id==equal.Id)&&x.Durum=="Aktif")==2,"Süre kuralı veri değiştirdi.");
        db.Ilanlar.RemoveRange(await db.Ilanlar.Where(x=>x.Id==past.Id||x.Id==equal.Id||x.Id==future.Id).ToListAsync());
        await db.SaveChangesAsync();
        Console.WriteLine("PASS: PostgreSQL süre filtresi API/web/sayfa toplamı, kayıt korunması ve sahibinin erişimi.");
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    sealed class FixedClock(DateTimeOffset now):TimeProvider {public override DateTimeOffset GetUtcNow()=>now;}
}