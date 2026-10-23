namespace FixCampus.Web.Models.Entities;

public class Sikayet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IlanId { get; set; }
    public Guid BildirenId { get; set; }
    public string Neden { get; set; } = string.Empty;
    public string Durum { get; set; } = "Bekliyor";
    public DateTimeOffset OlusturulmaTarihi { get; set; } = DateTimeOffset.UtcNow;
    public Ilan Ilan { get; set; } = null!;
    public Profil Bildiren { get; set; } = null!;
}
