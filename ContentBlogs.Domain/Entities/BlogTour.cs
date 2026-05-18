namespace ContentBlogs.Domain.Entities;

public sealed class BlogTour
{
    private BlogTour() { } // EF Core

    public Guid BlogId { get; private set; }
    public Guid TourId { get; private set; }
    public int SortOrder { get; private set; } = 0;

    public Blog Blog { get; private set; } = default!;

    /// <summary>
    /// Creates a new <see cref="BlogTour"/> junction row.  Callers are
    /// responsible for supplying non-empty <paramref name="blogId"/> and
    /// <paramref name="tourId"/>; the aggregate / handler perform existence
    /// and authorization checks before invoking this factory.
    /// </summary>
    public static BlogTour Create(Guid blogId, Guid tourId, int sortOrder)
    {
        if (blogId == Guid.Empty)
            throw new ArgumentException("BlogId is required.", nameof(blogId));
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));
        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder), "SortOrder must be non-negative.");

        return new BlogTour
        {
            BlogId = blogId,
            TourId = tourId,
            SortOrder = sortOrder,
        };
    }
}
