namespace FixCampus.Web.Models.Entities;

public class IlanFotografi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IlanId { get; set; }
    public string DosyaYolu { get; set; } = string.Empty;
    public DateTimeOffset YuklemeTarihi { get; set; } = DateTimeOffset.UtcNow;
    public Ilan Ilan { get; set; } = null!;
}
