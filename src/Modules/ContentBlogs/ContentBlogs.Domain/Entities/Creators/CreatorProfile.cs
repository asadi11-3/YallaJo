using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Events.Creators;
using YallaJo.SharedKernel.Domain.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities.Creators;

/// <summary>
/// Aggregate root representing an approved content creator's profile.
/// Created automatically upon <see cref="CreatorApplication"/> approval.
/// Wave 7 – Content Creator Module.
/// </summary>
public sealed class CreatorProfile : AuditableEntity, IAggregateRoot
{
    // ─── Identity ───────────────────────────────────────────────────────────

    /// <summary>The platform user this profile belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The application that resulted in this profile.</summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>URL-friendly unique slug (e.g. "ahmad-adventures").</summary>
    public string Slug { get; private set; } = null!;

    /// <summary>Display name for the creator.</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>Short bio visible on the creator's public page.</summary>
    public string? Bio { get; private set; }

    /// <summary>Profile avatar URL.</summary>
    public string? AvatarUrl { get; private set; }

    // ─── Trust & Status ─────────────────────────────────────────────────────

    /// <summary>Current trust tier determining publishing privileges.</summary>
    public CreatorTrustTier TrustTier { get; private set; }

    /// <summary>Current profile status.</summary>
    public CreatorProfileStatus Status { get; private set; }

    /// <summary>Reason for suspension (if suspended).</summary>
    public string? SuspensionReason { get; private set; }

    /// <summary>Admin who suspended the profile.</summary>
    public Guid? SuspendedByAdminId { get; private set; }

    /// <summary>Date of suspension.</summary>
    public DateTime? SuspendedAt { get; private set; }

    // ─── Stats Counters ─────────────────────────────────────────────────────

    /// <summary>Total published articles authored by this creator.</summary>
    public int ArticleCount { get; private set; }

    /// <summary>Total views across all articles.</summary>
    public long TotalViewCount { get; private set; }

    /// <summary>Total reactions across all articles.</summary>
    public long TotalReactionCount { get; private set; }

    /// <summary>Total comments across all articles.</summary>
    public long TotalCommentCount { get; private set; }

    /// <summary>Number of followers.</summary>
    public int FollowerCount { get; private set; }

    // ─── Tier Promotion / Content Stats ─────────────────────────────────────

    /// <summary>Total reports across all creator content.</summary>
    public int ReportCount { get; private set; }

    /// <summary>Percentage of published content that received reports (0.0 – 100.0).</summary>
    public double ReportRate { get; private set; }

    /// <summary>Whether the background service flagged this profile as eligible for Tier-1.</summary>
    public bool EligibleForTier1 { get; private set; }

    /// <summary>Whether the background service flagged this profile as eligible for Tier-2.</summary>
    public bool EligibleForTier2 { get; private set; }

    // ─── Cross-link ─────────────────────────────────────────────────────────

    /// <summary>If the creator is also a service provider, link to their provider profile.</summary>
    public Guid? LinkedProviderId { get; private set; }

    // ─── Navigation ─────────────────────────────────────────────────────────

    private readonly List<CreatorFollow> _followers = [];
    public IReadOnlyCollection<CreatorFollow> Followers => _followers.AsReadOnly();

    // ─── EF Constructor ─────────────────────────────────────────────────────

    private CreatorProfile() { }

    // ─── Factory ────────────────────────────────────────────────────────────

    /// <summary>Creates a creator profile upon application approval.</summary>
    public static Result<CreatorProfile> Create(
        Guid userId,
        Guid applicationId,
        string slug,
        string displayName,
        string? bio,
        string? avatarUrl)
    {
        var profile = new CreatorProfile
        {
            UserId = userId,
            ApplicationId = applicationId,
            Slug = slug,
            DisplayName = displayName,
            Bio = bio,
            AvatarUrl = avatarUrl,
            TrustTier = CreatorTrustTier.New,
            Status = CreatorProfileStatus.Active
        };

        profile.AddDomainEvent(new CreatorProfileCreatedDomainEvent(profile.Id, userId, applicationId));

        return Result<CreatorProfile>.Success(profile);
    }

