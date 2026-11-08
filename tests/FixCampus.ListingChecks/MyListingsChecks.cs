using System.Reflection;
using System.Security.Claims;
using FixCampus.Web.Contracts;
using FixCampus.Web.Controllers;
using FixCampus.Web.Models;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

internal static class MyListingsChecks
{
    public static async Task RunAsync()
    {
        var first=Guid.NewGuid(); var second=Guid.NewGuid();
        ListingSummaryResponse Item(Guid owner,string type) => new(Guid.NewGuid(),owner,"Test",Guid.NewGuid(),"Kategori","Başlık","Açıklama",type,"Yer",null,"Aktif",DateTimeOffset.UtcNow,0,null);
        var rows=new[] { Item(first,"Sosyal"), Item(first,"Kayip"), Item(second,"Buluntu") };
        var service=DispatchProxy.Create<IListingService, MyListingsServiceProbe>();
        var probe=(MyListingsServiceProbe)(object)service;
        Guid requested=Guid.Empty; int calls=0;
        probe.Read=id=> { calls++; requested=id; return rows.Where(x=>x.SahipId==id).ToArray(); };
        IlanlarController Controller(Guid? id) => new(service,NullLogger<IlanlarController>.Instance) {
            ControllerContext=new ControllerContext { HttpContext=new DefaultHttpContext {
                User=new ClaimsPrincipal(new ClaimsIdentity(id.HasValue ? new[] { new Claim(ClaimTypes.NameIdentifier,id.Value.ToString()) } : Array.Empty<Claim>(),"test")) } } };
        void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
        var c=Controller(first);
        c.Request.QueryString=new QueryString("?userId="+second);
        var vm=(MyListingsViewModel)((ViewResult)await c.Benim()).Model!;
        Check(requested==first && vm.Listings.Count==2 && vm.TotalCount==2,"İlanlarım kimliği oturumdan alınmadı.");
        vm=(MyListingsViewModel)((ViewResult)await Controller(second).Benim()).Model!;
        Check(requested==second && vm.Listings.Count==1 && vm.Listings.All(x=>x.SahipId==second),"İkinci hesabın ekranı hatalı.");
        vm=(MyListingsViewModel)((ViewResult)await Controller(first).Benim("Kayip")).Model!;
        Check(vm.Listings.Single().Tur=="Kayip" && vm.TotalCount==2 && vm.SocialCount==1 && vm.LostFoundCount==1,"Filtre sayaçları değiştirdi.");
        vm=(MyListingsViewModel)((ViewResult)await Controller(first).Benim("Buluntu")).Model!;
        Check(vm.Listings.Count==0 && vm.ErrorMessage==null,"Boş filtre sonucu hata sayıldı.");
        var before=calls;
        Check(await Controller(null).Benim() is ChallengeResult && await Controller(Guid.Empty).Benim() is ChallengeResult,"Kimliksiz oturum kabul edildi.");
        Check(await Controller(first).Benim("Root") is BadRequestObjectResult && calls==before,"Geçersiz filtre sorguya gönderildi.");
        probe.Read=_=>throw new TimeoutException();
        c=Controller(first); vm=(MyListingsViewModel)((ViewResult)await c.Benim("Kayip")).Model!;
        Check(c.Response.StatusCode==503 && vm.ErrorMessage!=null && vm.SelectedTur=="Kayip","Bağlantı hatası boş liste gibi gösterildi.");
        Console.WriteLine("PASS: gerçek İlanlarım controller akışı, iki oturum kimliği, sahte userId, filtre/sayaç, boş sonuç ve 503 hata durumu.");
    }
}
public class MyListingsServiceProbe : DispatchProxy
{
    public Func<Guid,IReadOnlyList<ListingSummaryResponse>> Read { get; set; } = null!;
    protected override object? Invoke(MethodInfo? method,object?[]? args) => method?.Name=="GetUserListingsAsync"
        ? Task.FromResult(Read((Guid)args![0]!)) : throw new NotSupportedException(method?.Name);
}