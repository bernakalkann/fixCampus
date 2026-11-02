using System.Security.Claims;
using FixCampus.Web.Authorization;
using FixCampus.Web.Contracts;
using FixCampus.Web.Models;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixCampus.Web.Controllers;

public sealed class IlanlarController(IListingService listingService) : Controller
{
    private Guid? CurrentUserId
    {
        get
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    private string CurrentUserName =>
        User.FindFirstValue(ClaimTypes.Name) ??
        User.FindFirstValue(ClaimTypes.Email)?.Split('@')[0] ??
        "Kampüs Üyesi";

    private bool IsAdmin =>
        User.IsInRole(AppRoles.Administrator) ||
        string.Equals(User.FindFirstValue(ClaimTypes.Role), AppRoles.Administrator, StringComparison.OrdinalIgnoreCase);

    // ==========================================
    // İLAN LİSTELEME
    // ==========================================
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? tur, Guid? kategoriId, string? q, int sayfa = 1, CancellationToken ct = default)
    {
        var categories = await listingService.GetCategoriesAsync(tur, ct);
        var paged = await listingService.GetListingsAsync(tur, kategoriId, q, sayfa, 12, ct);

        var model = new ListingListViewModel
        {
            PagedListings = paged,
            SelectedTur = tur,
            SelectedKategoriId = kategoriId,
            SearchQuery = q,
            Categories = categories
        };

        ViewData["Title"] = string.IsNullOrWhiteSpace(tur)
            ? "Tüm Kampüs İlanları"
            : (tur.ToLowerInvariant() switch
            {
                "sosyal" => "Sosyal Radar - Buluşmalar",
                "kayip" => "Kayıp Eşya İlanları",
                "buluntu" => "Buluntu Eşya İlanları",
                _ => $"{tur} İlanları"
            });

        return View(model);
    }

