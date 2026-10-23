using FixCampus.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixCampus.Web.Data;

public static class DemoListingSeeder
{
    // Explicit local-demo command only. No anonymous HTTP endpoint creates data.
    public static async Task SeedAsync(
        FixCampusDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(configuration["Demo:ProfileId"], out var profileId) || profileId == Guid.Empty)
            throw new InvalidOperationException(
                "Demo:ProfileId eksik. Supabase Authentication kullanıcısının UUID değerini User Secrets ile tanımlayın.");

        // Do not create an orphan profile; use an actual Supabase Auth identity.
        var exists = await db.Database.SqlQuery<bool>(
            $"SELECT EXISTS (SELECT 1 FROM auth.users WHERE id = {profileId}) AS \"Value\"")
            .SingleAsync(cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Demo:ProfileId Supabase auth.users içinde bulunamadı.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await db.Profiller.AnyAsync(x => x.Id == profileId, cancellationToken))
            db.Profiller.Add(new Profil { Id = profileId, AdSoyad = "Demo Öğrenci", Rol = "Ogrenci" });

        var category = await db.Kategoriler.SingleOrDefaultAsync(
            x => x.Ad == "Genel" && x.Tur == "Buluntu", cancellationToken);
        if (category is null)
        {
            category = new Kategori { Id = DemoListingIds.Category, Ad = "Genel", Tur = "Buluntu" };
            db.Kategoriler.Add(category);
        }

        if (!await db.Ilanlar.AnyAsync(x => x.Id == DemoListingIds.Listing, cancellationToken))
            db.Ilanlar.Add(new Ilan
            {
                Id = DemoListingIds.Listing,
                SahipId = profileId,
                KategoriId = category.Id,
                Baslik = "[Demo] Kütüphanede bulunan defter",
                Aciklama = "Supabase bağlantısını göstermek için veritabanına kaydedilen tek örnek ilan.",
                Tur = "Buluntu",
                Yer = "Kampüs kütüphanesi",
                BulusmaTarihi = null,
                Durum = "Aktif"
            });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
