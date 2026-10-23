namespace FixCampus.Web.Contracts;

/// <summary>Uygulamanın çalıştığını bildirir; veritabanı bağlantı testi yapmaz.</summary>
public sealed record ApiStatusResponse(
    string Application,
    string Status,
    string Stage,
    DateTimeOffset CheckedAtUtc);
