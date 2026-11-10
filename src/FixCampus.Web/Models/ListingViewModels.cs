using System.ComponentModel.DataAnnotations;
using FixCampus.Web.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FixCampus.Web.Models;

public sealed class ListingListViewModel
{
    public string? ErrorMessage { get; init; }
    public int? RetryPage { get; init; }
    public PagedListingsResponse PagedListings { get; init; } = new([], 0, 1, 12, 1);
    public IReadOnlyList<CategoryDto> Categories { get; init; } = [];
    public string? SelectedTur { get; init; }
    public Guid? SelectedKategoriId { get; init; }
    public string? SearchQuery { get; init; }
}

public sealed class MyListingsViewModel
{
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ListingSummaryResponse> Listings { get; init; } = [];
    public string? SelectedTur { get; init; }
    public int TotalCount { get; init; }
    public int SocialCount { get; init; }
    public int LostFoundCount { get; init; }
}

public class ListingFormViewModel : IValidatableObject
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "Sosyal",
        "Kayip",
        "Buluntu"
    };

    [Required(ErrorMessage = "Başlık alanı zorunludur.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Başlık 2 ile 150 karakter arasında olmalıdır.")]
    [Display(Name = "İlan Başlığı")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
    [StringLength(3000, MinimumLength = 1, ErrorMessage = "Açıklama en fazla 3000 karakter olabilir.")]
    [Display(Name = "Detaylı Açıklama")]
    public string Aciklama { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlan türü seçilmelidir.")]
    [Display(Name = "İlan Türü")]
    public string Tur { get; set; } = "Sosyal";

    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    [Display(Name = "Kategori")]
    public Guid KategoriId { get; set; }

    [Required(ErrorMessage = "Konum/Yer bilgisi zorunludur.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Yer en fazla 200 karakter olabilir.")]
    [Display(Name = "Kampüs İçi Konum / Yer")]
    public string Yer { get; set; } = string.Empty;

    [Display(Name = "Buluşma Tarihi ve Saati")]
    public DateTime? BulusmaZamani { get; set; }

    public IReadOnlyList<CategoryDto> Categories { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Baslik.Trim().Length < 2)
        {
            yield return new ValidationResult("Başlık en az 2 karakter olmalıdır.", [nameof(Baslik)]);
        }

        if (string.IsNullOrWhiteSpace(Aciklama))
        {
            yield return new ValidationResult("Açıklama boş olamaz.", [nameof(Aciklama)]);
        }

        if (string.IsNullOrWhiteSpace(Yer))
        {
            yield return new ValidationResult("Yer boş olamaz.", [nameof(Yer)]);
        }

        if (KategoriId == Guid.Empty)
        {
            yield return new ValidationResult("Kategori seçilmelidir.", [nameof(KategoriId)]);
        }

        var trimmedType = Tur?.Trim() ?? string.Empty;
        if (!AllowedTypes.Contains(trimmedType))
        {
            yield return new ValidationResult("İlan türü yalnız Sosyal, Kayip veya Buluntu olabilir.", [nameof(Tur)]);
            yield break;
        }

        var clock = validationContext.GetService<TimeProvider>() ?? TimeProvider.System;
        if (trimmedType == "Sosyal")
        {
            if (BulusmaZamani is null)
            {
                yield return new ValidationResult("Sosyal ilanlarda buluşma tarihi zorunludur.", [nameof(BulusmaZamani)]);
            }
            else
            {
                var localDate = DateTime.SpecifyKind(BulusmaZamani.Value, DateTimeKind.Local);
                var dto = new DateTimeOffset(localDate).ToUniversalTime();
                if (dto <= clock.GetUtcNow())
                {
                    yield return new ValidationResult("Buluşma tarihi gelecekte olmalıdır.", [nameof(BulusmaZamani)]);
                }
            }
        }
        else if (BulusmaZamani is not null)
        {
            yield return new ValidationResult("Kayıp ve buluntu ilanlarında buluşma tarihi boş olmalıdır.", [nameof(BulusmaZamani)]);
        }
    }
}

public sealed class ListingEditViewModel : ListingFormViewModel
{
    public Guid Id { get; set; }
}
