using FixCampus.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Services;

/// <summary>Web ve mobil API için ortak filtreler ve kararlı sayfalama.</summary>
public static class ListingQueries
{
    public static IQueryable<Ilan> Filter(IQueryable<Ilan> query, bool postgres,
        string? tur, Guid? kategoriId, string? search)
    {
        query = query.Where(x => x.Durum != "Gizli");
        if (!string.IsNullOrWhiteSpace(tur))
        {
            var type = tur.Trim();
            query = query.Where(x => x.Tur == type);
        }
        if (kategoriId.HasValue && kategoriId.Value != Guid.Empty)
            query = query.Where(x => x.KategoriId == kategoriId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            if (postgres)
            {
                var pattern = "%" + text.Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("%", "\\%", StringComparison.Ordinal)
                    .Replace("_", "\\_", StringComparison.Ordinal) + "%";
                query = query.Where(x => EF.Functions.ILike(x.Baslik, pattern, "\\") ||
                    EF.Functions.ILike(x.Aciklama, pattern, "\\") || EF.Functions.ILike(x.Yer, pattern, "\\"));
            }
            else
            {
                var lower = text.ToLowerInvariant();
                query = query.Where(x => x.Baslik.ToLower().Contains(lower) ||
                    x.Aciklama.ToLower().Contains(lower) || x.Yer.ToLower().Contains(lower));
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
