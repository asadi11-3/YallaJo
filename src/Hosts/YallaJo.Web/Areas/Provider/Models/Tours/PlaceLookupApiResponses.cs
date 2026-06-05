namespace YallaJo.Web.Areas.Provider.Models.Tours;

public sealed class PaginatedPlaceLookupResponse
{
    public List<PlaceLookupItemResponse> Items    { get; init; } = [];
    public int                           PageSize { get; init; }
    public int                           TotalCount { get; init; }
    public int                           TotalPages { get; init; }
}

public sealed class PlaceLookupItemResponse
{
    public Guid    Id         { get; init; }
    public string  Name       { get; init; } = string.Empty;
    public string? City       { get; init; }
    public string? Country    { get; init; }
    public bool    IsVerified { get; init; }
}
