namespace YallaJo.Web.Areas.Creator.Models.Application;

public sealed record CreateCreatorApplicationRequestBody(
    string? Bio,
    IReadOnlyList<string> PortfolioUrls,
    IReadOnlyList<string> SampleWorkUrls,
    IReadOnlyList<Guid> NicheIds,
    IReadOnlyList<string> FreeTags,
    IReadOnlyList<Guid> LanguageIds,
    IReadOnlyList<Guid> PreferredRegionIds,
    IReadOnlyDictionary<string, string> SocialHandles);
