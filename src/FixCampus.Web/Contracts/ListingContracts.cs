using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Contracts;

public sealed class SaveListingRequest : IValidatableObject
{
    [Required(ErrorMessage = "Başlık zorunludur."), StringLength(150, MinimumLength = 2, ErrorMessage = "Başlık 2–150 karakter olmalıdır.")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur."), StringLength(3000, ErrorMessage = "Açıklama en fazla 3000 karakter olabilir.")]
    public string Aciklama { get; set; } = string.Empty;

    public Guid KategoriId { get; set; }

    [Required(ErrorMessage = "İlan türü zorunludur."), RegularExpression("^(Sosyal|Kayip|Buluntu)$", ErrorMessage = "İlan türü Sosyal, Kayip veya Buluntu olmalıdır.")]
    public string Tur { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yer zorunludur."), StringLength(200, ErrorMessage = "Yer en fazla 200 karakter olabilir.")]
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
        var clock = validationContext.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        if (Tur == "Sosyal" && BulusmaTarihi is { } date && date <= clock.GetUtcNow())
            yield return new("Buluşma tarihi gelecekte olmalıdır.", [nameof(BulusmaTarihi)]);
        if (Tur is "Kayip" or "Buluntu" && BulusmaTarihi is not null)
            yield return new("Kayıp/buluntu ilanında buluşma tarihi kullanılamaz.", [nameof(BulusmaTarihi)]);
    }
}

public sealed record ListingResponse(Guid Id, Guid SahipId, Guid KategoriId, string Kategori,
    string Baslik, string Aciklama, string Tur, string Yer, DateTimeOffset? BulusmaTarihi,
    string Durum, DateTimeOffset OlusturulmaTarihi);

public sealed record CategoryResponse(Guid Id, string Ad, string Tur);


public sealed record ListingPageResponse(
    IReadOnlyList<ListingResponse> Items, int TotalCount, int Page, int PageSize, int TotalPages);
