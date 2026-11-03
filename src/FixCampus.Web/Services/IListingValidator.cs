using FixCampus.Web.Contracts;
using FixCampus.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Services;

public sealed record ListingValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ListingValidationResult Success() => new(true);
    public static ListingValidationResult Fail(string error) => new(false, error);
}

public interface IListingValidator
{
    Task<ListingValidationResult> ValidateCreateAsync(CreateListingRequest request, CancellationToken ct = default);
    Task<ListingValidationResult> ValidateUpdateAsync(UpdateListingRequest request, string existingTur, CancellationToken ct = default);
}

public sealed class ListingValidator(FixCampusDbContext db, TimeProvider? clock = null) : IListingValidator
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "Sosyal",
        "Kayip",
        "Buluntu"
    };

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ListingValidationResult> ValidateCreateAsync(CreateListingRequest request, CancellationToken ct = default)
    {
        return await ValidateCommonAsync(
            request.Baslik,
            request.Aciklama,
            request.Tur,
            request.KategoriId,
            request.Yer,
            request.BulusmaTarihi,
            ct);
    }

    public async Task<ListingValidationResult> ValidateUpdateAsync(UpdateListingRequest request, string existingTur, CancellationToken ct = default)
    {
        var effectiveTur = string.IsNullOrWhiteSpace(request.Tur) ? existingTur : request.Tur;

        return await ValidateCommonAsync(
            request.Baslik,
            request.Aciklama,
            effectiveTur,
            request.KategoriId,
            request.Yer,
            request.BulusmaTarihi,
            ct);
    }

    private async Task<ListingValidationResult> ValidateCommonAsync(
        string? baslik,
        string? aciklama,
        string? tur,
        Guid kategoriId,
        string? yer,
        DateTimeOffset? bulusmaTarihi,
        CancellationToken ct)
    {
        var trimmedTitle = baslik?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedTitle) || trimmedTitle.Length < 2 || baslik!.Length > 150)
        {
            return ListingValidationResult.Fail("Başlık alanı zorunludur ve 2 ile 150 karakter arasında olmalıdır.");
        }

        var trimmedDescription = aciklama?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedDescription) || aciklama!.Length > 3000)
        {
            return ListingValidationResult.Fail("Açıklama alanı zorunludur ve en fazla 3000 karakter olabilir.");
        }

        var trimmedPlace = yer?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedPlace) || yer!.Length > 200)
        {
            return ListingValidationResult.Fail("Yer/Konum alanı zorunludur ve en fazla 200 karakter olabilir.");
        }

        var trimmedType = tur?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedType) || !AllowedTypes.Contains(trimmedType))
        {
            return ListingValidationResult.Fail("İlan türü yalnızca 'Sosyal', 'Kayip' veya 'Buluntu' olabilir.");
        }

        if (trimmedType == "Sosyal")
        {
            if (bulusmaTarihi is null)
            {
                return ListingValidationResult.Fail("Sosyal buluşma ilanları için buluşma tarihi ve saati zorunludur.");
            }

            if (bulusmaTarihi.Value <= _clock.GetUtcNow())
            {
                return ListingValidationResult.Fail("Sosyal buluşma tarihi gelecekte bir zaman olmalıdır.");
            }
        }
        else if (trimmedType is "Kayip" or "Buluntu")
        {
            if (bulusmaTarihi is not null)
            {
                return ListingValidationResult.Fail("Kayıp ve buluntu ilanlarında buluşma tarihi belirtilemez (boş bırakılmalıdır).");
            }
        }

        if (kategoriId == Guid.Empty)
        {
            return ListingValidationResult.Fail("Geçerli bir kategori seçilmelidir.");
        }

        Models.Entities.Kategori? kategori;
        try
        {
            kategori = await db.Kategoriler
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == kategoriId, ct);
        }
        catch
        {
            return ListingValidationResult.Fail("Kategori doğrulanamadı; veritabanı bağlantısı kurulamadı.");
        }

        if (kategori is null)
        {
            return ListingValidationResult.Fail("Seçilen kategori sistemde bulunamadı.");
        }

        if (!string.Equals(kategori.Tur, trimmedType, StringComparison.Ordinal))
        {
            return ListingValidationResult.Fail($"Seçilen kategori '{kategori.Tur}' türüne aittir, '{trimmedType}' ilanı ile eşleşmiyor.");
        }

        return ListingValidationResult.Success();
    }
}
