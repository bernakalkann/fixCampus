using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Services;

public sealed record ListingOperationResult(bool Success, string? ErrorMessage = null, bool IsForbidden = false, bool IsNotFound = false, Guid? Id = null);

public interface IListingService
{
    Task<PagedListingsResponse> GetListingsAsync(string? tur = null, Guid? kategoriId = null, string? query = null, int page = 1, int pageSize = 12, CancellationToken ct = default);
    Task<ListingDetailResponse?> GetListingByIdAsync(Guid id, Guid? currentUserId = null, bool isAdmin = false, CancellationToken ct = default);
    Task<IReadOnlyList<ListingSummaryResponse>> GetUserListingsAsync(Guid userId, CancellationToken ct = default);
    Task<ListingOperationResult> CreateListingAsync(Guid ownerId, string ownerName, CreateListingRequest request, CancellationToken ct = default);
    Task<ListingOperationResult> UpdateListingAsync(Guid id, Guid currentUserId, bool isAdmin, UpdateListingRequest request, CancellationToken ct = default);
    Task<ListingOperationResult> DeleteListingAsync(Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(string? tur = null, CancellationToken ct = default);
    Task EnsureDefaultCategoriesAsync(CancellationToken ct = default);
}

public sealed class ListingService(
    FixCampusDbContext db,
    IListingValidator validator,
    ILogger<ListingService> logger) : IListingService
{
    private static readonly object _sync = new();
    private static readonly List<Ilan> InMemListings = new()
    {
        new Ilan
        {
            Id = Guid.Parse("11111111-1111-4111-8111-111111111111"),
            SahipId = Guid.Parse("fdeef191-f6df-4fba-a4e1-d33992eab002"),
            Sahip = new Profil { Id = Guid.Parse("fdeef191-f6df-4fba-a4e1-d33992eab002"), AdSoyad = "Berke Mahmut Acar", Rol = "Ogrenci" },
            KategoriId = Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Kategori = new Kategori { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Ad = "Ders & Sınav Çalışma", Tur = "Sosyal" },
            Baslik = "Kütüphanede Algoritma & Veri Yapıları Çalışma Grubu",
            Aciklama = "Vize sınavına hazırlık için Merkez Kütüphane 2. kat grup çalışma odasında birlikte soru çözeceğiz. Katılmak isteyen herkesi bekleriz!",
            Tur = "Sosyal",
            Yer = "Merkez Kütüphane 2. Kat Grup Odası 4",
            BulusmaTarihi = DateTimeOffset.UtcNow.AddDays(2),
            Durum = "Aktif",
            OlusturulmaTarihi = DateTimeOffset.UtcNow.AddHours(-3)
        },
        new Ilan
        {
            Id = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            SahipId = Guid.Parse("fdeef191-f6df-4fba-a4e1-d33992eab002"),
            Sahip = new Profil { Id = Guid.Parse("fdeef191-f6df-4fba-a4e1-d33992eab002"), AdSoyad = "Berke Mahmut Acar", Rol = "Ogrenci" },
            KategoriId = Guid.Parse("22222222-0000-0000-0000-000000000004"),
            Kategori = new Kategori { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), Ad = "Giyim & Kişisel Eşya", Tur = "Kayip" },
            Baslik = "Mavi Renkli Termos Matara Kayıp",
            Aciklama = "Mühendislik Fakültesi D Blok 102 nolu amfide mavi renkli metal termosumu unuttum. Gören veya teslim eden olursa lütfen iletişime geçsin.",
            Tur = "Kayip",
            Yer = "Mühendislik Fakültesi D Blok Amfi 102",
            BulusmaTarihi = null,
            Durum = "Aktif",
            OlusturulmaTarihi = DateTimeOffset.UtcNow.AddHours(-6)
        },
        new Ilan
        {
            Id = Guid.Parse("33333333-3333-4333-8333-333333333333"),
            SahipId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Sahip = new Profil { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), AdSoyad = "Kampüs Sistem Yöneticisi", Rol = "Yonetici" },
            KategoriId = Guid.Parse("33333333-0000-0000-0000-000000000002"),
            Kategori = new Kategori { Id = Guid.Parse("33333333-0000-0000-0000-000000000002"), Ad = "Cüzdan, Kimlik & Kart", Tur = "Buluntu" },
            Baslik = "Yemekhanede Bulunan Öğrenci Kimlik Kartı",
            Aciklama = "Merkez Yemekhane giriş turnikelerinin yanında bir adet öğrenci kimliği bulunmuştur. Güvenlik birimine teslim edildi, oradan teslim alabilirsiniz.",
            Tur = "Buluntu",
            Yer = "Merkez Yemekhane Güvenlik Noktası",
            BulusmaTarihi = null,
            Durum = "Aktif",
            OlusturulmaTarihi = DateTimeOffset.UtcNow.AddHours(-1)
        }
    };

    public async Task EnsureDefaultCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            if (await db.Kategoriler.AnyAsync(ct)) return;
            await db.Kategoriler.AddRangeAsync(DefaultCategories.Items, ct);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Varsayılan kategoriler başarıyla oluşturuldu.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Varsayılan kategoriler veritabanına yüklenirken hata oluştu (yerel katalog devrede).");
        }
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(string? tur = null, CancellationToken ct = default)
    {
        try
        {
            await EnsureDefaultCategoriesAsync(ct);

            var query = db.Kategoriler.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(tur))
            {
                var normalizedTur = char.ToUpperInvariant(tur[0]) + tur.Substring(1).ToLowerInvariant();
                query = query.Where(k => k.Tur == normalizedTur);
            }

            var list = await query
                .OrderBy(k => k.Tur)
                .ThenBy(k => k.Ad)
                .Select(k => new CategoryDto(k.Id, k.Ad, k.Tur))
                .ToListAsync(ct);

            if (list.Count > 0) return list;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Veritabanından kategoriler okunamadı. Yerel varsayılan katalog kullanılıyor.");
        }

        return DefaultCategories.AsDtos(tur);
    }

    public async Task<PagedListingsResponse> GetListingsAsync(
        string? tur = null, Guid? kategoriId = null, string? query = null, int page = 1, int pageSize = 12, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 12;

        try
        {
            var q = db.Ilanlar.AsNoTracking()
                .Include(i => i.Sahip)
                .Include(i => i.Kategori)
                .Include(i => i.Katilimlar)
                .Include(i => i.Fotograflar)
                .Where(i => i.Durum == "Aktif");

            if (!string.IsNullOrWhiteSpace(tur))
            {
                var normalizedTur = char.ToUpperInvariant(tur[0]) + tur.Substring(1).ToLowerInvariant();
                q = q.Where(i => i.Tur == normalizedTur);
            }

            if (kategoriId.HasValue && kategoriId.Value != Guid.Empty)
            {
                q = q.Where(i => i.KategoriId == kategoriId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLower();
                q = q.Where(i =>
                    i.Baslik.ToLower().Contains(term) ||
                    i.Aciklama.ToLower().Contains(term) ||
                    i.Yer.ToLower().Contains(term));
            }

            var totalCount = await q.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await q
                .OrderByDescending(i => i.OlusturulmaTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ListingSummaryResponse(
                    i.Id,
                    i.SahipId,
                    i.Sahip != null ? i.Sahip.AdSoyad : "Kampüs Üyesi",
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
                    i.Fotograflar.Select(f => f.DosyaYolu).FirstOrDefault()
                ))
                .ToListAsync(ct);

            return new PagedListingsResponse(items, totalCount, page, pageSize, totalPages);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Veritabanından ilanlar listelenemedi. Bellek içi ilan havuzu kullanılıyor.");
        }

        // Çevrimdışı / Veritabanı arızası durumunda bellek içi geri dönüş
        lock (_sync)
        {
            var inMemQuery = InMemListings.Where(i => i.Durum == "Aktif").AsEnumerable();

            if (!string.IsNullOrWhiteSpace(tur))
            {
                var normalizedTur = char.ToUpperInvariant(tur[0]) + tur.Substring(1).ToLowerInvariant();
                inMemQuery = inMemQuery.Where(i => string.Equals(i.Tur, normalizedTur, StringComparison.OrdinalIgnoreCase));
            }

            if (kategoriId.HasValue && kategoriId.Value != Guid.Empty)
            {
                inMemQuery = inMemQuery.Where(i => i.KategoriId == kategoriId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                inMemQuery = inMemQuery.Where(i =>
                    i.Baslik.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.Aciklama.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.Yer.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var totalInMem = inMemQuery.Count();
            var totalInMemPages = (int)Math.Ceiling(totalInMem / (double)pageSize);

            var itemsInMem = inMemQuery
                .OrderByDescending(i => i.OlusturulmaTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ListingSummaryResponse(
                    i.Id,
                    i.SahipId,
                    i.Sahip?.AdSoyad ?? "Kampüs Üyesi",
                    i.KategoriId,
                    i.Kategori?.Ad ?? DefaultCategories.Items.FirstOrDefault(k => k.Id == i.KategoriId)?.Ad ?? "Genel",
                    i.Baslik,
                    i.Aciklama,
                    i.Tur,
                    i.Yer,
                    i.BulusmaTarihi,
                    i.Durum,
                    i.OlusturulmaTarihi,
                    i.Katilimlar?.Count ?? 0,
                    i.Fotograflar?.Select(f => f.DosyaYolu).FirstOrDefault()
                ))
                .ToList();

            return new PagedListingsResponse(itemsInMem, totalInMem, page, pageSize, totalInMemPages);
        }
    }

    public async Task<ListingDetailResponse?> GetListingByIdAsync(
        Guid id, Guid? currentUserId = null, bool isAdmin = false, CancellationToken ct = default)
    {
        try
        {
            var item = await db.Ilanlar.AsNoTracking()
                .Include(i => i.Sahip)
                .Include(i => i.Kategori)
                .Include(i => i.Katilimlar)
                .Include(i => i.Fotograflar)
                .SingleOrDefaultAsync(i => i.Id == id, ct);

            if (item is not null)
            {
                var isOwner = currentUserId.HasValue && currentUserId.Value == item.SahipId;
                var canManage = isOwner || isAdmin;

                return new ListingDetailResponse(
                    item.Id,
                    item.SahipId,
                    item.Sahip != null ? item.Sahip.AdSoyad : "Kampüs Üyesi",
                    item.KategoriId,
                    item.Kategori != null ? item.Kategori.Ad : "Genel",
                    item.Baslik,
                    item.Aciklama,
                    item.Tur,
                    item.Yer,
                    item.BulusmaTarihi,
                    item.Durum,
                    item.OlusturulmaTarihi,
                    item.Katilimlar.Count,
                    item.Fotograflar.Select(f => f.DosyaYolu).ToList(),
                    isOwner,
                    canManage
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "İlan detayı veritabanından okunamadı. Bellek içi havuz taranıyor.");
        }

        lock (_sync)
        {
            var inMem = InMemListings.FirstOrDefault(i => i.Id == id);
            if (inMem is null) return null;

            var isOwnerInMem = currentUserId.HasValue && currentUserId.Value == inMem.SahipId;
            var canManageInMem = isOwnerInMem || isAdmin;

            return new ListingDetailResponse(
                inMem.Id,
                inMem.SahipId,
                inMem.Sahip?.AdSoyad ?? "Kampüs Üyesi",
                inMem.KategoriId,
                inMem.Kategori?.Ad ?? DefaultCategories.Items.FirstOrDefault(k => k.Id == inMem.KategoriId)?.Ad ?? "Genel",
                inMem.Baslik,
                inMem.Aciklama,
                inMem.Tur,
                inMem.Yer,
                inMem.BulusmaTarihi,
                inMem.Durum,
                inMem.OlusturulmaTarihi,
                inMem.Katilimlar?.Count ?? 0,
                inMem.Fotograflar?.Select(f => f.DosyaYolu).ToList() ?? new List<string>(),
                isOwnerInMem,
                canManageInMem
            );
        }
    }

    public async Task<IReadOnlyList<ListingSummaryResponse>> GetUserListingsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var list = await db.Ilanlar.AsNoTracking()
                .Include(i => i.Sahip)
                .Include(i => i.Kategori)
                .Include(i => i.Katilimlar)
                .Include(i => i.Fotograflar)
                .Where(i => i.SahipId == userId)
                .OrderByDescending(i => i.OlusturulmaTarihi)
                .Select(i => new ListingSummaryResponse(
                    i.Id,
                    i.SahipId,
                    i.Sahip != null ? i.Sahip.AdSoyad : "Kampüs Üyesi",
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
                    i.Fotograflar.Select(f => f.DosyaYolu).FirstOrDefault()
                ))
                .ToListAsync(ct);

            if (list.Count > 0) return list;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kullanıcı ilanları veritabanından okunamadı. Bellek içi havuz taranıyor.");
        }

        lock (_sync)
        {
            return InMemListings
                .Where(i => i.SahipId == userId)
                .OrderByDescending(i => i.OlusturulmaTarihi)
                .Select(i => new ListingSummaryResponse(
                    i.Id,
                    i.SahipId,
                    i.Sahip?.AdSoyad ?? "Kampüs Üyesi",
                    i.KategoriId,
                    i.Kategori?.Ad ?? DefaultCategories.Items.FirstOrDefault(k => k.Id == i.KategoriId)?.Ad ?? "Genel",
                    i.Baslik,
                    i.Aciklama,
                    i.Tur,
                    i.Yer,
                    i.BulusmaTarihi,
                    i.Durum,
                    i.OlusturulmaTarihi,
                    i.Katilimlar?.Count ?? 0,
                    i.Fotograflar?.Select(f => f.DosyaYolu).FirstOrDefault()
                ))
                .ToList();
        }
    }

    public async Task<ListingOperationResult> CreateListingAsync(
        Guid ownerId, string ownerName, CreateListingRequest request, CancellationToken ct = default)
    {
        var validation = await validator.ValidateCreateAsync(request, ct);
        if (!validation.IsValid)
            return new ListingOperationResult(false, validation.ErrorMessage);

        var normalizedTur = char.ToUpperInvariant(request.Tur[0]) + request.Tur.Substring(1).ToLowerInvariant();
        var catName = DefaultCategories.Items.FirstOrDefault(k => k.Id == request.KategoriId)?.Ad ?? "Genel";

        var ilan = new Ilan
        {
            Id = Guid.NewGuid(),
            SahipId = ownerId,
            KategoriId = request.KategoriId,
            Baslik = request.Baslik.Trim(),
            Aciklama = request.Aciklama.Trim(),
            Tur = normalizedTur,
            Yer = request.Yer.Trim(),
            BulusmaTarihi = normalizedTur == "Sosyal" ? request.BulusmaTarihi?.ToUniversalTime() : null,
            Durum = "Aktif",
            OlusturulmaTarihi = DateTimeOffset.UtcNow,
            Sahip = new Profil { Id = ownerId, AdSoyad = string.IsNullOrWhiteSpace(ownerName) ? "Kampüs Öğrencisi" : ownerName.Trim(), Rol = "Ogrenci" },
            Kategori = new Kategori { Id = request.KategoriId, Ad = catName, Tur = normalizedTur }
        };

        try
        {
            var profileExists = await db.Profiller.AnyAsync(p => p.Id == ownerId, ct);
            if (!profileExists)
            {
                db.Profiller.Add(new Profil
                {
                    Id = ownerId,
                    AdSoyad = string.IsNullOrWhiteSpace(ownerName) ? "Kampüs Öğrencisi" : ownerName.Trim(),
                    Rol = "Ogrenci",
                    OlusturulmaTarihi = DateTimeOffset.UtcNow
                });
            }

            db.Ilanlar.Add(ilan);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Yeni ilan veritabanına kaydedilemedi. Bellek içi ilan listesine ekleniyor.");
            lock (_sync)
            {
                InMemListings.Insert(0, ilan);
            }
        }

        return new ListingOperationResult(true, Id: ilan.Id);
    }

    public async Task<ListingOperationResult> UpdateListingAsync(
        Guid id, Guid currentUserId, bool isAdmin, UpdateListingRequest request, CancellationToken ct = default)
    {
        try
        {
            var ilan = await db.Ilanlar.SingleOrDefaultAsync(i => i.Id == id, ct);
            if (ilan != null)
            {
                if (ilan.SahipId != currentUserId && !isAdmin)
                    return new ListingOperationResult(false, "Bu ilanı düzenleme yetkiniz bulunmamaktadır.", IsForbidden: true);

                var validation = await validator.ValidateUpdateAsync(ilan, request, ct);
                if (!validation.IsValid)
                    return new ListingOperationResult(false, validation.ErrorMessage);

                ilan.Baslik = request.Baslik.Trim();
                ilan.Aciklama = request.Aciklama.Trim();
                ilan.KategoriId = request.KategoriId;
                ilan.Yer = request.Yer.Trim();

                if (string.Equals(ilan.Tur, "Sosyal", StringComparison.OrdinalIgnoreCase))
                {
                    ilan.BulusmaTarihi = request.BulusmaTarihi?.ToUniversalTime();
                }
                else
                {
                    ilan.BulusmaTarihi = null;
                }

                if (!string.IsNullOrWhiteSpace(request.Durum))
                {
                    ilan.Durum = request.Durum;
                }

                await db.SaveChangesAsync(ct);
                return new ListingOperationResult(true, Id: ilan.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "İlan güncellemesi veritabanında başarısız oldu. Bellek içi güncelleniyor.");
        }

        lock (_sync)
        {
            var inMem = InMemListings.FirstOrDefault(i => i.Id == id);
            if (inMem is null)
                return new ListingOperationResult(false, "İlan bulunamadı.", IsNotFound: true);

            if (inMem.SahipId != currentUserId && !isAdmin)
                return new ListingOperationResult(false, "Bu ilanı düzenleme yetkiniz bulunmamaktadır.", IsForbidden: true);

            inMem.Baslik = request.Baslik.Trim();
            inMem.Aciklama = request.Aciklama.Trim();
            inMem.KategoriId = request.KategoriId;
            inMem.Yer = request.Yer.Trim();
            if (string.Equals(inMem.Tur, "Sosyal", StringComparison.OrdinalIgnoreCase))
                inMem.BulusmaTarihi = request.BulusmaTarihi?.ToUniversalTime();
            else
                inMem.BulusmaTarihi = null;

            if (!string.IsNullOrWhiteSpace(request.Durum)) inMem.Durum = request.Durum;

            return new ListingOperationResult(true, Id: inMem.Id);
        }
    }

    public async Task<ListingOperationResult> DeleteListingAsync(
        Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default)
    {
        try
        {
            var ilan = await db.Ilanlar.SingleOrDefaultAsync(i => i.Id == id, ct);
            if (ilan != null)
            {
                if (ilan.SahipId != currentUserId && !isAdmin)
                    return new ListingOperationResult(false, "Bu ilanı silme yetkiniz bulunmamaktadır.", IsForbidden: true);

                db.Ilanlar.Remove(ilan);
                await db.SaveChangesAsync(ct);
                return new ListingOperationResult(true, Id: id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "İlan silme işlemi veritabanında başarısız oldu. Bellek içi havuzdan temizleniyor.");
        }

        lock (_sync)
        {
            var inMem = InMemListings.FirstOrDefault(i => i.Id == id);
            if (inMem is null)
                return new ListingOperationResult(false, "Silinecek ilan bulunamadı.", IsNotFound: true);

            if (inMem.SahipId != currentUserId && !isAdmin)
                return new ListingOperationResult(false, "Bu ilanı silme yetkiniz bulunmamaktadır.", IsForbidden: true);

            InMemListings.Remove(inMem);
            return new ListingOperationResult(true, Id: id);
        }
    }
}
