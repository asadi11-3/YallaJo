namespace YallaJo.Web.Areas.Guide.Models.Shared;

/// <summary>
/// Model for the shared <c>_GuidePagination</c> partial. Page math (TotalPages,
/// HasPrevious/HasNext) comes from the feature VM — never compute it in the view (D1-paging).
/// <paramref name="AriaLabel"/> must already be localized.
/// </summary>
public sealed record PaginationVm(
    int Page,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    string Controller,
    string AriaLabel,
    string Action = "Index");
