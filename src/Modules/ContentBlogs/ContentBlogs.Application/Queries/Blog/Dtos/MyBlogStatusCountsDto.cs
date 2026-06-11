namespace ContentBlogs.Application.Queries.Blog.Dtos;

/// <summary>
/// Aggregate counts of the caller's own blogs grouped by lifecycle bucket.
/// <para>
/// The non-deleted buckets (<see cref="Draft"/>, <see cref="PendingReview"/>,
/// <see cref="Published"/>, <see cref="Archived"/>) are counted from active rows
/// grouped by <c>BlogStatus</c>; <see cref="Deleted"/> counts soft-deleted rows.
/// </para>
/// </summary>
public sealed record MyBlogStatusCountsDto(
    int Draft,
    int PendingReview,
    int Published,
    int Archived,
    int Deleted);
