namespace FixCampus.Web.Services;

internal static class ListingMapLink
{
    public static string? SearchUrl(string? place)
    {
        if (string.IsNullOrWhiteSpace(place)) return null;

        return $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(place.Trim())}";
    }
}
