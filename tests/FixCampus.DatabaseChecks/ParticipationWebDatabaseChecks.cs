using FixCampus.Web.Data;
using FixCampus.Web.Models.Entities;
using FixCampus.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

internal static class ParticipationWebDatabaseChecks
{
    public static async Task RunAsync(FixCampusDbContext db, Guid owner, Guid guest, CancellationToken ct)
    {
        var now = new DateTimeOffset(2030, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var socialCat = await db.Kategoriler.AsNoTracking().FirstAsync(x => x.Tur == "Sosyal", ct);

        var activeMeetup = new Ilan
        {
            Id = Guid.NewGuid(),
            SahipId = owner,
            KategoriId = socialCat.Id,
            Tur = "Sosyal",
            Baslik = "Hafta 5 Web Katılım Testi",
            Aciklama = "Web servis katılımcı ve gizlilik doğrulaması.",
            Yer = "Merkez Kütüphane 2. Kat",
            BulusmaTarihi = now.AddHours(6),
            Durum = "Aktif",
            OlusturulmaTarihi = now.AddMinutes(-10)
        };
        var expiredMeetup = new Ilan
        {
            Id = Guid.NewGuid(),
            SahipId = owner,
            KategoriId = socialCat.Id,
            Tur = "Sosyal",
            Baslik = "Hafta 5 Süresi Dolmuş Buluşma",
            Aciklama = "Süresi dolan buluşmaya katılım engellenmelidir.",
            Yer = "Kantin",
            BulusmaTarihi = now.AddMinutes(-5),
            Durum = "Aktif",
            OlusturulmaTarihi = now.AddDays(-2)
        };
        db.Ilanlar.AddRange(activeMeetup, expiredMeetup);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        var service = new ListingService(db, new ListingValidator(db, clock), NullLogger<ListingService>.Instance, clock);

        var firstJoin = await service.JoinListingAsync(activeMeetup.Id, guest, ct: ct);
        Check(firstJoin.Success && firstJoin.IsJoined && firstJoin.ParticipantCount == 1,
            "Web: Geliyorum işlemi katılımcıyı kaydetmelidir.");

        var duplicateJoin = await service.JoinListingAsync(activeMeetup.Id, guest, ct: ct);
        Check(!duplicateJoin.Success && duplicateJoin.IsConflict && duplicateJoin.ParticipantCount == 1,
            "Web: Aynı kullanıcının tekrar katılması çakışma (conflict) olarak reddedilmelidir.");

        var expiredJoin = await service.JoinListingAsync(expiredMeetup.Id, guest, ct: ct);
        Check(!expiredJoin.Success, "Web: Süresi biten buluşmaya katılım reddedilmelidir.");

        var feedForGuest = await service.GetListingsAsync("Sosyal", null, "Hafta 5 Web Katılım", 1, 12, guest, ct);
        Check(feedForGuest.Items.Single().Id == activeMeetup.Id && feedForGuest.Items.Single().IsJoined,
            "Web: Arama/liste ekranında kullanıcının katıldığı buluşmada IsJoined true dönmelidir.");

        var ownerDetail = await service.GetListingByIdAsync(activeMeetup.Id, owner, isAdmin: false, ct);
        var guestDetail = await service.GetListingByIdAsync(activeMeetup.Id, guest, isAdmin: false, ct);
        Check(ownerDetail is not null && ownerDetail.IsOwner && ownerDetail.Katilimcilar.Count == 1,
            "Web: İlan sahibi kendi buluşmasının katılımcı listesini görmelidir.");
        Check(guestDetail is not null && !guestDetail.IsOwner && guestDetail.IsJoined && guestDetail.Katilimcilar.Count == 0,
            "Web: İlan sahibi olmayan kullanıcı IsJoined görmeli ama katılımcı listesini görememelidir.");

        var leaveResult = await service.LeaveListingAsync(activeMeetup.Id, guest, ct);
        Check(leaveResult.Success && !leaveResult.IsJoined && leaveResult.ParticipantCount == 0,
            "Web: Katılımı geri alma işlemi kaydı kaldırmalıdır.");

        db.Ilanlar.RemoveRange(await db.Ilanlar.Where(x => x.Id == activeMeetup.Id || x.Id == expiredMeetup.Id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        Console.WriteLine("PASS: PostgreSQL web buluşma katılımı, tekrar engeli, süre sınırı, IsJoined ve ilan sahibi katılımcı gizliliği.");
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
