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
    Task<PagedListingsResponse> GetListingsAsync(string? tur, Guid? kategoriId, string? arama, int sayfa, int sayfaBoyutu, Guid? currentUserId, CancellationToken ct = default)
        => GetListingsAsync(tur, kategoriId, arama, sayfa, sayfaBoyutu, ct);
    Task<ListingDetailResponse?> GetListingByIdAsync(Guid id, Guid? currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<IReadOnlyList<ListingSummaryResponse>> GetUserListingsAsync(Guid userId, string? tur = null, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(string? tur = null, CancellationToken ct = default);
    Task<ListingOperationResult> CreateListingAsync(CreateListingRequest request, Guid userId, string userDisplayName, string userEmail, CancellationToken ct = default);
    Task<ListingOperationResult> UpdateListingAsync(Guid id, UpdateListingRequest request, Guid currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<ListingOperationResult> DeleteListingAsync(Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<ParticipationOperationResult> JoinListingAsync(Guid listingId, Guid userId, string userDisplayName = "Kampüs Öğrencisi", string userEmail = "ogrenci@ankara.edu.tr", CancellationToken ct = default);
    Task<ParticipationOperationResult> LeaveListingAsync(Guid listingId, Guid userId, CancellationToken ct = default);
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

    public Task<PagedListingsResponse> GetListingsAsync(
        string? tur,
        Guid? kategoriId,
        string? arama,
        int sayfa,
        int sayfaBoyutu,
        CancellationToken ct = default) =>
        GetListingsAsync(tur, kategoriId, arama, sayfa, sayfaBoyutu, currentUserId: null, ct);

    public async Task<PagedListingsResponse> GetListingsAsync(
        string? tur,
        Guid? kategoriId,
        string? arama,
        int sayfa,
        int sayfaBoyutu,
        Guid? currentUserId,
        CancellationToken ct = default)
    {
        (sayfa, sayfaBoyutu) = ListingQueries.Normalize(sayfa, sayfaBoyutu);
        var query = ListingQueries.Filter(db.Ilanlar.AsNoTracking(), db.Database.IsNpgsql(), tur, kategoriId, arama, visibleAtUtc: _clock.GetUtcNow());

        var totalCount = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)sayfaBoyutu));
        var userId = currentUserId ?? Guid.Empty;

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
            )
            {
                IsJoined = userId != Guid.Empty && i.Katilimlar.Any(k => k.ProfilId == userId)
            })
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
                .ThenInclude(k => k.Profil)
            .Include(i => i.Fotograflar.OrderBy(f => f.YuklemeTarihi))
            .Where(i => i.Id == id && i.Durum != "Gizli")
            .FirstOrDefaultAsync(ct);

        if (ilan is null)
        {
            return null;
        }

        var isOwner = currentUserId.HasValue && ilan.SahipId == currentUserId.Value;
        var now = _clock.GetUtcNow();
        var isExpired = ilan.Tur == "Sosyal" && (!ilan.BulusmaTarihi.HasValue || ilan.BulusmaTarihi.Value <= now);
        var isJoined = currentUserId.HasValue && ilan.Katilimlar.Any(k => k.ProfilId == currentUserId.Value);

        // Berna'nın GET /api/ilanlar/{id}/katilimcilar kuralıyla uyumlu olarak
        // katılımcı ad-soyad, bölüm ve sınıf bilgisi yalnızca ilan sahibine gösterilir.
        IReadOnlyList<ParticipantResponse> participants = isOwner
            ? ilan.Katilimlar
                .OrderBy(k => k.KatilimTarihi)
                .ThenBy(k => k.Id)
                .Select(k => new ParticipantResponse(
                    k.Profil?.AdSoyad ?? "Kampüs Öğrencisi",
                    k.Profil?.Bolum,
                    k.Profil?.Sinif))
                .ToList()
            : [];

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
        )
        {
            IsJoined = isJoined,
            IsExpired = isExpired,
            Katilimcilar = participants
        };
    }

    public async Task<IReadOnlyList<ListingSummaryResponse>> GetUserListingsAsync(Guid userId, string? tur = null, CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
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
            )
            {
                IsExpired = i.Tur == "Sosyal" && (i.BulusmaTarihi == null || i.BulusmaTarihi <= now),
                IsJoined = i.Katilimlar.Any(k => k.ProfilId == userId)
            })
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
            await EnsureProfileExistsAsync(userId, userDisplayName, userEmail, ct);

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

    public async Task<ParticipationOperationResult> JoinListingAsync(
        Guid listingId,
        Guid userId,
        string userDisplayName = "Kampüs Öğrencisi",
        string userEmail = "ogrenci@ankara.edu.tr",
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return new ParticipationOperationResult(false, "Geçerli bir kullanıcı oturumu gereklidir.");
        }

        try
        {
            var ilan = await db.Ilanlar
                .AsNoTracking()
                .Where(i => i.Id == listingId && i.Durum != "Gizli")
                .FirstOrDefaultAsync(ct);

            if (ilan is null)
            {
                return new ParticipationOperationResult(false, "Buluşma ilanı bulunamadı.", IsNotFound: true);
            }

            if (ilan.Tur != "Sosyal")
            {
                return new ParticipationOperationResult(false, "Yalnızca sosyal buluşma ilanlarına katılım sağlanabilir.");
            }

            if (ilan.Durum != "Aktif")
            {
                return new ParticipationOperationResult(false, "Bu buluşma ilanı artık aktif değil.");
            }

            if (!ilan.BulusmaTarihi.HasValue || ilan.BulusmaTarihi.Value <= _clock.GetUtcNow())
            {
                return new ParticipationOperationResult(false, "Süresi geçmiş buluşmalara katılım sağlanamaz.");
            }

            if (await db.Katilimlar.AnyAsync(k => k.IlanId == listingId && k.ProfilId == userId, ct))
            {
                var currentCount = await db.Katilimlar.CountAsync(k => k.IlanId == listingId, ct);
                return new ParticipationOperationResult(
                    false,
                    "Bu buluşmaya zaten katılıyorsunuz.",
                    IsConflict: true,
                    IsJoined: true,
                    ParticipantCount: currentCount);
            }

            await EnsureProfileExistsAsync(userId, userDisplayName, userEmail, ct);

            db.Katilimlar.Add(new Katilim
            {
                Id = Guid.NewGuid(),
                IlanId = listingId,
                ProfilId = userId,
                KatilimTarihi = _clock.GetUtcNow()
            });

            await db.SaveChangesAsync(ct);

            var updatedCount = await db.Katilimlar.CountAsync(k => k.IlanId == listingId, ct);
            return new ParticipationOperationResult(true, IsJoined: true, ParticipantCount: updatedCount);
        }
        catch (DbUpdateException ex)
        {
            db.ChangeTracker.Clear();
            if (await db.Katilimlar.AnyAsync(k => k.IlanId == listingId && k.ProfilId == userId, ct))
            {
                var currentCount = await db.Katilimlar.CountAsync(k => k.IlanId == listingId, ct);
                return new ParticipationOperationResult(
                    false,
                    "Bu buluşmaya zaten katılıyorsunuz.",
                    IsConflict: true,
                    IsJoined: true,
                    ParticipantCount: currentCount);
            }

            logger.LogError(ex, "Buluşmaya katılım kaydedilirken veritabanı hatası oluştu: {ListingId}", listingId);
            return new ParticipationOperationResult(false, "Katılım işlemi kaydedilemedi. Lütfen tekrar deneyin.");
        }
        catch (Exception ex) when (ex is DbException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "Buluşmaya katılım sırasında veritabanı hatası oluştu: {ListingId}", listingId);
            return new ParticipationOperationResult(false, "Katılım işlemi kaydedilemedi. Lütfen bağlantınızı kontrol edin.");
        }
    }

    public async Task<ParticipationOperationResult> LeaveListingAsync(
        Guid listingId,
        Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return new ParticipationOperationResult(false, "Geçerli bir kullanıcı oturumu gereklidir.");
        }

        try
        {
            var ilan = await db.Ilanlar
                .AsNoTracking()
                .Where(i => i.Id == listingId && i.Durum != "Gizli")
                .FirstOrDefaultAsync(ct);

            if (ilan is null)
            {
                return new ParticipationOperationResult(false, "Buluşma ilanı bulunamadı.", IsNotFound: true);
            }

            if (ilan.Tur != "Sosyal")
            {
                return new ParticipationOperationResult(false, "Yalnızca sosyal buluşma ilanlarından ayrılabilirsiniz.");
            }

            var existing = await db.Katilimlar
                .Where(k => k.IlanId == listingId && k.ProfilId == userId)
                .FirstOrDefaultAsync(ct);

            if (existing is null)
            {
                var currentCount = await db.Katilimlar.CountAsync(k => k.IlanId == listingId, ct);
                return new ParticipationOperationResult(
                    false,
                    "Bu buluşmaya aktif bir katılımınız bulunmuyor.",
                    IsJoined: false,
                    ParticipantCount: currentCount);
            }

            db.Katilimlar.Remove(existing);
            await db.SaveChangesAsync(ct);

            var updatedCount = await db.Katilimlar.CountAsync(k => k.IlanId == listingId, ct);
            return new ParticipationOperationResult(true, IsJoined: false, ParticipantCount: updatedCount);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "Buluşmadan ayrılma sırasında veritabanı hatası oluştu: {ListingId}", listingId);
            return new ParticipationOperationResult(false, "Katılım geri alınamadı. Lütfen tekrar deneyin.");
        }
    }

    private async Task EnsureProfileExistsAsync(Guid userId, string userDisplayName, string userEmail, CancellationToken ct)
    {
        if (await db.Profiller.AnyAsync(p => p.Id == userId, ct))
        {
            return;
        }

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
                    VALUES ({userId}, {safeName}, {role}, {_clock.GetUtcNow()})
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

}