    // ─── Profile Management ─────────────────────────────────────────────────

    /// <summary>Update profile details.</summary>
    public Result UpdateProfile(string displayName, string? bio, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure(new Error("CreatorProfile.DisplayNameRequired", "Display name is required."));

        DisplayName = displayName;
        Bio = bio;
        AvatarUrl = avatarUrl;
        MarkUpdated();

        return Result.Success();
    }

    /// <summary>Change the slug (must be validated for uniqueness externally).</summary>
    public Result ChangeSlug(string newSlug)
    {
        if (string.IsNullOrWhiteSpace(newSlug))
            return Result.Failure(new Error("CreatorProfile.SlugRequired", "Slug is required."));

        Slug = newSlug;
        MarkUpdated();

        return Result.Success();
    }

    // ─── Trust Management ───────────────────────────────────────────────────

    // ─── Suspension / Reinstatement ─────────────────────────────────────────

    /// <summary>Admin suspends the creator profile.</summary>
    public Result Suspend(Guid adminId, string reason)
    {
        if (Status == CreatorProfileStatus.Suspended)
            return Result.Failure(CreatorProfileErrors.AlreadySuspended);

        Status = CreatorProfileStatus.Suspended;
        SuspensionReason = reason;
        SuspendedByAdminId = adminId;
        SuspendedAt = DateTime.UtcNow;
        MarkUpdated();
        AddDomainEvent(new CreatorProfileSuspendedDomainEvent(Id, UserId, adminId, reason));

        return Result.Success();
    }

    /// <summary>Admin reinstates a suspended creator profile.</summary>
    public Result Reinstate(Guid adminId)
    {
        if (Status != CreatorProfileStatus.Suspended)
            return Result.Failure(CreatorProfileErrors.NotSuspended);

        Status = CreatorProfileStatus.Active;
        SuspensionReason = null;
        SuspendedByAdminId = null;
        SuspendedAt = null;
        MarkUpdated();
        AddDomainEvent(new CreatorProfileReinstatedDomainEvent(Id, UserId, adminId));

        return Result.Success();
    }

    // ─── Follow / Unfollow (raises domain events from aggregate root) ───────

    /// <summary>
    /// Records that a user followed this creator. Raises <see cref="CreatorFollowedDomainEvent"/>
    /// so that follower count is updated and integration events are staged.
    /// Must be called from the command handler AFTER persisting the <see cref="CreatorFollow"/> entity.
    /// </summary>
    public void RecordFollow(Guid followerUserId)
    {
        AddDomainEvent(new CreatorFollowedDomainEvent(Id, followerUserId));
    }

    /// <summary>
    /// Records that a user unfollowed this creator. Raises <see cref="CreatorUnfollowedDomainEvent"/>
    /// so that follower count is decremented.
    /// Must be called from the command handler AFTER removing the <see cref="CreatorFollow"/> entity.
    /// </summary>
    public void RecordUnfollow(Guid followerUserId)
    {
        AddDomainEvent(new CreatorUnfollowedDomainEvent(Id, followerUserId));
    }

    // ─── Stats Mutation (called by domain event handlers) ───────────────────

    public void IncrementFollowerCount() { FollowerCount++; MarkUpdated(); }
    public void DecrementFollowerCount() { if (FollowerCount > 0) FollowerCount--; MarkUpdated(); }
    public void IncrementArticleCount() { ArticleCount++; MarkUpdated(); }
    public void DecrementArticleCount() { if (ArticleCount > 0) ArticleCount--; MarkUpdated(); }
    public void AddViews(long count) { TotalViewCount += count; MarkUpdated(); }
    public void AddReactions(long count) { TotalReactionCount += count; MarkUpdated(); }
    public void AddComments(long count) { TotalCommentCount += count; MarkUpdated(); }

