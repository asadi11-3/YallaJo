namespace YallaJo.Web.Areas.Admin.Models.Creators;

public sealed class CreatorApplicationPageResponse
{
    public IReadOnlyList<CreatorApplicationSummaryResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>Mirrors the API's CreatorApplicationStatusCountsDto (GET /applications/status-counts).</summary>
public sealed class CreatorApplicationStatusCountsResponse
{
    public int Draft { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int MoreInfoNeeded { get; set; }
}

public sealed class CreatorApplicationSummaryResponse
{
    public Guid Id { get; set; }
    public Guid ApplicantUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int ReapplicationCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreatorApplicationDetailResponse
{
    public Guid Id { get; set; }
    public Guid ApplicantUserId { get; set; }
    public string? Bio { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public IReadOnlyList<string> PortfolioUrls { get; set; } = [];
    public IReadOnlyList<string> SampleWorkUrls { get; set; } = [];
    public IReadOnlyList<string> FreeTags { get; set; } = [];
    public IReadOnlyDictionary<string, string> SocialHandles { get; set; } = new Dictionary<string, string>();
    public string? AdminNote { get; set; }
    public int ReapplicationCount { get; set; }
    public DateTime? LastRejectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
