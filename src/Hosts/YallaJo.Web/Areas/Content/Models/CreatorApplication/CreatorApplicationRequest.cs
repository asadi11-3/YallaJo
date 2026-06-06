namespace YallaJo.Web.Areas.Content.Models.CreatorApplication;

public sealed record CreateCreatorApplicationApiRequest(
    string? Bio,
    List<string>? PortfolioUrls,
    List<string>? SampleWorkUrls,
    List<Guid>? NicheIds,
    List<string>? FreeTags,
    List<Guid>? LanguageIds,
    List<Guid>? PreferredRegionIds,
    Dictionary<string, string>? SocialHandles);

public sealed record UpdateCreatorApplicationApiRequest(
    string? Bio,
    List<string>? PortfolioUrls,
    List<string>? SampleWorkUrls,
    List<Guid>? NicheIds,
    List<string>? FreeTags,
    List<Guid>? LanguageIds,
    List<Guid>? PreferredRegionIds,
    Dictionary<string, string>? SocialHandles);

public sealed record RedeemCreatorInvitationApiRequest(string Token);
