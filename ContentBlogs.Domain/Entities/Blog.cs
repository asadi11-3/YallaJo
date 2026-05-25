using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Events;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities;

public sealed class Blog : AuditableEntity, IAggregateRoot
{
    private readonly List<BlogTranslation> _blogTranslations = [];
    private readonly List<BlogComment> _blogComments = [];
    private readonly List<BlogTour> _blogTours = [];

    private Blog()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public Guid AuthorId { get; private set; }
    public BlogStatus Status { get; private set; } = BlogStatus.Draft;
    public int ViewCount { get; private set; }
    public int? ReadTimeMinutes { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid? PlaceId { get; private set; }

    /// <summary>
    /// Non-null when the article was authored by a content creator.
    /// References <c>CreatorProfile.Id</c> in the Creators sub-domain.
    /// </summary>
    public Guid? AuthoredByCreatorId { get; private set; }

    /// <summary>
    /// The language in which this article was originally written.
    /// </summary>
    public Guid LanguageId { get; private set; }

    /// <summary>
    /// Whether this article is sponsored content (Disclosure enforcement).
    /// </summary>
    public bool IsSponsored { get; private set; }

    /// <summary>
    /// Transparency disclosures for entities referenced in this article.
    /// </summary>
    private readonly List<ValueObjects.DisclosureTarget> _disclosedTargets = [];
    public IReadOnlyList<ValueObjects.DisclosureTarget> DisclosedTargets => _disclosedTargets.AsReadOnly();

    // ── Moderation fields ──────────────────────────────────────────────

    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public string? RejectionReason { get; private set; }

    // ── Time-bound featuring (replaces IsFeatured bool) ────────────────

    public DateTime? FeaturedAt { get; private set; }
    public Guid? FeaturedByAdminId { get; private set; }
    public DateTime? FeaturedUntil { get; private set; }

    /// <summary>
    /// Computed: featured when FeaturedAt is set and not yet expired.
    /// </summary>
    public bool IsFeatured => FeaturedAt.HasValue && (FeaturedUntil == null || FeaturedUntil > DateTime.UtcNow);

    // ── Denormalized counters ─────────────────────────────────────────

    public int ReactionCount { get; private set; }
    public int CommentCount { get; private set; }
    public int ReportCount { get; private set; }

    public IReadOnlyCollection<BlogTranslation> BlogTranslations => _blogTranslations.AsReadOnly();
    public IReadOnlyCollection<BlogComment> BlogComments => _blogComments.AsReadOnly();
    public IReadOnlyCollection<BlogTour> BlogTours => _blogTours.AsReadOnly();

    public static Blog Create(
        string title,
        string slug,
        string content,
        Guid authorId,
        Guid sourceLanguageId,
        DateTime utcNow,
        string? summary = null,
        string? metaTitle = null,
        string? metaDescription = null,
        Guid? placeId = null,
        int? readTimeMinutes = null)
    {
        ValidateTitle(title);
        ValidateSlug(slug);
        ValidateContent(content);

        if (authorId == Guid.Empty)
            throw new ArgumentException("AuthorId is required.", nameof(authorId));
        if (sourceLanguageId == Guid.Empty)
            throw new ArgumentException("SourceLanguageId is required.", nameof(sourceLanguageId));
        if (placeId.HasValue && placeId.Value == Guid.Empty)
            throw new ArgumentException("PlaceId cannot be Guid.Empty.", nameof(placeId));

        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var blog = new Blog
        {
            Title = title.Trim(),
            Slug = normalizedSlug,
            Content = content.Trim(),
            Summary = summary?.Trim(),
            AuthorId = authorId,
            Status = BlogStatus.Draft,
            MetaTitle = metaTitle?.Trim(),
            MetaDescription = metaDescription?.Trim(),
            PlaceId = placeId,
            ReadTimeMinutes = readTimeMinutes,
            ViewCount = 0,
            LanguageId = sourceLanguageId,
        };

        blog.CreatedAt = utcNow;
        blog.ReactionCount = 0;
        blog.CommentCount = 0;
        blog.ReportCount = 0;

        blog.AddDomainEvent(new BlogCreatedDomainEvent(
            BlogId: blog.Id,
            Slug: blog.Slug,
            Title: blog.Title,
            AuthorId: blog.AuthorId,
            SourceLanguageId: sourceLanguageId,
            PlaceId: blog.PlaceId,
            CreatedAtUtc: utcNow));

        return blog;
    }

    /// <summary>
    /// Factory for creator-authored articles. Sets <see cref="AuthoredByCreatorId"/> and
    /// initial status to <see cref="BlogStatus.Draft"/>. The creator must later call
    /// <see cref="SubmitForCreatorReview"/> to put it into the admin queue.
    /// </summary>
    public static Blog CreateByCreator(
        string title,
        string slug,
        string content,
        Guid authorId,
        Guid creatorProfileId,
        Guid sourceLanguageId,
        DateTime utcNow,
        string? summary = null,
        string? metaTitle = null,
        string? metaDescription = null,
        Guid? placeId = null,
        int? readTimeMinutes = null)
    {
        if (creatorProfileId == Guid.Empty)
            throw new ArgumentException("CreatorProfileId is required.", nameof(creatorProfileId));

        var blog = Create(
            title, slug, content, authorId, sourceLanguageId, utcNow,
            summary, metaTitle, metaDescription, placeId, readTimeMinutes);

        blog.AuthoredByCreatorId = creatorProfileId;
        blog.LanguageId = sourceLanguageId;

        return blog;
    }

    /// <summary>
    /// Creator submits the draft (or rejected) article for admin review.
    /// Transitions: Draft|Rejected → PendingReview.
    /// </summary>
    public Result SubmitForReview(DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (Status is not BlogStatus.Draft and not BlogStatus.Rejected)
            return Result.Failure(BlogErrors.InvalidTransitionToReview);

        if (AuthoredByCreatorId is null)
            return Result.Failure(BlogErrors.NotCreatorAuthored);

        Status = BlogStatus.PendingReview;
        SubmittedAt = utcNow;
        MarkUpdated();

        AddDomainEvent(new BlogSubmittedForReviewDomainEvent(
            BlogId: Id,
            Slug: Slug,
            CreatorProfileId: AuthoredByCreatorId.Value,
            SubmittedAtUtc: utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Admin approves a pending blog. Publishes it.
    /// Transitions: PendingReview → Published.
    /// </summary>
    public Result Approve(Guid adminId, DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (Status != BlogStatus.PendingReview)
            return Result.Failure(BlogErrors.InvalidTransitionToApproved);

        Status = BlogStatus.Published;
        PublishedAt ??= utcNow;
        ReviewedAt = utcNow;
        ReviewedByAdminId = adminId;
        RejectionReason = null;
        MarkUpdated();

        AddDomainEvent(new BlogApprovedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            ApprovedByAdminId: adminId,
            AuthoredByCreatorId: AuthoredByCreatorId,
            ApprovedAtUtc: utcNow));

        AddDomainEvent(new BlogPublishedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            Title: Title,
            AuthorId: AuthorId,
            PlaceId: PlaceId,
            PublishedAtUtc: PublishedAt.Value));

        return Result.Success();
    }

    /// <summary>
    /// Admin rejects a pending blog with a reason.
    /// Transitions: PendingReview → Rejected.
    /// </summary>
    public Result Reject(Guid adminId, string reason, DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (Status != BlogStatus.PendingReview)
            return Result.Failure(BlogErrors.InvalidTransitionToRejected);

        Status = BlogStatus.Rejected;
        ReviewedAt = utcNow;
        ReviewedByAdminId = adminId;
        RejectionReason = reason;
        MarkUpdated();

        AddDomainEvent(new BlogRejectedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            RejectedByAdminId: adminId,
            Reason: reason,
            AuthoredByCreatorId: AuthoredByCreatorId,
            RejectedAtUtc: utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Admin removes a blog for moderation reasons.
    /// Transitions: any non-deleted → Removed.
    /// </summary>
    public Result Remove(Guid adminId, string reason, DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (Status == BlogStatus.Removed)
            return Result.Failure(BlogErrors.InvalidTransitionToRemoved);

        Status = BlogStatus.Removed;
        MarkUpdated();

        AddDomainEvent(new BlogRemovedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            RemovedByAdminId: adminId,
            Reason: reason,
            RemovedAtUtc: utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Admin features a published blog with an optional expiry.
    /// </summary>
    public Result Feature(Guid adminId, DateTime utcNow, DateTime? until = null)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (Status != BlogStatus.Published)
            return Result.Failure(BlogErrors.InvalidTransitionToFeatured);

        if (IsFeatured)
            return Result.Failure(BlogErrors.AlreadyFeatured);

        FeaturedAt = utcNow;
        FeaturedByAdminId = adminId;
        FeaturedUntil = until;
        MarkUpdated();

        AddDomainEvent(new BlogFeaturedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            Title: Title,
            AuthorId: AuthorId,
            PlaceId: PlaceId,
            FeaturedAtUtc: utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Admin removes the featured status from a blog.
    /// </summary>
    public Result Unfeature(DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (!IsFeatured)
            return Result.Failure(BlogErrors.NotFeatured);

        FeaturedAt = null;
        FeaturedByAdminId = null;
        FeaturedUntil = null;
        MarkUpdated();

        AddDomainEvent(new BlogUnfeaturedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            PlaceId: PlaceId,
            UnfeaturedAtUtc: utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Admin hides a published article. Hidden articles are not visible to the public but
    /// the creator can still edit them.
    /// Transitions: Published → Hidden.
    /// </summary>
    public Result Hide(Guid hiddenByAdminId, string reason, DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (Status != BlogStatus.Published)
            return Result.Failure(BlogErrors.InvalidTransitionToHidden);

        Status = BlogStatus.Hidden;
        MarkUpdated();

        AddDomainEvent(new BlogHiddenDomainEvent(
            BlogId: Id,
            HiddenByAdminId: hiddenByAdminId,
            Reason: reason,
            HiddenAtUtc: utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Admin unhides a hidden article, returning it to Published.
    /// Transitions: Hidden → Published.
    /// </summary>
    public Result Unhide(DateTime utcNow)
    {
        if (IsDeleted)
            return Result.Failure(BlogErrors.AlreadyDeleted);

        if (Status != BlogStatus.Hidden)
            return Result.Failure(BlogErrors.InvalidTransitionToPublished);

        Status = BlogStatus.Published;
        MarkUpdated();

        AddDomainEvent(new BlogUnhiddenDomainEvent(
            BlogId: Id,
            UnhiddenAtUtc: utcNow));

        return Result.Success();
    }

    public void Update(
        string title,
        string slug,
        string content,
        string? summary,
        string? metaTitle,
        string? metaDescription,
        Guid? placeId,
        int? readTimeMinutes,
        DateTime utcNow)
    {
        EnsureNotDeleted();
        EnsureMutable();

        ValidateTitle(title);
        ValidateSlug(slug);
        ValidateContent(content);

        if (placeId.HasValue && placeId.Value == Guid.Empty)
            throw new ArgumentException("PlaceId cannot be Guid.Empty.", nameof(placeId));

        var oldSlug = Slug;
        var newSlug = slug.Trim().ToLowerInvariant();
        var newTitle = title.Trim();
        var newContent = content.Trim();
        var newSummary = summary?.Trim();
        var newMetaTitle = metaTitle?.Trim();
        var newMetaDescription = metaDescription?.Trim();

        var fieldsChanged = new List<string>();

        if (!string.Equals(Title, newTitle, StringComparison.Ordinal))
            fieldsChanged.Add(nameof(Title));
        if (!string.Equals(Slug, newSlug, StringComparison.Ordinal))
            fieldsChanged.Add(nameof(Slug));
        if (!string.Equals(Content, newContent, StringComparison.Ordinal))
            fieldsChanged.Add(nameof(Content));
        if (!string.Equals(Summary, newSummary, StringComparison.Ordinal))
            fieldsChanged.Add(nameof(Summary));
        if (!string.Equals(MetaTitle, newMetaTitle, StringComparison.Ordinal))
            fieldsChanged.Add(nameof(MetaTitle));
        if (!string.Equals(MetaDescription, newMetaDescription, StringComparison.Ordinal))
            fieldsChanged.Add(nameof(MetaDescription));
        if (PlaceId != placeId)
            fieldsChanged.Add(nameof(PlaceId));
        if (ReadTimeMinutes != readTimeMinutes)
            fieldsChanged.Add(nameof(ReadTimeMinutes));

        Title = newTitle;
        Slug = newSlug;
        Content = newContent;
        Summary = newSummary;
        MetaTitle = newMetaTitle;
        MetaDescription = newMetaDescription;
        PlaceId = placeId;
        ReadTimeMinutes = readTimeMinutes;
        UpdatedAt = utcNow;

        if (fieldsChanged.Count > 0)
        {
            AddDomainEvent(new BlogUpdatedDomainEvent(
                BlogId: Id,
                OldSlug: oldSlug,
                NewSlug: newSlug,
                FieldsChanged: fieldsChanged.AsReadOnly(),
                UpdatedAtUtc: utcNow));
        }
    }

    /// <summary>
    /// Admin/Tier-1+ direct publish. Skips PendingReview.
    /// Valid from: Draft, Rejected.
    /// </summary>
    public void Publish(DateTime utcNow)
    {
        EnsureNotDeleted();

        if (Status is not BlogStatus.Draft and not BlogStatus.Rejected)
        {
            throw new InvalidOperationException(
                $"Blog.InvalidTransition: cannot publish a blog with status {Status}. Required: Draft or Rejected.");
        }

        Status = BlogStatus.Published;
        PublishedAt ??= utcNow;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogPublishedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            Title: Title,
            AuthorId: AuthorId,
            PlaceId: PlaceId,
            PublishedAtUtc: PublishedAt.Value));
    }

    public void Unpublish(DateTime utcNow)
    {
        EnsureNotDeleted();

        if (Status != BlogStatus.Published)
        {
            throw new InvalidOperationException(
              $"Blog.InvalidTransition: cannot unpublish a blog with status {Status}. Required: Published.");
        }

        Status = BlogStatus.Draft;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogUnpublishedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            UnpublishedAtUtc: utcNow));
    }

    public void Archive(DateTime utcNow)
    {
        EnsureNotDeleted();

        if (Status is BlogStatus.Archived or BlogStatus.Removed)
        {
            throw new InvalidOperationException(
               $"Blog.InvalidTransition: cannot archive a blog with status {Status}.");
        }

        Status = BlogStatus.Archived;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogArchivedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            ArchivedAtUtc: utcNow));
    }

    public void Delete(DateTime utcNow)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = utcNow;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogDeletedDomainEvent(
            BlogId: Id,
            Slug: Slug,
            DeletedAtUtc: utcNow));
    }

    public void AddTranslation(BlogTranslation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);
        EnsureNotDeleted();

        if (translation.BlogId != Id)
        {
            throw new InvalidOperationException(
                $"Blog.TranslationMismatch: translation BlogId {translation.BlogId} does not match aggregate {Id}.");
        }

        if (_blogTranslations.Any(t => t.LanguageId == translation.LanguageId))
        {
            throw new InvalidOperationException(
                $"Blog.TranslationDuplicate: a translation for language {translation.LanguageId} already exists.");
        }

        _blogTranslations.Add(translation);
    }

    public IReadOnlyList<Guid> LinkTours(
        IEnumerable<(Guid TourId, int? SortOrder)> tours,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(tours);
        EnsureNotDeleted();
        EnsureMutable();

        var existing = _blogTours.Select(bt => bt.TourId).ToHashSet();
        var addedTourIds = new List<Guid>();
        var nextSortOrder = _blogTours.Count == 0 ? 0 : _blogTours.Max(bt => bt.SortOrder) + 1;

        foreach (var (tourId, sortOrder) in tours)
        {
            if (tourId == Guid.Empty)
                throw new ArgumentException("TourId cannot be Guid.Empty.", nameof(tours));

            if (existing.Contains(tourId))
                continue;

            var link = BlogTour.Create(
                blogId: Id,
                tourId: tourId,
                sortOrder: sortOrder ?? nextSortOrder++);

            _blogTours.Add(link);
            existing.Add(tourId);
            addedTourIds.Add(tourId);

            AddDomainEvent(new BlogTourLinkedDomainEvent(
                BlogId: Id,
                TourId: tourId,
                LinkedAtUtc: utcNow));
        }

        if (addedTourIds.Count > 0)
            UpdatedAt = utcNow;

        return addedTourIds;
    }

    public void RegisterTourUnlinked(Guid tourId, DateTime utcNow)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId cannot be Guid.Empty.", nameof(tourId));

        EnsureNotDeleted();
        EnsureMutable();

        var local = _blogTours.FirstOrDefault(bt => bt.TourId == tourId);
        if (local is not null)
            _blogTours.Remove(local);

        UpdatedAt = utcNow;

        AddDomainEvent(new BlogTourUnlinkedDomainEvent(
            BlogId: Id,
            TourId: tourId,
            UnlinkedAtUtc: utcNow));
    }

    /// <summary>
    /// Counter increment: called by event handler on comment creation.
    /// </summary>
    public void IncrementCommentCount() => CommentCount++;

    /// <summary>
    /// Counter decrement: called by event handler on comment deletion.
    /// </summary>
    public void DecrementCommentCount() => CommentCount = Math.Max(0, CommentCount - 1);

    /// <summary>
    /// Counter increment: called by event handler on reaction.
    /// </summary>
    public void IncrementReactionCount() => ReactionCount++;

    /// <summary>
    /// Counter decrement: called by event handler on reaction removal.
    /// </summary>
    public void DecrementReactionCount() => ReactionCount = Math.Max(0, ReactionCount - 1);

    /// <summary>
    /// Counter increment: called by event handler on report.
    /// </summary>
    public void IncrementReportCount() => ReportCount++;

    public void Restore(DateTime utcNow)
    {
        if (!IsDeleted)
        {
            throw new InvalidOperationException(
                "Blog.InvalidTransition: cannot restore a blog that is not deleted.");
        }

        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogRestoredDomainEvent(
            BlogId:        Id,
            Slug:          Slug,
            RestoredAtUtc: utcNow));
    }

    public void UnlinkFromPlace(DateTime utcNow)
    {
        if (IsDeleted)
            return;

        // Idempotent — no event, no UpdatedAt bump when nothing to clear.
        if (PlaceId is null)
            return;

        var oldSlug = Slug;
        PlaceId = null;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogUpdatedDomainEvent(
            BlogId:        Id,
            OldSlug:       oldSlug,
            NewSlug:       Slug,
            FieldsChanged: new List<string> { nameof(PlaceId) }.AsReadOnly(),
            UpdatedAtUtc:  utcNow));
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
              "Blog.Deleted: operation not permitted on a soft-deleted blog.");
        }
    }

    private void EnsureMutable()
    {
        if (Status is BlogStatus.Archived or BlogStatus.Removed)
        {
            throw new InvalidOperationException(
               $"Blog.ReadOnly: blogs with status {Status} cannot be modified.");
        }
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Blog title is required.", nameof(title));
        if (title.Trim().Length > 500)
            throw new ArgumentException("Blog title cannot exceed 500 characters.", nameof(title));
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Blog slug is required.", nameof(slug));
        if (slug.Trim().Length > 300)
            throw new ArgumentException("Blog slug cannot exceed 300 characters.", nameof(slug));
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Blog content is required.", nameof(content));
    }
}
