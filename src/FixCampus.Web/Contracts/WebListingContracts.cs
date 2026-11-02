using System.ComponentModel.DataAnnotations;

namespace FixCampus.Web.Contracts;

public sealed record CreateListingRequest(
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Başlık 3 ile 150 karakter arasında olmalıdır.")]
    string Baslik,

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(3000, MinimumLength = 5, ErrorMessage = "Açıklama 5 ile 3000 karakter arasında olmalıdır.")]
    string Aciklama,

    [Required(ErrorMessage = "İlan türü zorunludur.")]
    string Tur,

    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    Guid KategoriId,

    [Required(ErrorMessage = "Yer/Konum bilgisi zorunludur.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Yer 2 ile 200 karakter arasında olmalıdır.")]
    string Yer,

    DateTimeOffset? BulusmaTarihi
);

public sealed record UpdateListingRequest(
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Başlık 3 ile 150 karakter arasında olmalıdır.")]
    string Baslik,

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(3000, MinimumLength = 5, ErrorMessage = "Açıklama 5 ile 3000 karakter arasında olmalıdır.")]
    string Aciklama,

    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    Guid KategoriId,

    [Required(ErrorMessage = "Yer/Konum bilgisi zorunludur.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Yer 2 ile 200 karakter arasında olmalıdır.")]
    string Yer,

    DateTimeOffset? BulusmaTarihi,

    string? Durum = "Aktif"
);

public sealed record ListingSummaryResponse(
    Guid Id,
    Guid SahipId,
    string SahipAd,
    Guid KategoriId,
    string KategoriAd,
    string Baslik,
    string Aciklama,
    string Tur,
    string Yer,
    DateTimeOffset? BulusmaTarihi,
    string Durum,
    DateTimeOffset OlusturulmaTarihi,
    int KatilimSayisi,
    string? IlkFotografUrl
)
{
    public int KatilimciSayisi => KatilimSayisi;
}

public sealed record ListingDetailResponse(
    Guid Id,
    Guid SahipId,
    string SahipAd,
    Guid KategoriId,
    string KategoriAd,
    string Baslik,
    string Aciklama,
    string Tur,
    string Yer,
    DateTimeOffset? BulusmaTarihi,
    string Durum,
    DateTimeOffset OlusturulmaTarihi,
    int KatilimSayisi,
    IReadOnlyList<string> Fotograflar,
    bool IsOwner,
    bool CanManage
)
{
    public int KatilimciSayisi => KatilimSayisi;
}

public sealed record PagedListingsResponse(
    IReadOnlyList<ListingSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record CategoryDto(
    Guid Id,
    string Ad,
    string Tur
);
