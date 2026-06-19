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

// ── Tier Management ──────────────────────────────────────────────────

public sealed record PromoteCreatorTierRequest(ContentBlogs.Domain.Enums.CreatorTrustTier TargetTier);

public sealed record DemoteCreatorTierRequest(
    ContentBlogs.Domain.Enums.CreatorTrustTier TargetTier,
    string Reason);

// ── Blog-CreatorPost Merger: Admin Profile Management ────────────────

public sealed record AdminUpdateCreatorProfileRequest(
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Slug);

public sealed record AdminDeleteCreatorProfileRequest(string Reason);
