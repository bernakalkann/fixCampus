namespace FixCampus.Web.Models.Entities;

public class Katilim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IlanId { get; set; }
    public Guid ProfilId { get; set; }
    public DateTimeOffset KatilimTarihi { get; set; } = DateTimeOffset.UtcNow;
    public Ilan Ilan { get; set; } = null!;
    public Profil Profil { get; set; } = null!;
}
