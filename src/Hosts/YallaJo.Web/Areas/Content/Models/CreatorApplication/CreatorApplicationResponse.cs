namespace YallaJo.Web.Areas.Content.Models.CreatorApplication;

public sealed class CreatorApplicationResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public IReadOnlyList<string> PortfolioUrls { get; init; } = [];
    public IReadOnlyList<string> SampleWorkUrls { get; init; } = [];
    public IReadOnlyList<Guid> NicheIds { get; init; } = [];
    public IReadOnlyList<string> FreeTags { get; init; } = [];
    public IReadOnlyList<Guid> LanguageIds { get; init; } = [];
    public IReadOnlyList<Guid> PreferredRegionIds { get; init; } = [];
    public IReadOnlyDictionary<string, string> SocialHandles { get; init; } = new Dictionary<string, string>();
    public string? AdminNote { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
