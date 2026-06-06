namespace YallaJo.Web.Areas.Public.Models.Agencies;

/// <summary>List-item payload from GET /api/v1/agency (AgencyListItemDto).</summary>
public sealed class AgencyListItemResponse
{
    public Guid UserId { get; init; }
    public string BusinessName { get; init; } = "";
    public string ContactEmail { get; init; } = "";
    public string? Description { get; init; }
    public DateTime ApprovedAt { get; init; }
}

/// <summary>Detail payload from GET /api/v1/agency/{agencyUserId} (AgencyDetailDto).</summary>
public sealed class AgencyDetailResponse
{
    public Guid UserId { get; init; }
    public string BusinessName { get; init; } = "";
    public string ContactEmail { get; init; } = "";
    public string ContactPhone { get; init; } = "";
    public string? Address { get; init; }
    public string? Description { get; init; }
    public string? ProviderType { get; init; }
    public int ActiveGuideCount { get; init; }
    public DateTime ApprovedAt { get; init; }
}

/// <summary>
/// List wrapper for GET /api/v1/agency. The API returns GetAgenciesResult(Agencies, TotalCount)
/// — note the property is "Agencies" (not "Items") and only TotalCount is echoed (no Page/PageSize).
/// </summary>
public sealed class PaginatedAgenciesResponse
{
    public List<AgencyListItemResponse> Agencies { get; init; } = [];
    public int TotalCount { get; init; }
}
