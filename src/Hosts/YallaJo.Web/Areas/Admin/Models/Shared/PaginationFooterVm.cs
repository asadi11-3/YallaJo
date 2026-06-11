namespace YallaJo.Web.Areas.Admin.Models.Shared;

/// <summary>
/// View model for the shared <c>Partials/_PaginationFooter</c> partial (D1, D2, RTL3).
/// Driven solely by <see cref="HasPrevious"/> / <see cref="HasNext"/> — never by a
/// computed total. The caller builds the prev/next URLs (preserving its own filter
/// route values) via <c>Url.Action(...)</c> and passes pre-localized labels.
/// </summary>
public sealed class PaginationFooterVm
{
    public required bool HasPrevious { get; init; }

    public required bool HasNext { get; init; }

    /// <summary>Current 1-based page number, rendered LTR with <c>.font-data</c> (RTL3).</summary>
    public required int Page { get; init; }

    /// <summary>URL of the previous page; ignored when <see cref="HasPrevious"/> is false.</summary>
    public string? PreviousUrl { get; init; }

    /// <summary>URL of the next page; ignored when <see cref="HasNext"/> is false.</summary>
    public string? NextUrl { get; init; }

    public required string PreviousText { get; init; }

    public required string NextText { get; init; }

    /// <summary>Pre-localized "Page" label shown beside the current page number.</summary>
    public required string PageLabel { get; init; }
}
