namespace FixCampus.Web.Models;

public sealed record ListingLoadErrorViewModel(string Message, string Action,
    string? Tur = null, Guid? KategoriId = null, string? Query = null, int? Page = null);