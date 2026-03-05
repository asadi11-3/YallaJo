namespace ContentBlogs.Domain.Entities;

public sealed class BlogTour
{
    private BlogTour() { } // EF Core

    public Guid BlogId { get; private set; }
    public Guid TourId { get; private set; }
    public int SortOrder { get; private set; } = 0;

    public Blog Blog { get; private set; } = default!;
}
