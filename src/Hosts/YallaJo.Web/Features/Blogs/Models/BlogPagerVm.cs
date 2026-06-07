namespace YallaJo.Web.Features.Blogs.Models;

public sealed class BlogPagerVm
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public string? Search { get; init; }
    public bool? IsFeatured { get; init; }
}
