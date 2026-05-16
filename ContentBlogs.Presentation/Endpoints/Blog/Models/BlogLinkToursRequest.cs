namespace ContentBlogs.Presentation.Endpoints.Blog.Models;

public sealed record BlogLinkToursRequest(
    byte[] RowVersion,
    IReadOnlyCollection<BlogLinkTourItem> Tours);

public sealed record BlogLinkTourItem(Guid TourId, int? SortOrder = null);
