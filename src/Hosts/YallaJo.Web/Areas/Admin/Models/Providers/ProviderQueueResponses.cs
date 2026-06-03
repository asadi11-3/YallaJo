namespace YallaJo.Web.Areas.Admin.Models.Providers;

/// <summary>
/// Mirrors <c>GetAdminProviderQueueResult</c> from <c>GET /api/v1/admin/providers</c>.
/// Enum-typed API fields are modeled as strings (the API serializes enums as strings).
/// </summary>
public sealed class ProviderQueueResponse
{
    public List<ProviderQueueItemResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>Mirrors <c>ProviderApplicationSummary</c>.</summary>
public sealed class ProviderQueueItemResponse
{
    public Guid ApplicationId { get; init; }
    public Guid UserId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string BusinessName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public int DocumentCount { get; init; }
    public int ReapplicationCount { get; init; }
}
