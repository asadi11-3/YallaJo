namespace YallaJo.Web.Infrastructure.Api;

public static class PlaceDisplayFormatter
{
 
    public const string NotSpecified = "Place not specified";

    public static string Format(string? name, string? city, string? country)
    {
        if (string.IsNullOrWhiteSpace(name))
            return NotSpecified;

        var location = (city?.Trim(), country?.Trim()) switch
        {
            ({ Length: > 0 } c, { Length: > 0 } co) => $"{c}, {co}",
            ({ Length: > 0 } c, _)                   => c,
            (_, { Length: > 0 } co)                  => co,
            _                                        => null,
        };

        return location is null ? name.Trim() : $"{name.Trim()} — {location}";
    }

    public static string UnresolvedLabel(Guid placeId) =>
        $"Place {placeId.ToString()[..8]}";
}
