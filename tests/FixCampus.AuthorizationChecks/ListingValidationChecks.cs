using System.ComponentModel.DataAnnotations;
using FixCampus.Web.Contracts;
internal static class ListingValidationChecks
{
    public static void Run()
    {
        var now = new DateTimeOffset(2030,1,1,12,0,0,TimeSpan.Zero);
        using var services = new ServiceCollection().AddSingleton<TimeProvider>(new Clock(now)).BuildServiceProvider();
        SaveListingRequest Valid() => new() { Baslik="Başlık", Aciklama="Açıklama", Yer="Kampüs",
            KategoriId=Guid.NewGuid(), Tur="Sosyal", BulusmaTarihi=now.AddTicks(1) };
        List<ValidationResult> Errors(SaveListingRequest r)
        {
            var errors=new List<ValidationResult>();
            Validator.TryValidateObject(r,new ValidationContext(r,services,null),errors,true);
            return errors;
        }
        if (Errors(Valid()).Count!=0) throw new Exception("Gelecek tarih reddedildi.");
        foreach(var date in new[] {now.AddTicks(-1),now,now.ToOffset(TimeSpan.FromHours(3))})
        {
            var r=Valid(); r.BulusmaTarihi=date;
            if (!Errors(r).Any(x=>x.MemberNames.Contains("BulusmaTarihi"))) throw new Exception("Geçmiş/eşit tarih kabul edildi.");
        }
        foreach(var change in new Action<SaveListingRequest>[] {
            x=>x.Baslik=" a ", x=>x.Baslik=new string('a',151), x=>x.Aciklama="  ",
            x=>x.Aciklama=new string('a',3001), x=>x.Yer="  ", x=>x.Yer=new string('a',201),
            x=>x.KategoriId=Guid.Empty, x=>x.Tur="Root", x=>x.BulusmaTarihi=null })
        {
            var r=Valid(); change(r);
            if(Errors(r).Count==0) throw new Exception("Geçersiz alan kabul edildi.");
        }
        var max=Valid(); max.Baslik=new string('a',150); max.Aciklama=new string('a',3000); max.Yer=new string('a',200);
        if(Errors(max).Count!=0) throw new Exception("Geçerli üst sınır reddedildi.");
        Console.WriteLine("PASS: ilan alan sınırları, boşluk, tarih sınır anı ve UTC eşdeğerliği.");
    }
    private sealed class Clock(DateTimeOffset now):TimeProvider { public override DateTimeOffset GetUtcNow()=>now; }
}