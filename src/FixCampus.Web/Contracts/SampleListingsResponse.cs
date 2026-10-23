namespace FixCampus.Web.Contracts;

/// <summary>Web ve mobil bağlantı denemeleri için kalıcı olmayan örnek yanıt.</summary>
public sealed record SampleListingsResponse(
    string DataSource,
    bool IsPersisted,
    IReadOnlyList<SampleListing> Items);

/// <summary>Nihai veritabanı modelinden bağımsız bir ekran verisi örneği.</summary>
public sealed record SampleListing(
    Guid Id,
    string Type,
    string Title,
    string Description,
    string Location,
    DateTimeOffset? StartsAtUtc);
