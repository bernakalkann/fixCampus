namespace FixCampus.Web.Models.Entities;

public class Profil
{
    public Guid Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string? Bolum { get; set; }
    public string? Sinif { get; set; }
    public string Rol { get; set; } = "Ogrenci";
    public DateTimeOffset OlusturulmaTarihi { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Ilan> Ilanlar { get; set; } = new List<Ilan>();
    public ICollection<Katilim> Katilimlar { get; set; } = new List<Katilim>();
    public ICollection<Sikayet> Sikayetler { get; set; } = new List<Sikayet>();
}
