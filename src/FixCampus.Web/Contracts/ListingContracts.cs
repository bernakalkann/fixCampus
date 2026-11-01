using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Contracts;

public sealed class SaveListingRequest : IValidatableObject
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Baslik { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Aciklama { get; set; } = string.Empty;

    public Guid KategoriId { get; set; }

    [Required, RegularExpression("^(Sosyal|Kayip|Buluntu)$")]
    public string Tur { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Yer { get; set; } = string.Empty;

    public DateTimeOffset? BulusmaTarihi { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (KategoriId == Guid.Empty)
            yield return new("Kategori seçilmelidir.", [nameof(KategoriId)]);
        if (Baslik is not null && Baslik.Trim().Length < 2)
            yield return new("Başlık en az 2 karakter olmalıdır.", [nameof(Baslik)]);
        if (Tur == "Sosyal" && BulusmaTarihi is null)
            yield return new("Sosyal ilan için buluşma tarihi gereklidir.", [nameof(BulusmaTarihi)]);
        if (Tur is "Kayip" or "Buluntu" && BulusmaTarihi is not null)
            yield return new("Kayıp/buluntu ilanında buluşma tarihi kullanılamaz.", [nameof(BulusmaTarihi)]);
    }
}

public sealed record ListingResponse(Guid Id, Guid SahipId, Guid KategoriId, string Kategori,
    string Baslik, string Aciklama, string Tur, string Yer, DateTimeOffset? BulusmaTarihi,
    string Durum, DateTimeOffset OlusturulmaTarihi);

public sealed record CategoryResponse(Guid Id, string Ad, string Tur);
