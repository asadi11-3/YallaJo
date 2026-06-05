namespace YallaJo.Web.Areas.Public.Models.Tours;

public sealed class PlaceLookupResponse
{
    public Guid    Id         { get; init; }
    public string  Name       { get; init; } = string.Empty;
    public string? City       { get; init; }
    public string? Country    { get; init; }
    public bool    IsVerified { get; init; }
}
