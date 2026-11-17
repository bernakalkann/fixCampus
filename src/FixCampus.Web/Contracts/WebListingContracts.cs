using System.ComponentModel.DataAnnotations;
using FixCampus.Web.Services;

namespace FixCampus.Web.Contracts;

public sealed record CreateListingRequest(
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Başlık 2 ile 150 karakter arasında olmalıdır.")]
    string Baslik,

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(3000, MinimumLength = 1, ErrorMessage = "Açıklama en fazla 3000 karakter olabilir.")]
    string Aciklama,

    [Required(ErrorMessage = "İlan türü zorunludur.")]
    string Tur,

    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    Guid KategoriId,

    [Required(ErrorMessage = "Yer/Konum bilgisi zorunludur.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Yer en fazla 200 karakter olabilir.")]
    string Yer,

    DateTimeOffset? BulusmaTarihi
);

public sealed record UpdateListingRequest(
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Başlık 2 ile 150 karakter arasında olmalıdır.")]
    string Baslik,

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(3000, MinimumLength = 1, ErrorMessage = "Açıklama en fazla 3000 karakter olabilir.")]
    string Aciklama,

    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    Guid KategoriId,

    [Required(ErrorMessage = "Yer/Konum bilgisi zorunludur.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Yer en fazla 200 karakter olabilir.")]
    string Yer,

    DateTimeOffset? BulusmaTarihi,

    string? Tur = null
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
    public string? HaritaUrl => ListingMapLink.SearchUrl(Yer);
    public bool IsJoined { get; init; }
    public bool IsExpired { get; init; }
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
    public string? HaritaUrl => ListingMapLink.SearchUrl(Yer);
    public bool IsJoined { get; init; }
    public bool IsExpired { get; init; }
    public IReadOnlyList<ParticipantResponse> Katilimcilar { get; init; } = [];
}

public sealed record ParticipationOperationResult(
    bool Success,
    string? ErrorMessage = null,
    bool IsNotFound = false,
    bool IsConflict = false,
    bool IsJoined = false,
    int ParticipantCount = 0
);

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
