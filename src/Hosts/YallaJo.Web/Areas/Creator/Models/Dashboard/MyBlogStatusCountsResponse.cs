namespace YallaJo.Web.Areas.Creator.Models.Dashboard;

/// <summary>
/// Response shape for GET /api/v1/blogs/my-blogs/status-counts. Deserialized by name
/// (PropertyNameCaseInsensitive), so positional order is irrelevant.
/// </summary>
public sealed record MyBlogStatusCountsResponse(
    int Draft,
    int PendingReview,
    int Published,
    int Archived,
    int Deleted);
