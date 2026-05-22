using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Domain.ValueObjects;
using YallaJo.SharedKernel.Domain.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities.Creators;

/// <summary>
/// A multi-type content piece authored by a creator.
/// Wave 8 – Multi-type Creator Posts, Tier Promotion, Disclosure Enforcement.
/// </summary>
public sealed class CreatorPost : AuditableEntity, IAggregateRoot
{
    // ────────── Constants ──────────
    public const int MinExcerptLength = 100;
    public const int MaxExcerptLength = 500;
    public const int MaxNiches = 3;
    public const int MaxFreeTags = 10;
    public const int MaxFeaturedGlobal = 12;

    // ────────── Properties ──────────
    public Guid CreatorProfileId { get; private set; }
    public CreatorPostType PostType { get; private set; }
    public string Slug { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Excerpt { get; private set; } = default!;
    public string? Body { get; private set; }
    public Guid LanguageId { get; private set; }
    public CreatorPostStatus Status { get; private set; }

    // ── Moderation ──
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // ── Featuring ──
    public bool IsFeatured { get; private set; }
    public DateTime? FeaturedAt { get; private set; }
    public Guid? FeaturedByAdminId { get; private set; }
    public DateTime? FeaturedUntil { get; private set; }

    // ── Disclosure ──
    public bool IsSponsored { get; private set; }
    private readonly List<DisclosureTarget> _disclosedTargets = [];
    public IReadOnlyList<DisclosureTarget> DisclosedTargets => _disclosedTargets.AsReadOnly();

    // ── Stats ──
    public long ViewCount { get; private set; }
    public long ReactionCount { get; private set; }
    public long CommentCount { get; private set; }
    public int ReportCount { get; private set; }

    // ── Tags / metadata (JSON columns) ──
    public List<Guid> TaggedEntityIds { get; private set; } = [];
    public List<string> TaggedEntityTypes { get; private set; } = [];
    public List<Guid> NicheIds { get; private set; } = [];
    public List<string> FreeTags { get; private set; } = [];
    public List<Guid> PlaceRegionIds { get; private set; } = [];

    // ── Type-specific payload (opaque JSON) ──
    public string? TypeSpecificDataJson { get; private set; }

    // ────────── EF Core constructor ──────────
    private CreatorPost() { }

    // ────────── Factory ──────────

    /// <summary>Creates a new creator post in <see cref="CreatorPostStatus.Draft"/> status.</summary>
    public static Result<CreatorPost> Create(
        Guid creatorProfileId,
        CreatorPostType postType,
        string slug,
        string title,
        string excerpt,
        Guid languageId,
        string? typeSpecificDataJson)
    {
        if (creatorProfileId == Guid.Empty)
            return Result.Failure<CreatorPost>(new Error("CreatorPost.InvalidCreatorProfile", "Creator profile ID is required."));
        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure<CreatorPost>(CreatorPostErrors.SlugAlreadyTaken);
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<CreatorPost>(new Error("CreatorPost.TitleRequired", "Title is required."));
        if (string.IsNullOrWhiteSpace(excerpt))
            return Result.Failure<CreatorPost>(CreatorPostErrors.ExcerptTooShort);
        if (languageId == Guid.Empty)
            return Result.Failure<CreatorPost>(new Error("CreatorPost.InvalidLanguage", "Language ID is required."));
        if (excerpt.Length < MinExcerptLength)
            return Result.Failure<CreatorPost>(CreatorPostErrors.ExcerptTooShort);
        if (excerpt.Length > MaxExcerptLength)
            return Result.Failure<CreatorPost>(CreatorPostErrors.ExcerptTooLong);

        var post = new CreatorPost
        {
            CreatorProfileId = creatorProfileId,
            PostType = postType,
            Slug = slug,
            Title = title,
            Excerpt = excerpt,
            LanguageId = languageId,
            Status = CreatorPostStatus.Draft,
            TypeSpecificDataJson = typeSpecificDataJson
        };

        post.AddDomainEvent(new CreatorPostCreatedDomainEvent(post.Id, creatorProfileId, postType));
        return Result.Success(post);
    }

    // ────────── Content mutations ──────────

    /// <summary>Update the core content fields while the post is still a draft.</summary>
    public Result UpdateContent(string title, string excerpt, string? body, string? typeSpecificDataJson)
    {
        if (Status != CreatorPostStatus.Draft)
            return Result.Failure(CreatorPostErrors.NotDraft);
        if (excerpt.Length < MinExcerptLength)
            return Result.Failure(CreatorPostErrors.ExcerptTooShort);
        if (excerpt.Length > MaxExcerptLength)
            return Result.Failure(CreatorPostErrors.ExcerptTooLong);

        Title = title;
        Excerpt = excerpt;
        Body = body;
        TypeSpecificDataJson = typeSpecificDataJson;
        MarkUpdated();
        return Result.Success();
    }

    /// <summary>Tag the post with entity references, niches, free tags, and regions.</summary>
    public Result Tag(
        List<Guid> taggedEntityIds,
        List<string> taggedEntityTypes,
        List<Guid> nicheIds,
        List<string> freeTags,
        List<Guid> placeRegionIds)
    {
        if (Status != CreatorPostStatus.Draft)
            return Result.Failure(CreatorPostErrors.NotDraft);
        if (nicheIds.Count > MaxNiches)
            return Result.Failure(CreatorPostErrors.TooManyNiches);
        if (freeTags.Count > MaxFreeTags)
            return Result.Failure(CreatorPostErrors.TooManyFreeTags);

        TaggedEntityIds = taggedEntityIds;
        TaggedEntityTypes = taggedEntityTypes;
        NicheIds = nicheIds;
        FreeTags = freeTags;
        PlaceRegionIds = placeRegionIds;
        MarkUpdated();
        return Result.Success();
    }

    /// <summary>Set the sponsorship flag and disclosure targets.</summary>
    public Result MarkDisclosure(bool isSponsored, IReadOnlyList<DisclosureTarget> targets)
    {
        if (Status != CreatorPostStatus.Draft)
            return Result.Failure(CreatorPostErrors.NotDraft);

        IsSponsored = isSponsored;
        _disclosedTargets.Clear();
        _disclosedTargets.AddRange(targets);
        MarkUpdated();
        return Result.Success();
    }

    // ────────── Lifecycle transitions ──────────

    /// <summary>
    /// Tier-0 creator submits post for admin pre-moderation.
    /// Tier-1+ creators should use <see cref="PublishDirect"/> instead.
    /// </summary>
    public Result SubmitForReview(CreatorTrustTier currentTier, DateTime utcNow)
    {
        if (Status != CreatorPostStatus.Draft)
            return Result.Failure(CreatorPostErrors.NotDraft);

        // Tier-1+ creators can publish directly; submitting for review is still allowed
        // but we log the tier for auditing purposes via the domain event.
        Status = CreatorPostStatus.PendingReview;
        SubmittedAt = utcNow;
        MarkUpdated();

        AddDomainEvent(new CreatorPostSubmittedDomainEvent(Id, CreatorProfileId, utcNow));
        return Result.Success();
    }

    /// <summary>Tier-1+ creator publishes directly without pre-moderation.</summary>
    public Result PublishDirect(CreatorTrustTier currentTier, DateTime utcNow)
    {
        if (Status != CreatorPostStatus.Draft)
            return Result.Failure(CreatorPostErrors.NotDraft);
        if (currentTier < CreatorTrustTier.Trusted)
            return Result.Failure(CreatorPostErrors.TierInsufficientForDirectPublish);

        Status = CreatorPostStatus.Published;
        PublishedAt = utcNow;
        MarkUpdated();

        AddDomainEvent(new CreatorPostPublishedDomainEvent(Id, CreatorProfileId, false, utcNow));
        return Result.Success();
    }

    /// <summary>Admin approves a post in PendingReview → Published.</summary>
    public Result ApproveByAdmin(Guid adminId, DateTime utcNow)
    {
        if (Status != CreatorPostStatus.PendingReview)
            return Result.Failure(CreatorPostErrors.NotPendingReview);

        Status = CreatorPostStatus.Published;
        ReviewedAt = utcNow;
        ReviewedByAdminId = adminId;
        PublishedAt = utcNow;
        MarkUpdated();

        AddDomainEvent(new CreatorPostPublishedDomainEvent(Id, CreatorProfileId, true, utcNow));
        return Result.Success();
    }

    /// <summary>Admin rejects a post in PendingReview → Rejected.</summary>
    public Result RejectByAdmin(Guid adminId, string reason, DateTime utcNow)
    {
        if (Status != CreatorPostStatus.PendingReview)
            return Result.Failure(CreatorPostErrors.NotPendingReview);

        Status = CreatorPostStatus.Rejected;
        ReviewedAt = utcNow;
        ReviewedByAdminId = adminId;
        RejectionReason = reason;
        MarkUpdated();

        AddDomainEvent(new CreatorPostRejectedDomainEvent(Id, CreatorProfileId, adminId, reason));
        return Result.Success();
    }

    /// <summary>Admin features a published post. Requires Expert tier.</summary>
    public Result Feature(Guid adminId, DateTime? featuredUntilUtc, DateTime utcNow, CreatorTrustTier creatorTier)
    {
        if (Status != CreatorPostStatus.Published)
            return Result.Failure(CreatorPostErrors.NotPublished);
        if (IsFeatured)
            return Result.Failure(CreatorPostErrors.AlreadyFeatured);
        if (creatorTier < CreatorTrustTier.Expert)
            return Result.Failure(CreatorPostErrors.TierInsufficientForFeaturing);

        IsFeatured = true;
        FeaturedAt = utcNow;
        FeaturedByAdminId = adminId;
        FeaturedUntil = featuredUntilUtc;
        MarkUpdated();

        AddDomainEvent(new CreatorPostFeaturedDomainEvent(Id, CreatorProfileId, adminId, featuredUntilUtc));
        return Result.Success();
    }

    /// <summary>Unfeature a post (manually or on expiry).</summary>
    public Result Unfeature()
    {
        if (!IsFeatured)
            return Result.Failure(CreatorPostErrors.NotFeatured);

        IsFeatured = false;
        FeaturedAt = null;
        FeaturedByAdminId = null;
        FeaturedUntil = null;
        MarkUpdated();

        AddDomainEvent(new CreatorPostUnfeaturedDomainEvent(Id, CreatorProfileId));
        return Result.Success();
    }

    /// <summary>Admin hides a published post (policy violation).</summary>
    public Result Hide(Guid adminId, string reason, DateTime utcNow)
    {
        if (Status != CreatorPostStatus.Published)
            return Result.Failure(CreatorPostErrors.NotPublished);

        // If featured, auto-unfeature and raise event
        if (IsFeatured)
        {
            IsFeatured = false;
            FeaturedAt = null;
            FeaturedByAdminId = null;
            FeaturedUntil = null;
            AddDomainEvent(new CreatorPostUnfeaturedDomainEvent(Id, CreatorProfileId));
        }

        Status = CreatorPostStatus.Hidden;
        MarkUpdated();

        return Result.Success();
    }

    /// <summary>Admin unhides a hidden post → Published.</summary>
    public Result Unhide(DateTime utcNow)
    {
        if (Status != CreatorPostStatus.Hidden)
            return Result.Failure(CreatorPostErrors.NotHidden);

        Status = CreatorPostStatus.Published;
        MarkUpdated();
        return Result.Success();
    }

    /// <summary>Creator permanently removes their own post.</summary>
    public Result Remove()
    {
        if (Status == CreatorPostStatus.Removed)
            return Result.Failure(CreatorPostErrors.AlreadyRemoved);

        // If featured, auto-unfeature and raise event
        if (IsFeatured)
        {
            IsFeatured = false;
            FeaturedAt = null;
            FeaturedByAdminId = null;
            FeaturedUntil = null;
            AddDomainEvent(new CreatorPostUnfeaturedDomainEvent(Id, CreatorProfileId));
        }

        Status = CreatorPostStatus.Removed;
        MarkUpdated();

        AddDomainEvent(new CreatorPostRemovedDomainEvent(Id, CreatorProfileId));
        return Result.Success();
    }

    /// <summary>
    /// Validates disclosure rules: if the creator tags entities they own (via provider cross-link)
    /// and IsSponsored is false, validation fails.
    /// Call from application layer with resolved provider entity IDs.
    /// </summary>
    public Result ValidateDisclosure(IReadOnlySet<Guid> creatorOwnedEntityIds)
    {
        // If no tagged entities, nothing to disclose
        if (TaggedEntityIds.Count == 0)
            return Result.Success();

        // Find entities the creator owns that are also tagged in this post
        var overlappingIds = TaggedEntityIds.Where(id => creatorOwnedEntityIds.Contains(id)).ToList();

        if (overlappingIds.Count == 0)
            return Result.Success();

        // Creator tags their own entities — disclosure is REQUIRED
        if (!IsSponsored)
            return Result.Failure(CreatorPostErrors.DisclosureRequired);

        // Ensure every overlapping entity has a matching disclosure target
        foreach (var entityId in overlappingIds)
        {
            var hasDisclosure = _disclosedTargets.Any(d => d.EntityId == entityId);
            if (!hasDisclosure)
                return Result.Failure(new Error(
                    "CreatorPost.DisclosureMissing",
                    $"Disclosure is missing for tagged entity {entityId}."));
        }

        return Result.Success();
    }

    // ────────── Deletion guard ──────────

    /// <summary>
    /// Validates that the post can be deleted by its creator.
    /// Only Draft or Rejected posts may be deleted.
    /// </summary>
    public Result CanBeDeletedByCreator()
    {
        if (Status is not (CreatorPostStatus.Draft or CreatorPostStatus.Rejected))
            return Result.Failure(new Error("CreatorPost.CannotDelete",
                "Only draft or rejected posts can be deleted by their creator."));

        return Result.Success();
    }

    // ────────── Slug mutation ──────────

    /// <summary>Change the post slug (only allowed in Draft or Rejected status).</summary>
    public Result ChangeSlug(string newSlug)
    {
        if (Status is not (CreatorPostStatus.Draft or CreatorPostStatus.Rejected))
            return Result.Failure(CreatorPostErrors.NotDraft);

        if (string.IsNullOrWhiteSpace(newSlug))
            return Result.Failure(new Error("CreatorPost.SlugRequired", "Slug is required."));

        Slug = newSlug;
        MarkUpdated();
        return Result.Success();
    }
}
