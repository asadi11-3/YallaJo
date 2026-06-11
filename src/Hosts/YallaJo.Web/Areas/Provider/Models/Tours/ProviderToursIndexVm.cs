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

    /// <summary>Per-status listing counts ([Backend] B1). Null when the counts call failed — UI degrades to tabs without counts (ERR3).</summary>
    public TourStatusCountsVm? StatusCounts { get; init; }
}

/// <summary>Per-status tour counts for the listings filter tabs ([Backend] B1).</summary>
public sealed class TourStatusCountsVm
{
    public int Draft { get; init; }
    public int Pending { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Suspended { get; init; }
    public int Archived { get; init; }
    public int Total { get; init; }

    /// <summary>Count for a status filter value, or null for unknown values.</summary>
    public int? For(string? status) => status switch
    {
        null or "" => Total,
        "Draft" => Draft,
        "Pending" => Pending,
        "Approved" => Approved,
        "Rejected" => Rejected,
        "Suspended" => Suspended,
        "Archived" => Archived,
        _ => null,
    };
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
