namespace FixCampus.Web.Models.Entities;

public class Ilan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SahipId { get; set; }
    public Guid KategoriId { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string Tur { get; set; } = string.Empty;
    public string Yer { get; set; } = string.Empty;
    public DateTimeOffset? BulusmaTarihi { get; set; }
    public string Durum { get; set; } = "Aktif";
    public DateTimeOffset OlusturulmaTarihi { get; set; } = DateTimeOffset.UtcNow;
    public Profil Sahip { get; set; } = null!;
    public Kategori Kategori { get; set; } = null!;
    public ICollection<Katilim> Katilimlar { get; set; } = new List<Katilim>();
    public ICollection<IlanFotografi> Fotograflar { get; set; } = new List<IlanFotografi>();
    public ICollection<Sikayet> Sikayetler { get; set; } = new List<Sikayet>();
}
