namespace YallaJo.Web.Areas.Admin.Models.Creators;

public sealed class CreatorsVm
{
    public IReadOnlyList<CreatorApplicationRowVm> Applications { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? StatusFilter { get; set; }
    public Guid? LookupId { get; set; }
    public CreatorApplicationDetailVm? Detail { get; set; }
}

public sealed class CreatorApplicationRowVm
{
    public Guid Id { get; set; }
    public Guid ApplicantUserId { get; set; }

    /// <summary>Human-readable applicant identity (email) resolved server-side; null when not resolvable (F10 — never show the raw GUID).</summary>
    public string? ApplicantEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int ReapplicationCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreatorApplicationDetailVm
{
    public Guid Id { get; set; }
    public Guid ApplicantUserId { get; set; }

    /// <summary>Human-readable applicant identity (email) resolved server-side; null when not resolvable (F10).</summary>
    public string? ApplicantEmail { get; set; }
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
