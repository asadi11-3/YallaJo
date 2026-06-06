namespace YallaJo.Web.Areas.Creator.Models.Application;

/// <summary>
/// Outbound body for PUT /api/v1/blogs/creators/applications/{id} (edit a
/// Draft/MoreInfoNeeded application). Same shape as the create body — the backend
/// <c>UpdateCreatorApplicationRequest</c> is identical to the create request.
/// </summary>
public sealed record UpdateCreatorApplicationRequestBody(
    string? Bio,
    IReadOnlyList<string> PortfolioUrls,
    IReadOnlyList<string> SampleWorkUrls,
    IReadOnlyList<Guid> NicheIds,
    IReadOnlyList<string> FreeTags,
    IReadOnlyList<Guid> LanguageIds,
    IReadOnlyList<Guid> PreferredRegionIds,
    IReadOnlyDictionary<string, string> SocialHandles);
