namespace FixCampus.Web.Models.Entities;

public class Kategori
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Ad { get; set; } = string.Empty;
    public string Tur { get; set; } = string.Empty;
    public ICollection<Ilan> Ilanlar { get; set; } = new List<Ilan>();
}
