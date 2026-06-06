namespace YallaJo.Web.Areas.Public.Models.Agencies;

public sealed class AgencyCardVm
{
    public Guid UserId { get; init; }
    public string BusinessName { get; init; } = "";
    public string? Description { get; init; }
}

public sealed class AgenciesGridVm
{
    public IReadOnlyList<AgencyCardVm> Agencies { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasResults => Agencies.Count > 0;
}

public sealed class AgencyDetailVm
{
    public Guid UserId { get; init; }
    public string BusinessName { get; init; } = "";
    public string ContactEmail { get; init; } = "";
    public string ContactPhone { get; init; } = "";
    public string? Address { get; init; }
    public string? Description { get; init; }
    public string? ProviderType { get; init; }
    public int ActiveGuideCount { get; init; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool HasContact =>
        !string.IsNullOrWhiteSpace(ContactEmail)
        || !string.IsNullOrWhiteSpace(ContactPhone)
        || !string.IsNullOrWhiteSpace(Address);
}
