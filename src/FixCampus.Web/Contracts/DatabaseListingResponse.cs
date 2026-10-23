namespace FixCampus.Web.Contracts;

public sealed record DatabaseListingResponse(
    Guid Id,
    string Baslik,
    string Aciklama,
    string Tur,
    string Yer,
    string Kategori,
    string Durum,
    DateTimeOffset OlusturulmaTarihi,
    string DataSource = "database",
    bool IsPersisted = true);
