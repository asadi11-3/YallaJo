namespace YallaJo.Web.Areas.Provider.Models.Tours;

public sealed class PlaceOptionVm
{
    public Guid    Id         { get; init; }
    public string  Name       { get; init; } = string.Empty;
    public string? City       { get; init; }
    public string? Country    { get; init; }
    public bool    IsVerified { get; init; }

    public string DisplayLabel
    {
        get
        {
            var location = (City, Country) switch
            {
                ({ Length: > 0 } c, { Length: > 0 } co) => $"{c}, {co}",
                ({ Length: > 0 } c, _)                   => c,
                (_, { Length: > 0 } co)                  => co,
                _                                        => null,
            };

            var label = location is null ? Name : $"{Name} — {location}";
            return IsVerified ? label : $"{label} (unverified)";
        }
    }
}
