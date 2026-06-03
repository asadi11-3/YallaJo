namespace YallaJo.Web.Areas.Provider.Models.Tours;

public sealed class ProviderToursIndexVm
{
    public List<ProviderTourRowVm> Items { get; init; } = [];

    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int Total { get; init; }
    public int TotalPages { get; init; }

    public bool HasItems => Items.Count > 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public IReadOnlyList<ProviderTourStatusOption> StatusOptions { get; init; } = [];
}

public sealed class ProviderTourRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "text-bg-secondary";
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public bool CanEdit { get; init; }
    public bool CanSubmit { get; init; }
    public bool CanArchive { get; init; }
}

public sealed class ProviderTourStatusOption
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}