    // ==========================================
    // İLAN DETAYI
    // ==========================================
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Detay(Guid id, CancellationToken ct)
    {
        var listing = await listingService.GetListingByIdAsync(id, CurrentUserId, IsAdmin, ct);
        if (listing is null)
        {
            TempData["ErrorMessage"] = "Aradığınız ilan bulunamadı veya yayından kaldırılmış olabilir.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = listing.Baslik;
        return View(listing);
    }

    // ==========================================
    // YENİ İLAN OLUŞTUR
    // ==========================================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Yeni(string? tur = "Sosyal", CancellationToken ct = default)
    {
        var normalizedTur = string.IsNullOrWhiteSpace(tur) ? "Sosyal" : char.ToUpperInvariant(tur[0]) + tur.Substring(1).ToLowerInvariant();
        if (normalizedTur != "Sosyal" && normalizedTur != "Kayip" && normalizedTur != "Buluntu")
            normalizedTur = "Sosyal";

        var categories = await listingService.GetCategoriesAsync(normalizedTur, ct);
        var model = new ListingFormViewModel
        {
            Tur = normalizedTur,
            AvailableCategories = categories,
            BulusmaZamani = normalizedTur == "Sosyal" ? DateTime.Now.AddDays(1).Date.AddHours(14) : null
        };

        ViewData["Title"] = "Yeni İlan Oluştur";
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yeni(ListingFormViewModel model, CancellationToken ct)
    {
        if (CurrentUserId is null) return Challenge();

        var normalizedTur = char.ToUpperInvariant(model.Tur[0]) + model.Tur.Substring(1).ToLowerInvariant();
        model.Tur = normalizedTur;

        // Model doğrulaması
        if (normalizedTur == "Sosyal" && !model.BulusmaZamani.HasValue)
        {
            ModelState.AddModelError(nameof(model.BulusmaZamani), "Sosyal buluşmalar için buluşma tarihi zorunludur.");
        }
        else if (normalizedTur == "Sosyal" && model.BulusmaZamani.HasValue && model.BulusmaZamani.Value <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(model.BulusmaZamani), "Buluşma tarihi ileri bir tarih ve saat olmalıdır.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await listingService.GetCategoriesAsync(model.Tur, ct);
            return View(model);
        }

        DateTimeOffset? bulusmaOffset = null;
        if (normalizedTur == "Sosyal" && model.BulusmaZamani.HasValue)
        {
            bulusmaOffset = new DateTimeOffset(model.BulusmaZamani.Value.ToUniversalTime(), TimeSpan.Zero);
        }

        var request = new CreateListingRequest(
            model.Baslik,
            model.Aciklama,
            model.Tur,
            model.KategoriId,
            model.Yer,
            bulusmaOffset
        );

        var result = await listingService.CreateListingAsync(CurrentUserId.Value, CurrentUserName, request, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "İlan kaydedilirken bir hata oluştu.");
            model.AvailableCategories = await listingService.GetCategoriesAsync(model.Tur, ct);
            return View(model);
        }

        TempData["SuccessMessage"] = "İlanınız başarıyla yayınlandı!";
        return RedirectToAction(nameof(Detay), new { id = result.Id });
    }

    // ==========================================
    // İLAN DÜZENLE
    // ==========================================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Duzenle(Guid id, CancellationToken ct)
    {
        if (CurrentUserId is null) return Challenge();

        var listing = await listingService.GetListingByIdAsync(id, CurrentUserId, IsAdmin, ct);
        if (listing is null)
        {
            TempData["ErrorMessage"] = "Düzenlenecek ilan bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Sahiplik veya Yönetici kontrolü
        if (!listing.CanManage)
        {
            TempData["ErrorMessage"] = "Bu ilanı düzenleme yetkiniz bulunmamaktadır.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var categories = await listingService.GetCategoriesAsync(listing.Tur, ct);
        var model = new ListingFormViewModel
        {
            Id = listing.Id,
            Baslik = listing.Baslik,
            Aciklama = listing.Aciklama,
            Tur = listing.Tur,
            KategoriId = listing.KategoriId,
            Yer = listing.Yer,
            BulusmaZamani = listing.BulusmaTarihi?.LocalDateTime,
            Durum = listing.Durum,
            AvailableCategories = categories
        };

        ViewData["Title"] = $"İlanı Düzenle: {listing.Baslik}";
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(Guid id, ListingFormViewModel model, CancellationToken ct)
    {
        if (CurrentUserId is null) return Challenge();

        var existing = await listingService.GetListingByIdAsync(id, CurrentUserId, IsAdmin, ct);
        if (existing is null)
        {
            TempData["ErrorMessage"] = "İlan bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (!existing.CanManage)
        {
            TempData["ErrorMessage"] = "Bu ilanı düzenleme yetkiniz yoktur.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (existing.Tur == "Sosyal" && !model.BulusmaZamani.HasValue)
        {
            ModelState.AddModelError(nameof(model.BulusmaZamani), "Sosyal buluşmalar için buluşma tarihi zorunludur.");
        }

        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Tur = existing.Tur;
            model.AvailableCategories = await listingService.GetCategoriesAsync(existing.Tur, ct);
            return View(model);
        }

        DateTimeOffset? bulusmaOffset = null;
        if (existing.Tur == "Sosyal" && model.BulusmaZamani.HasValue)
        {
            bulusmaOffset = new DateTimeOffset(model.BulusmaZamani.Value.ToUniversalTime(), TimeSpan.Zero);
        }

        var request = new UpdateListingRequest(
            model.Baslik,
            model.Aciklama,
            model.KategoriId,
            model.Yer,
            bulusmaOffset,
            model.Durum
        );

        var result = await listingService.UpdateListingAsync(id, CurrentUserId.Value, IsAdmin, request, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Güncelleme sırasında hata oluştu.");
            model.Id = id;
            model.Tur = existing.Tur;
            model.AvailableCategories = await listingService.GetCategoriesAsync(existing.Tur, ct);
            return View(model);
        }

        TempData["SuccessMessage"] = "İlan başarıyla güncellendi!";
        return RedirectToAction(nameof(Detay), new { id });
    }

    // ==========================================
    // İLAN SİL
    // ==========================================
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(Guid id, CancellationToken ct)
    {
        if (CurrentUserId is null) return Challenge();

        var result = await listingService.DeleteListingAsync(id, CurrentUserId.Value, IsAdmin, ct);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "İlan silinemedi.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        TempData["SuccessMessage"] = "İlan başarıyla silindi.";
        return RedirectToAction(nameof(Benim));
    }

    // ==========================================
    // İLANLARIM
    // ==========================================
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Benim(CancellationToken ct)
    {
        if (CurrentUserId is null) return Challenge();

        var listings = await listingService.GetUserListingsAsync(CurrentUserId.Value, ct);
        ViewData["Title"] = "İlanlarım";
        return View(listings);
    }
}