    // ─── Tier Promotion / Demotion (Wave 8) ────────────────────────────────

    /// <summary>Mark the profile as eligible for Tier-1 promotion (set by background service).</summary>
    public void MarkEligibleForTier1()
    {
        EligibleForTier1 = true;
        MarkUpdated();
        AddDomainEvent(new CreatorEligibleForTierPromotionDomainEvent(Id, UserId, TrustTier, CreatorTrustTier.Trusted));
    }

    /// <summary>Mark the profile as eligible for Tier-2 promotion (set by background service).</summary>
    public void MarkEligibleForTier2()
    {
        EligibleForTier2 = true;
        MarkUpdated();
        AddDomainEvent(new CreatorEligibleForTierPromotionDomainEvent(Id, UserId, TrustTier, CreatorTrustTier.Expert));
    }

    /// <summary>
    /// Admin promotes the creator to a higher trust tier.
    /// Promotion is allowed even without the eligibility flag set (admin override),
    /// but the eligibility flags serve as a guideline from the background service.
    /// </summary>
    public Result Promote(Guid adminId, CreatorTrustTier targetTier, DateTime utcNow)
    {
        if (Status == CreatorProfileStatus.Suspended)
            return Result.Failure(CreatorProfileErrors.Suspended);
        if (targetTier <= TrustTier)
            return Result.Failure(CreatorProfileErrors.AlreadyAtOrAboveTier);

        var previousTier = TrustTier;
        TrustTier = targetTier;

        // Reset the eligibility flags for the tier we just promoted to
        if (targetTier >= CreatorTrustTier.Trusted) EligibleForTier1 = false;
        if (targetTier >= CreatorTrustTier.Expert) EligibleForTier2 = false;

        MarkUpdated();
        AddDomainEvent(new CreatorTierPromotedDomainEvent(Id, UserId, adminId, previousTier, targetTier));
        return Result.Success();
    }

    /// <summary>Demote the creator to a lower trust tier (automated or manual).</summary>
    public Result Demote(CreatorTrustTier targetTier, string reason, DateTime utcNow)
    {
        if (targetTier >= TrustTier)
            return Result.Failure(CreatorProfileErrors.AlreadyAtOrBelowTier);

        var previousTier = TrustTier;
        TrustTier = targetTier;

        // Reset eligibility flags
        EligibleForTier1 = false;
        EligibleForTier2 = false;

        MarkUpdated();
        AddDomainEvent(new CreatorTierDemotedDomainEvent(Id, UserId, previousTier, targetTier, reason));
        return Result.Success();
    }

    // ─── Report Stats (updated by rollup service) ───────────────────────────

    /// <summary>Set the report metrics (called from background rollup service).</summary>
    public void UpdateReportStats(int reportCount, double reportRate)
    {
        ReportCount = reportCount;
        ReportRate = reportRate;
        MarkUpdated();
    }

    // ─── Self-Deactivation ──────────────────────────────────────────────────

    /// <summary>Creator voluntarily deactivates own profile (soft delete). Background job hard-deletes after 60 days.</summary>
    public Result Deactivate(DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(CreatorProfileErrors.NotFound, Outcome.NotFound);

        SoftDelete();
        return Result.Success();
    }

    // ─── Avatar & Cover Image ───────────────────────────────────────────────

    /// <summary>Update avatar URL directly (used for dedicated avatar upload endpoint).</summary>
    public void UpdateAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
        MarkUpdated();
    }

    /// <summary>Clears the avatar URL (used by the dedicated avatar delete endpoint).</summary>
    public void ClearAvatar()
    {
        AvatarUrl = null;
        MarkUpdated();
    }

    // ─── Provider Cross-link ────────────────────────────────────────────────

    /// <summary>Links this creator profile to a service provider profile.</summary>
    public void LinkProvider(Guid providerId)
    {
        LinkedProviderId = providerId;
        MarkUpdated();
    }
}
