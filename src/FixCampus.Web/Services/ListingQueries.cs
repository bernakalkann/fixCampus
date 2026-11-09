using FixCampus.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Services;

/// <summary>Web ve mobil API için ortak filtreler ve kararlı sayfalama.</summary>
public static class ListingQueries
{
    public static IQueryable<Ilan> Filter(IQueryable<Ilan> query, bool postgres,
        string? tur, Guid? kategoriId, string? search,
        bool lostFoundOnly = false, bool activeSocialOnly = false)
    {
        query = query.Where(x => x.Durum != "Gizli");
        if (lostFoundOnly) query = query.Where(x => x.Tur == "Kayip" || x.Tur == "Buluntu");
        if (activeSocialOnly) query = query.Where(x => x.Tur != "Sosyal" || x.Durum == "Aktif");
        if (!string.IsNullOrWhiteSpace(tur))
        {
            var type = tur.Trim();
            query = query.Where(x => x.Tur == type);
        }
        if (kategoriId.HasValue && kategoriId.Value != Guid.Empty)
            query = query.Where(x => x.KategoriId == kategoriId.Value);
        foreach (var token in (search ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var text = token.Replace("İ", "i", StringComparison.Ordinal)
                .Replace("I", "ı", StringComparison.Ordinal).ToLowerInvariant();
            if (postgres)
            {
                var pattern = "%" + text.Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("%", "\\%", StringComparison.Ordinal)
                    .Replace("_", "\\_", StringComparison.Ordinal) + "%";
                query = query.Where(x =>
                    EF.Functions.ILike(x.Baslik.Replace("İ", "i").Replace("I", "ı").ToLower(), pattern, "\\") ||
                    EF.Functions.ILike(x.Aciklama.Replace("İ", "i").Replace("I", "ı").ToLower(), pattern, "\\") ||
                    EF.Functions.ILike(x.Yer.Replace("İ", "i").Replace("I", "ı").ToLower(), pattern, "\\") ||
                    (x.Kategori != null && EF.Functions.ILike(x.Kategori.Ad.Replace("İ", "i").Replace("I", "ı").ToLower(), pattern, "\\")));
            }
            else
            {
                query = query.Where(x =>
                    x.Baslik.Replace("İ", "i").Replace("I", "ı").ToLower().Contains(text) ||
                    x.Aciklama.Replace("İ", "i").Replace("I", "ı").ToLower().Contains(text) ||
                    x.Yer.Replace("İ", "i").Replace("I", "ı").ToLower().Contains(text) ||
                    (x.Kategori != null && x.Kategori.Ad.Replace("İ", "i").Replace("I", "ı").ToLower().Contains(text)));
            }
        }
        return query;
    }

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize) =>
        (Math.Max(1, page ?? 1), pageSize is null or < 1 ? 12 : Math.Min(pageSize.Value, 50));

    public static IOrderedQueryable<Ilan> Order(IQueryable<Ilan> query) =>
        query.OrderByDescending(x => x.OlusturulmaTarihi).ThenBy(x => x.Id);

    public static IQueryable<Ilan> Page(IQueryable<Ilan> query, int page, int pageSize)
    {
        // int.MaxValue gibi girişlerde çarpım taşması ilk sayfayı döndürmesin.
        var offset = ((long)page - 1) * pageSize;
        return offset > int.MaxValue ? Order(query).Where(_ => false) :
            Order(query).Skip((int)offset).Take(pageSize);
    }
}
