namespace YallaJo.Web.Areas.Public.Models.Help;

public sealed class FaqItemResponse
{
    public Guid Id { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class FaqPageResponse
{
    public IReadOnlyList<FaqItemResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
