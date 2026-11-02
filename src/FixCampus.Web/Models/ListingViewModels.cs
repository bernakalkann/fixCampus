using System.ComponentModel.DataAnnotations;
using FixCampus.Web.Contracts;

namespace FixCampus.Web.Models;

public class ListingFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Başlık 3 ile 150 karakter arasında olmalıdır.")]
    [Display(Name = "İlan Başlığı")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
    [StringLength(3000, MinimumLength = 5, ErrorMessage = "Açıklama 5 ile 3000 karakter arasında olmalıdır.")]
    [Display(Name = "Detaylı Açıklama")]
    public string Aciklama { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlan türü seçilmelidir.")]
    [Display(Name = "İlan Türü")]
    public string Tur { get; set; } = "Sosyal";

    [Required(ErrorMessage = "Lütfen bir kategori seçiniz.")]
    [Display(Name = "Kategori")]
    public Guid KategoriId { get; set; }

    [Required(ErrorMessage = "Konum / Yer bilgisi zorunludur.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Konum 2 ile 200 karakter arasında olmalıdır.")]
    [Display(Name = "Buluşma / Eşya Konumu")]
    public string Yer { get; set; } = string.Empty;

    [Display(Name = "Buluşma Tarihi ve Saati")]
    public DateTime? BulusmaZamani { get; set; }

    [Display(Name = "İlan Durumu")]
    public string Durum { get; set; } = "Aktif";

    public IReadOnlyList<CategoryDto> AvailableCategories { get; set; } = new List<CategoryDto>();
}

public class ListingListViewModel
{
    public PagedListingsResponse PagedListings { get; set; } = new(new List<ListingSummaryResponse>(), 0, 1, 12, 0);
    public string? SelectedTur { get; set; }
    public Guid? SelectedKategoriId { get; set; }
    public string? SearchQuery { get; set; }
    public IReadOnlyList<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
}

