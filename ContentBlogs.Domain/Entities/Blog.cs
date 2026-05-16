using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Events;
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
    public bool IsFeatured { get; private set; }
    public int ViewCount { get; private set; }
    public int? ReadTimeMinutes { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid? PlaceId { get; private set; }

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
            IsFeatured = false,
        };

        blog.CreatedAt = utcNow;

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

    public void Publish(DateTime utcNow)
    {
        EnsureNotDeleted();

        if (Status != BlogStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Blog.InvalidTransition: cannot publish a blog with status {Status}. Required: Draft.");
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

        if (Status != BlogStatus.Published)
        {
            throw new InvalidOperationException(
               $"Blog.InvalidTransition: cannot archive a blog with status {Status}. Required: Published.");
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

    public void IncrementViewCount(DateTime utcNow)
    {
        EnsureNotDeleted();
        ViewCount++;
        UpdatedAt = utcNow;
    }

    public void MarkAsFeatured(DateTime utcNow)
    {
        EnsureNotDeleted();

        if (Status != BlogStatus.Published)
        {
            throw new InvalidOperationException(
                $"Blog.InvalidTransition: cannot feature a blog with status {Status}. Required: Published.");
        }

        if (IsFeatured)
        {
            throw new InvalidOperationException(
                "Blog.InvalidTransition: blog is already featured.");
        }

        IsFeatured = true;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogFeaturedDomainEvent(
            BlogId:        Id,
            Slug:          Slug,
            Title:         Title,
            AuthorId:      AuthorId,
            PlaceId:       PlaceId,
            FeaturedAtUtc: utcNow));
    }

    public void MarkAsUnfeatured(DateTime utcNow)
    {
        EnsureNotDeleted();

        if (!IsFeatured)
        {
            throw new InvalidOperationException(
                "Blog.InvalidTransition: blog is not currently featured.");
        }

        IsFeatured = false;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogUnfeaturedDomainEvent(
            BlogId:          Id,
            Slug:            Slug,
            PlaceId:         PlaceId,
            UnfeaturedAtUtc: utcNow));
    }

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
        if (Status == BlogStatus.Archived)
        {
            throw new InvalidOperationException(
               "Blog.ArchivedReadOnly: archived blogs cannot be modified. " +
               "Allowed statuses: Draft, Published.");
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
