using System.Data.Common;
using System.Security.Claims;
using FixCampus.Web.Authorization;
using FixCampus.Web.Contracts;
using FixCampus.Web.Models;
using FixCampus.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Controllers;

[Authorize]
public sealed class IlanlarController(
    IListingService listingService,
    ILogger<IlanlarController> logger,
    TimeProvider? clock = null) : Controller
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    // GET: /Ilanlar
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? tur,
        [FromQuery] Guid? kategoriId,
        [FromQuery] string? q,
        [FromQuery] int sayfa = 1,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await listingService.GetListingsAsync(tur, kategoriId, q, sayfa, 12, ct);
            var categories = await listingService.GetCategoriesAsync(tur, ct);

            var vm = new ListingListViewModel
            {
                PagedListings = paged,
                Categories = categories,
                SelectedTur = tur,
                SelectedKategoriId = kategoriId,
                SearchQuery = q
            };

            return View(vm);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "İlan listesi yüklenirken veritabanı hatası oluştu.");
            TempData["ErrorMessage"] = "İlanlar yüklenirken veritabanı bağlantısı kurulamadı.";
            return View(new ListingListViewModel
            {
                SelectedTur = tur,
                SelectedKategoriId = kategoriId,
                SearchQuery = q
            });
        }
    }

    // GET: /Ilanlar/Detay/{id}
    [HttpGet]
    public async Task<IActionResult> Detay(Guid id, CancellationToken ct = default)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsAdmin();

        var detail = await listingService.GetListingByIdAsync(id, currentUserId, isAdmin, ct);
        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    // GET: /Ilanlar/Benim
    [HttpGet]
    public async Task<IActionResult> Benim([FromQuery] string? tur = null, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Challenge();
        }

        var selectedType = string.IsNullOrWhiteSpace(tur) ? null : tur.Trim();
        if (selectedType is not (null or "Sosyal" or "Kayip" or "Buluntu"))
            return BadRequest("Geçersiz ilan türü.");

        try
        {
            var allUserListings = await listingService.GetUserListingsAsync(userId.Value, null, ct);
            var normalizedTur = selectedType;
            var filtered = normalizedTur is null
                ? allUserListings
                : allUserListings.Where(x => string.Equals(x.Tur, normalizedTur, StringComparison.OrdinalIgnoreCase)).ToList();

            var vm = new MyListingsViewModel
            {
                Listings = filtered,
                SelectedTur = normalizedTur,
                TotalCount = allUserListings.Count,
                SocialCount = allUserListings.Count(x => x.Tur == "Sosyal"),
                LostFoundCount = allUserListings.Count(x => x.Tur is "Kayip" or "Buluntu")
            };

            return View(vm);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "Kullanıcı ilanları yüklenirken veritabanı hatası oluştu.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new MyListingsViewModel
            {
                SelectedTur = selectedType,
                ErrorMessage = "İlanlarınız yüklenemedi. Bağlantınızı kontrol edip tekrar deneyin."
            });
        }
    }

    // GET: /Ilanlar/Yeni
    [HttpGet]
    public async Task<IActionResult> Yeni([FromQuery] string? tur = "Sosyal", CancellationToken ct = default)
    {
        var selectedTur = tur is "Sosyal" or "Kayip" or "Buluntu" ? tur : "Sosyal";
        var categories = await SafeGetCategoriesAsync(null, ct);
        var defaultCat = categories.FirstOrDefault(c => c.Tur == selectedTur)?.Id ?? Guid.Empty;

        var vm = new ListingFormViewModel
        {
            Tur = selectedTur,
            KategoriId = defaultCat,
            BulusmaZamani = selectedTur == "Sosyal"
                ? _clock.GetUtcNow().ToLocalTime().DateTime.AddDays(1).Date.AddHours(14)
                : null,
            Categories = categories
        };

        return View(vm);
    }

    // POST: /Ilanlar/Yeni
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yeni(ListingFormViewModel model, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var bulusmaTarihi = ValidateAndConvertMeetingDate(model);

        if (!ModelState.IsValid)
        {
            model.Categories = await SafeGetCategoriesAsync(null, ct);
            return View(model);
        }

        var request = new CreateListingRequest(
            model.Baslik,
            model.Aciklama,
            model.Tur,
            model.KategoriId,
            model.Yer,
            bulusmaTarihi);

        var userName = User.Identity?.Name ?? "Kampüs Öğrencisi";
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ogrenci@ankara.edu.tr";

        var result = await listingService.CreateListingAsync(request, userId.Value, userName, userEmail, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "İlan veritabanına kaydedilemedi.");
            model.Categories = await SafeGetCategoriesAsync(null, ct);
            return View(model);
        }

        TempData["SuccessMessage"] = "İlanınız başarıyla yayınlandı!";
        return RedirectToAction(nameof(Detay), new { id = result.ListingId });
    }

    // GET: /Ilanlar/Duzenle/{id}
    [HttpGet]
    public async Task<IActionResult> Duzenle(Guid id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var listing = await listingService.GetListingByIdAsync(id, userId, IsAdmin(), ct);
        if (listing is null)
        {
            return NotFound();
        }

        if (!listing.IsOwner)
        {
            return Forbid();
        }

        var categories = await SafeGetCategoriesAsync(null, ct);
        var vm = new ListingEditViewModel
        {
            Id = listing.Id,
            Baslik = listing.Baslik,
            Aciklama = listing.Aciklama,
            Tur = listing.Tur,
            KategoriId = listing.KategoriId,
            Yer = listing.Yer,
            BulusmaZamani = listing.BulusmaTarihi?.ToLocalTime().DateTime,
            Categories = categories
        };

        return View(vm);
    }

    // POST: /Ilanlar/Duzenle/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(Guid id, ListingEditViewModel model, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var existing = await listingService.GetListingByIdAsync(id, userId, IsAdmin(), ct);
        if (existing is null)
        {
            return NotFound();
        }

        if (!existing.IsOwner)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(model.Tur))
        {
            model.Tur = existing.Tur;
        }

        var bulusmaTarihi = ValidateAndConvertMeetingDate(model);

        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Categories = await SafeGetCategoriesAsync(null, ct);
            return View(model);
        }

        var request = new UpdateListingRequest(
            model.Baslik,
            model.Aciklama,
            model.KategoriId,
            model.Yer,
            bulusmaTarihi,
            model.Tur);

        var result = await listingService.UpdateListingAsync(id, request, userId.Value, IsAdmin(), ct);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (result.IsForbidden)
        {
            return Forbid();
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Güncelleme başarısız oldu.");
            model.Id = id;
            model.Categories = await SafeGetCategoriesAsync(null, ct);
            return View(model);
        }

        TempData["SuccessMessage"] = "İlan bilgileri başarıyla güncellendi.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    // POST: /Ilanlar/Sil/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(Guid id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var result = await listingService.DeleteListingAsync(id, userId.Value, IsAdmin(), ct);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (result.IsForbidden)
        {
            return Forbid();
        }

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Silme işlemi başarısız oldu.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        TempData["SuccessMessage"] = "İlan kalıcı olarak silindi.";
        return RedirectToAction(nameof(Benim));
    }

    // GET: /Ilanlar/Kategoriler?tur=Sosyal
    [HttpGet]
    public async Task<IActionResult> Kategoriler([FromQuery] string? tur = null, CancellationToken ct = default)
    {
        var items = await SafeGetCategoriesAsync(tur, ct);
        return Json(items);
    }

    private DateTimeOffset? ValidateAndConvertMeetingDate(ListingFormViewModel model)
    {
        var trimmedTur = model.Tur?.Trim() ?? string.Empty;
        var hasExistingError = ModelState.TryGetValue(nameof(model.BulusmaZamani), out var entry) && entry.Errors.Count > 0;

        if (trimmedTur == "Sosyal")
        {
            if (!model.BulusmaZamani.HasValue)
            {
                if (!hasExistingError)
                {
                    ModelState.AddModelError(nameof(model.BulusmaZamani), "Sosyal ilanlarda buluşma tarihi zorunludur.");
                }
                return null;
            }

            var localDate = DateTime.SpecifyKind(model.BulusmaZamani.Value, DateTimeKind.Local);
            var dto = new DateTimeOffset(localDate).ToUniversalTime();
            if (dto <= _clock.GetUtcNow())
            {
                if (!hasExistingError)
                {
                    ModelState.AddModelError(nameof(model.BulusmaZamani), "Buluşma tarihi gelecekte olmalıdır.");
                }
                return null;
            }

            return dto;
        }

        if (trimmedTur is "Kayip" or "Buluntu" && model.BulusmaZamani.HasValue && !hasExistingError)
        {
            ModelState.AddModelError(nameof(model.BulusmaZamani), "Kayıp ve buluntu ilanlarında buluşma tarihi boş olmalıdır.");
        }

        return null;
    }

    private async Task<IReadOnlyList<CategoryDto>> SafeGetCategoriesAsync(string? tur, CancellationToken ct)
    {
        try
        {
            return await listingService.GetCategoriesAsync(tur, ct);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "Kategoriler yüklenirken veritabanı hatası oluştu.");
            return [];
        }
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private bool IsAdmin() => User.IsInRole(AppRoles.Administrator);
}
