namespace ContentBlogs.Presentation.Endpoints.Creator.Models;

public sealed record CreateCreatorApplicationRequest(
    string? Bio,
    List<string>? PortfolioUrls,
    List<string>? SampleWorkUrls,
    List<Guid>? NicheIds,
    List<string>? FreeTags,
    List<Guid>? LanguageIds,
    List<Guid>? PreferredRegionIds,
    Dictionary<string, string>? SocialHandles);

public sealed record UpdateCreatorApplicationRequest(
    string? Bio,
    List<string>? PortfolioUrls,
    List<string>? SampleWorkUrls,
    List<Guid>? NicheIds,
    List<string>? FreeTags,
    List<Guid>? LanguageIds,
    List<Guid>? PreferredRegionIds,
    Dictionary<string, string>? SocialHandles);

public sealed record UpdateCreatorProfileRequest(
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? NewSlug);

public sealed record RedeemCreatorInvitationRequest(string Token);

public sealed record ApproveApplicationRequest(
    string DisplayName,
    string? AvatarUrl);

public sealed record RejectApplicationRequest(string Reason);

public sealed record RequestMoreInfoRequest(string AdminNote);

public sealed record SuspendProfileRequest(string Reason);

public sealed record SendInvitationRequest(
    ContentBlogs.Domain.Enums.CreatorInvitationKind Kind,
    string? Email,
    Guid? InvitedUserId,
    string? PersonalMessage);

// ── Wave 8: Creator Posts ────────────────────────────────────────────

public sealed record CreateCreatorPostRequest(
    ContentBlogs.Domain.Enums.CreatorPostType PostType,
    string Title,
    string Excerpt,
    Guid LanguageId,
    string? TypeSpecificDataJson,
    List<Guid>? NicheIds,
    List<string>? FreeTags);

public sealed record UpdateCreatorPostRequest(
    string Title,
    string Excerpt,
    string? Body,
    string? TypeSpecificDataJson);

public sealed record RejectCreatorPostRequest(string Reason);

public sealed record RemoveCreatorPostRequest(string Reason);

public sealed record FeatureCreatorPostRequest(DateTime? FeaturedUntil);

public sealed record PromoteCreatorTierRequest(ContentBlogs.Domain.Enums.CreatorTrustTier TargetTier);

public sealed record DemoteCreatorTierRequest(
    ContentBlogs.Domain.Enums.CreatorTrustTier TargetTier,
    string Reason);
