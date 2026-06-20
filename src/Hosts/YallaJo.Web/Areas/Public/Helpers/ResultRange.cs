namespace YallaJo.Web.Areas.Public.Helpers;

/// <summary>
/// The "Showing {start}–{end} of {total} results" range for a paginated grid.
/// </summary>
public readonly record struct ResultRange(int Start, int End)
{
    /// <summary>The empty range (no results / nothing visible) — renders as 0–0.</summary>
    public static readonly ResultRange Empty = new(0, 0);
}

/// <summary>
/// Pure, UI-focused computation of the visible item range shown next to a tour grid.
/// Kept tiny and side-effect free so the rule (start ≥ 1 whenever cards are visible,
/// end based on the actual visible count and capped at total, 0–0 only when empty) is
/// unit-testable independently of Razor.
/// </summary>
public static class ResultRangeCalculator
{
    /// <summary>
    /// Computes the inclusive 1-based range for the items currently visible on the page.
    /// </summary>
    /// <param name="totalCount">Total matching items across all pages.</param>
    /// <param name="visibleCount">Number of cards actually rendered on this page.</param>
    /// <param name="pageNumber">1-based current page; values &lt; 1 are treated as page 1.</param>
    /// <param name="pageSize">Items per page; values &lt; 1 fall back to the visible count.</param>
    public static ResultRange Compute(int totalCount, int visibleCount, int pageNumber, int pageSize)
    {
        // No results, or nothing rendered → a clean empty 0–0 (never an invalid range
        // while cards are visible, and never a "1–N of 0").
        if (totalCount <= 0 || visibleCount <= 0)
        {
            return ResultRange.Empty;
        }

        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize < 1 ? visibleCount : pageSize;

        var start = ((page - 1) * size) + 1;
        if (start < 1)
        {
            start = 1;
        }

        var end = start + visibleCount - 1;
        if (end > totalCount)
        {
            end = totalCount;
        }

        // Safety: a stale/over-large page number must never yield start > end.
        if (start > end)
        {
            start = end;
        }

        return new ResultRange(start, end);
    }
}
