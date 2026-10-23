using FixCampus.Web.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Data;

public sealed class DemoListingReader(FixCampusDbContext db)
{
    public Task<DatabaseListingResponse?> ReadAsync(CancellationToken cancellationToken = default) =>
        db.Ilanlar.AsNoTracking()
            .Where(x => x.Id == DemoListingIds.Listing && x.Durum == "Aktif")
            .Select(x => new DatabaseListingResponse(
                x.Id, x.Baslik, x.Aciklama, x.Tur, x.Yer, x.Kategori.Ad,
                x.Durum, x.OlusturulmaTarihi, "database", true))
            .SingleOrDefaultAsync(cancellationToken);
}
