using System.Data.Common;
using FixCampus.Web.Authorization;
using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Services;

public sealed record ListingOperationResult(
    bool Success,
    string? ErrorMessage = null,
    bool IsForbidden = false,
    bool IsNotFound = false,
    Guid? ListingId = null
);

public interface IListingService
{
    Task<PagedListingsResponse> GetListingsAsync(string? tur, Guid? kategoriId, string? arama, int sayfa, int sayfaBoyutu, CancellationToken ct = default);
    Task<ListingDetailResponse?> GetListingByIdAsync(Guid id, Guid? currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<IReadOnlyList<ListingSummaryResponse>> GetUserListingsAsync(Guid userId, string? tur = null, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(string? tur = null, CancellationToken ct = default);
    Task<ListingOperationResult> CreateListingAsync(CreateListingRequest request, Guid userId, string userDisplayName, string userEmail, CancellationToken ct = default);
    Task<ListingOperationResult> UpdateListingAsync(Guid id, UpdateListingRequest request, Guid currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<ListingOperationResult> DeleteListingAsync(Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default);
}

public sealed class ListingService(
    FixCampusDbContext db,
    IListingValidator validator,
    ILogger<ListingService> logger,
    TimeProvider? clock = null) : IListingService
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(string? tur = null, CancellationToken ct = default)
    {
        var query = db.Kategoriler.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(tur))
        {
            var normalizedTur = tur.Trim();
            query = query.Where(k => k.Tur == normalizedTur);
        }

        return await query
            .OrderBy(k => k.Tur)
            .ThenBy(k => k.Ad)
            .ThenBy(k => k.Id)
            .Select(k => new CategoryDto(k.Id, k.Ad, k.Tur))
            .ToListAsync(ct);
    }

    public async Task<PagedListingsResponse> GetListingsAsync(
        string? tur,
        Guid? kategoriId,
        string? arama,
        int sayfa,
        int sayfaBoyutu,
        CancellationToken ct = default)
    {
        (sayfa, sayfaBoyutu) = ListingQueries.Normalize(sayfa, sayfaBoyutu);
        var query = ListingQueries.Filter(db.Ilanlar.AsNoTracking(), db.Database.IsNpgsql(), tur, kategoriId, arama);

        var totalCount = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)sayfaBoyutu));

        var items = await ListingQueries.Page(query, sayfa, sayfaBoyutu)
            .Select(i => new ListingSummaryResponse(
                i.Id,
                i.SahipId,
                i.Sahip != null ? i.Sahip.AdSoyad : "Kampüs Öğrencisi",
                i.KategoriId,
                i.Kategori != null ? i.Kategori.Ad : "Genel",
                i.Baslik,
                i.Aciklama,
                i.Tur,
                i.Yer,
                i.BulusmaTarihi,
                i.Durum,
                i.OlusturulmaTarihi,
                i.Katilimlar.Count,
                i.Fotograflar.OrderBy(f => f.YuklemeTarihi).Select(f => f.DosyaYolu).FirstOrDefault()
            ))
            .ToListAsync(ct);

        return new PagedListingsResponse(items, totalCount, sayfa, sayfaBoyutu, totalPages);
    }

    public async Task<ListingDetailResponse?> GetListingByIdAsync(
        Guid id,
        Guid? currentUserId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var ilan = await db.Ilanlar
            .AsNoTracking()
            .Include(i => i.Sahip)
            .Include(i => i.Kategori)
            .Include(i => i.Katilimlar)
            .Include(i => i.Fotograflar.OrderBy(f => f.YuklemeTarihi))
            .Where(i => i.Id == id && i.Durum != "Gizli")
            .FirstOrDefaultAsync(ct);

        if (ilan is null)
        {
            return null;
        }

        var isOwner = currentUserId.HasValue && ilan.SahipId == currentUserId.Value;

        return new ListingDetailResponse(
            ilan.Id,
            ilan.SahipId,
            ilan.Sahip?.AdSoyad ?? "Kampüs Öğrencisi",
            ilan.KategoriId,
            ilan.Kategori?.Ad ?? "Genel",
            ilan.Baslik,
            ilan.Aciklama,
            ilan.Tur,
            ilan.Yer,
            ilan.BulusmaTarihi,
            ilan.Durum,
            ilan.OlusturulmaTarihi,
            ilan.Katilimlar.Count,
            ilan.Fotograflar.OrderBy(f => f.YuklemeTarihi).Select(f => f.DosyaYolu).ToList(),
            IsOwner: isOwner,
            CanManage: isOwner
        );
    }

    public async Task<IReadOnlyList<ListingSummaryResponse>> GetUserListingsAsync(Guid userId, string? tur = null, CancellationToken ct = default)
    {
        var query = db.Ilanlar
            .AsNoTracking()
            .Include(i => i.Sahip)
            .Include(i => i.Kategori)
            .Include(i => i.Katilimlar)
            .Include(i => i.Fotograflar)
            .Where(i => i.SahipId == userId && i.Durum != "Gizli");

        if (!string.IsNullOrWhiteSpace(tur))
        {
            var normalizedTur = tur.Trim();
            query = query.Where(i => i.Tur == normalizedTur);
        }

        return await query
            .OrderByDescending(i => i.OlusturulmaTarihi)
            .ThenByDescending(i => i.Id)
            .Select(i => new ListingSummaryResponse(
                i.Id,
                i.SahipId,
                i.Sahip != null ? i.Sahip.AdSoyad : "Kampüs Öğrencisi",
                i.KategoriId,
                i.Kategori != null ? i.Kategori.Ad : "Genel",
                i.Baslik,
                i.Aciklama,
                i.Tur,
                i.Yer,
                i.BulusmaTarihi,
                i.Durum,
                i.OlusturulmaTarihi,
                i.Katilimlar.Count,
                i.Fotograflar.OrderBy(f => f.YuklemeTarihi).Select(f => f.DosyaYolu).FirstOrDefault()
            ))
            .ToListAsync(ct);
    }

    public async Task<ListingOperationResult> CreateListingAsync(
        CreateListingRequest request,
        Guid userId,
        string userDisplayName,
        string userEmail,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateCreateAsync(request, ct);
        if (!validation.IsValid)
        {
            return new ListingOperationResult(false, validation.ErrorMessage);
        }

        var normalizedTur = request.Tur.Trim();
        var newListing = new Ilan
        {
            Id = Guid.NewGuid(),
            SahipId = userId,
            KategoriId = request.KategoriId,
            Baslik = request.Baslik.Trim(),
            Aciklama = request.Aciklama.Trim(),
            Tur = normalizedTur,
            Yer = request.Yer.Trim(),
            BulusmaTarihi = normalizedTur == "Sosyal" && request.BulusmaTarihi.HasValue
                ? request.BulusmaTarihi.Value.ToUniversalTime()
                : null,
            Durum = "Aktif",
            OlusturulmaTarihi = _clock.GetUtcNow()
        };

        try
        {
            if (!await db.Profiller.AnyAsync(p => p.Id == userId, ct))
            {
                var role = userEmail.Contains("admin", StringComparison.OrdinalIgnoreCase) || userDisplayName.Contains("Yönetici", StringComparison.OrdinalIgnoreCase)
                    ? AppRoles.Administrator
                    : AppRoles.Student;

                var safeName = string.IsNullOrWhiteSpace(userDisplayName) ? "Kampüs Öğrencisi" : userDisplayName.Trim();
                try
                {
                    if (db.Database.IsNpgsql())
                    {
                        await db.Database.ExecuteSqlInterpolatedAsync($"""
                            INSERT INTO "Profiller" ("Id", "AdSoyad", "Rol", "OlusturulmaTarihi")
                            VALUES ({userId}, {safeName}, {role}, {DateTimeOffset.UtcNow})
                            ON CONFLICT ("Id") DO NOTHING
                        """, ct);
                    }
                    else
                    {
                        db.Profiller.Add(new Profil
                        {
                            Id = userId,
                            AdSoyad = safeName,
                            Rol = role,
                            OlusturulmaTarihi = _clock.GetUtcNow()
                        });
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception pEx)
                {
                    logger.LogWarning(pEx, "Profil otomatik oluşturulurken hata oluştu.");
                }
            }

            db.Ilanlar.Add(newListing);
            await db.SaveChangesAsync(ct);
            return new ListingOperationResult(true, ListingId: newListing.Id);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "İlan veritabanına kaydedilemedi.");
            return new ListingOperationResult(false, "İlan veritabanına kaydedilemedi. Lütfen veritabanı bağlantınızı kontrol edip tekrar deneyin.");
        }
    }

    public async Task<ListingOperationResult> UpdateListingAsync(
        Guid id,
        UpdateListingRequest request,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        try
        {
            var ilan = await db.Ilanlar
                .Where(i => i.Id == id && i.Durum != "Gizli")
                .FirstOrDefaultAsync(ct);

            if (ilan is null)
            {
                return new ListingOperationResult(false, "İlan bulunamadı.", IsNotFound: true);
            }

            if (ilan.SahipId != currentUserId)
            {
                return new ListingOperationResult(
                    false,
                    "Bu ilanı düzenleme yetkiniz bulunmamaktadır (403 Forbidden). Yalnızca ilan sahibi düzenleyebilir.",
                    IsForbidden: true);
            }

            var effectiveTur = string.IsNullOrWhiteSpace(request.Tur) ? ilan.Tur : request.Tur.Trim();
            var validation = await validator.ValidateUpdateAsync(request with { Tur = effectiveTur }, ilan.Tur, ct);
            if (!validation.IsValid)
            {
                return new ListingOperationResult(false, validation.ErrorMessage);
            }

            ilan.Baslik = request.Baslik.Trim();
            ilan.Aciklama = request.Aciklama.Trim();
            ilan.Tur = effectiveTur;
            ilan.KategoriId = request.KategoriId;
            ilan.Yer = request.Yer.Trim();
            ilan.BulusmaTarihi = effectiveTur == "Sosyal" && request.BulusmaTarihi.HasValue
                ? request.BulusmaTarihi.Value.ToUniversalTime()
                : null;

            await db.SaveChangesAsync(ct);
            return new ListingOperationResult(true, ListingId: ilan.Id);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "İlan güncellenirken veritabanı hatası oluştu: {ListingId}", id);
            return new ListingOperationResult(false, "İlan veritabanında güncellenemedi. Lütfen tekrar deneyin.");
        }
    }

    public async Task<ListingOperationResult> DeleteListingAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        try
        {
            var ilan = await db.Ilanlar
                .Where(i => i.Id == id && i.Durum != "Gizli")
                .FirstOrDefaultAsync(ct);

            if (ilan is null)
            {
                return new ListingOperationResult(false, "Silinmek istenen ilan bulunamadı.", IsNotFound: true);
            }

            if (ilan.SahipId != currentUserId)
            {
                return new ListingOperationResult(
                    false,
                    "Bu ilanı silme yetkiniz bulunmamaktadır (403 Forbidden). Yalnızca ilan sahibi silebilir.",
                    IsForbidden: true);
            }

            db.Ilanlar.Remove(ilan);
            await db.SaveChangesAsync(ct);
            return new ListingOperationResult(true);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "İlan silinirken veritabanı hatası oluştu: {ListingId}", id);
            return new ListingOperationResult(false, "İlan veritabanından silinemedi. Lütfen tekrar deneyin.");
        }
    }

}
