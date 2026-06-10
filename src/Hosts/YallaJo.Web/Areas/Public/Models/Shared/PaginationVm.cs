namespace YallaJo.Web.Areas.Public.Models.Shared;

/// <summary>
/// View model for the shared pagination partial
/// (Areas/Public/Views/Shared/_Pagination.cshtml).
/// The partial preserves the current query string and only swaps the
/// <c>page</c> parameter, so callers don't need any route plumbing.
/// </summary>
public sealed class PaginationVm
{
    public int PageNumber { get; init; } = 1;

    public int TotalPages { get; init; }

    /// <summary>Localized aria-label for the nav element (e.g. "Tours pagination").</summary>
    public string AriaLabel { get; init; } = string.Empty;

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
