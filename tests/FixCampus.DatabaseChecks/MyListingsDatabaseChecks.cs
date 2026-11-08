using System.Security.Claims;
using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers.Api;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

internal static class MyListingsDatabaseChecks
{
    public static async Task RunAsync(FixCampusDbContext db, Guid first, Guid second, CancellationToken ct)
    {
        var social=await db.Kategoriler.AsNoTracking().FirstAsync(x=>x.Tur=="Sosyal",ct);
        var lost=await db.Kategoriler.AsNoTracking().FirstAsync(x=>x.Tur=="Kayip",ct);
        Ilan Row(Guid owner,Kategori category,string status="Aktif") => new() {
            Id=Guid.NewGuid(), SahipId=owner,KategoriId=category.Id,Tur=category.Tur,
            Baslik="[TEST] İlanlarım "+Guid.NewGuid().ToString("N"), Aciklama="Geçici test",
            Yer="Kampüs",Durum=status,BulusmaTarihi=category.Tur=="Sosyal" ? DateTimeOffset.UtcNow.AddDays(2):null };
        var a=Row(first,social);var b=Row(first,lost);var c=Row(second,lost);var hidden=Row(second,lost,"Gizli");
        db.Ilanlar.AddRange(a,b,c,hidden);await db.SaveChangesAsync(ct);db.ChangeTracker.Clear();
        var ids=new[]{a.Id,b.Id,c.Id,hidden.Id};
        async Task<List<ListingResponse>> Api(Guid owner,string? type=null)
        {
            var controller=new ListingsController(db) { ControllerContext=new ControllerContext { HttpContext=new DefaultHttpContext {
                User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,owner.ToString())},"test")) } } };
            return (List<ListingResponse>)((OkObjectResult)await controller.MyListings(type,ct)).Value!;
        }
        var web=new ListingService(db,new ListingValidator(db),NullLogger<ListingService>.Instance);
        foreach(var owner in new[]{first,second})
        {
            var expected=owner==first ? new[]{a.Id,b.Id} : new[]{c.Id};
            var api=await Api(owner);var view=await web.GetUserListingsAsync(owner,null,ct);
            Require(api.All(x=>x.SahipId==owner)&&view.All(x=>x.SahipId==owner),"Başka hesaba ait ilan sızdı.");
            Require(api.Where(x=>ids.Contains(x.Id)).Select(x=>x.Id).Order().SequenceEqual(expected.Order()),"API kayıt izolasyonu hatalı.");
            Require(view.Where(x=>ids.Contains(x.Id)).Select(x=>x.Id).Order().SequenceEqual(expected.Order()),"Web kayıt izolasyonu hatalı.");
            Require(!api.Any(x=>x.Id==hidden.Id)&&!view.Any(x=>x.Id==hidden.Id),"Gizli ilan sızdı.");
            var filtered=await Api(owner,"Sosyal");var webFiltered=await web.GetUserListingsAsync(owner,"Sosyal",ct);
            Require(filtered.All(x=>x.Tur=="Sosyal")&&webFiltered.All(x=>x.Tur=="Sosyal"),"Tür filtresi hatalı.");
            Require(filtered.Any(x=>x.Id==a.Id)==(owner==first)&&webFiltered.Any(x=>x.Id==a.Id)==(owner==first),"Tür filtresi sahipliği bozdu.");
        }
        db.Ilanlar.RemoveRange(await db.Ilanlar.Where(x=>ids.Contains(x.Id)).ToListAsync(ct));await db.SaveChangesAsync(ct);
        Require(!await db.Ilanlar.AnyAsync(x=>ids.Contains(x.Id),ct),"Test kayıtları temizlenmedi.");
        Console.WriteLine("PASS: gerçek PostgreSQL üzerinde iki profil için API/web İlanlarım sahiplik, tür ve gizli ilan izolasyonu.");
    }
    private static void Require(bool value,string message) { if(!value)throw new InvalidOperationException(message); }
}