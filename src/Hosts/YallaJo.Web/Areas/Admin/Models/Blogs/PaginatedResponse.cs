namespace YallaJo.Web.Areas.Admin.Models.Blogs;

/// <summary>Mirrors the API's <c>PaginatedResult&lt;T&gt;</c> JSON shape.</summary>
public sealed class PaginatedResponse<T>
{
    public List<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
