using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Services;

public sealed record ValidationOutcome(bool IsValid, string? ErrorMessage = null)
{
    public static ValidationOutcome Success() => new(true);
    public static ValidationOutcome Failed(string message) => new(false, message);
}

public interface IListingValidator
{
    Task<ValidationOutcome> ValidateCreateAsync(CreateListingRequest request, CancellationToken ct = default);
    Task<ValidationOutcome> ValidateUpdateAsync(Ilan existing, UpdateListingRequest request, CancellationToken ct = default);
}

public sealed class ListingValidator(FixCampusDbContext db) : IListingValidator
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sosyal", "Kayip", "Buluntu"
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Aktif", "Cozuldu", "Gizli"
    };

    public async Task<ValidationOutcome> ValidateCreateAsync(CreateListingRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Baslik) || request.Baslik.Trim().Length < 3 || request.Baslik.Length > 150)
            return ValidationOutcome.Failed("İlan başlığı 3 ile 150 karakter arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(request.Aciklama) || request.Aciklama.Trim().Length < 5 || request.Aciklama.Length > 3000)
            return ValidationOutcome.Failed("İlan açıklaması 5 ile 3000 karakter arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(request.Yer) || request.Yer.Trim().Length < 2 || request.Yer.Length > 200)
            return ValidationOutcome.Failed("Konum/Yer bilgisi 2 ile 200 karakter arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(request.Tur) || !ValidTypes.Contains(request.Tur))
            return ValidationOutcome.Failed("Geçersiz ilan türü. İlan türü 'Sosyal', 'Kayip' veya 'Buluntu' olmalıdır.");

        var turNormalized = char.ToUpperInvariant(request.Tur[0]) + request.Tur.Substring(1).ToLowerInvariant();
        if (turNormalized == "Sosyal")
        {
            if (request.BulusmaTarihi is null)
                return ValidationOutcome.Failed("Sosyal buluşma ilanları için buluşma tarihi zorunludur.");

            if (request.BulusmaTarihi.Value <= DateTimeOffset.UtcNow)
                return ValidationOutcome.Failed("Sosyal buluşma tarihi ileri bir tarih ve saat olmalıdır.");
        }
        else
        {
            if (request.BulusmaTarihi is not null)
                return ValidationOutcome.Failed("Kayıp ve buluntu ilanlarında buluşma tarihi belirtilemez.");
        }

        // Kategori varlığı ve tür uyumu denetimi
        Kategori? category = null;
        try
        {
            category = await db.Kategoriler.AsNoTracking().SingleOrDefaultAsync(k => k.Id == request.KategoriId, ct);
        }
        catch
        {
            // Veritabanı çevrimdışıysa varsayılan katalogdan kontrol et
        }
        category ??= DefaultCategories.Items.FirstOrDefault(k => k.Id == request.KategoriId);

        if (category is null)
            return ValidationOutcome.Failed("Seçilen kategori sistemde bulunamadı.");

        if (!string.Equals(category.Tur, request.Tur, StringComparison.OrdinalIgnoreCase))
            return ValidationOutcome.Failed($"Seçilen kategori '{category.Tur}' türündedir, ancak ilan '{request.Tur}' türünde açılmaktadır. Kategori ve ilan türü eşleşmelidir.");

        return ValidationOutcome.Success();
    }

    public async Task<ValidationOutcome> ValidateUpdateAsync(Ilan existing, UpdateListingRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Baslik) || request.Baslik.Trim().Length < 3 || request.Baslik.Length > 150)
            return ValidationOutcome.Failed("İlan başlığı 3 ile 150 karakter arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(request.Aciklama) || request.Aciklama.Trim().Length < 5 || request.Aciklama.Length > 3000)
            return ValidationOutcome.Failed("İlan açıklaması 5 ile 3000 karakter arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(request.Yer) || request.Yer.Trim().Length < 2 || request.Yer.Length > 200)
            return ValidationOutcome.Failed("Konum/Yer bilgisi 2 ile 200 karakter arasında olmalıdır.");

        if (!string.IsNullOrWhiteSpace(request.Durum) && !ValidStatuses.Contains(request.Durum))
            return ValidationOutcome.Failed("Geçersiz ilan durumu. Durum 'Aktif', 'Cozuldu' veya 'Gizli' olmalıdır.");

        if (string.Equals(existing.Tur, "Sosyal", StringComparison.OrdinalIgnoreCase))
        {
            if (request.BulusmaTarihi is null)
                return ValidationOutcome.Failed("Sosyal buluşma ilanları için buluşma tarihi zorunludur.");
        }
        else
        {
            if (request.BulusmaTarihi is not null)
                return ValidationOutcome.Failed("Kayıp ve buluntu ilanlarında buluşma tarihi belirtilemez.");
        }

        // Kategori varlığı ve tür uyumu denetimi
        Kategori? category = null;
        try
        {
            category = await db.Kategoriler.AsNoTracking().SingleOrDefaultAsync(k => k.Id == request.KategoriId, ct);
        }
        catch
        {
            // Veritabanı çevrimdışıysa varsayılan katalogdan kontrol et
        }
        category ??= DefaultCategories.Items.FirstOrDefault(k => k.Id == request.KategoriId);

        if (category is null)
            return ValidationOutcome.Failed("Seçilen kategori sistemde bulunamadı.");

        if (!string.Equals(category.Tur, existing.Tur, StringComparison.OrdinalIgnoreCase))
            return ValidationOutcome.Failed($"Seçilen kategori '{category.Tur}' türündedir, ancak ilan '{existing.Tur}' türündedir. Kategori ve ilan türü eşleşmelidir.");

        return ValidationOutcome.Success();
    }
}

