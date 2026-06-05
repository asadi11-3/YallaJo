namespace YallaJo.Web.Areas.Creator.Models.Dashboard;

public sealed class CreatorApplicationMineResponse
{
    public Guid Id { get; init; }
    public Guid ApplicantUserId { get; init; }
    public string? Bio { get; init; }

    public string? Status { get; init; }

    public string? Source { get; init; }

    public IReadOnlyList<string> PortfolioUrls { get; init; } = [];
    public IReadOnlyList<string> SampleWorkUrls { get; init; } = [];
    public IReadOnlyList<Guid> NicheIds { get; init; } = [];
    public IReadOnlyList<string> FreeTags { get; init; } = [];
    public IReadOnlyList<Guid> LanguageIds { get; init; } = [];
    public IReadOnlyList<Guid> PreferredRegionIds { get; init; } = [];
    public IReadOnlyDictionary<string, string> SocialHandles { get; init; }
        = new Dictionary<string, string>();

    public string? AdminNote { get; init; }
    public int ReapplicationCount { get; init; }
    public DateTime? LastRejectedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
}
