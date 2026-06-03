namespace YallaJo.Web.Areas.Admin.Models.Providers;

/// <summary>View model for the admin provider-application queue (<c>/admin/providers</c>).</summary>
public sealed class ProviderQueueVm
{
    public List<ProviderQueueRowVm> Items { get; init; } = [];

    // Filters (echoed back into the filter form and pagination links).
    public string? Status { get; init; }
    public string? Type { get; init; }

    // Pagination.
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public bool HasItems => Items.Count > 0;

    // Dropdown options.
    public IReadOnlyList<ProviderFilterOptionVm> StatusOptions { get; init; } = [];
    public IReadOnlyList<ProviderFilterOptionVm> TypeOptions { get; init; } = [];
    public IReadOnlyList<ProviderFilterOptionVm> DocumentTypeOptions { get; init; } = [];
}

/// <summary>One row in the queue.</summary>
public sealed class ProviderQueueRowVm
{
    public Guid ApplicationId { get; init; }
    public string TypeLabel { get; init; } = string.Empty;
    public string BusinessName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "text-bg-secondary";
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public int DocumentCount { get; init; }
    public int ReapplicationCount { get; init; }

    // Which row-level actions are valid for this application's current status.
    public bool CanApprove { get; init; }
    public bool CanReject { get; init; }
    public bool CanRequestDocs { get; init; }
    public bool CanSuspend { get; init; }
    public bool CanReinstate { get; init; }

    public bool HasAnyAction => CanApprove || CanReject || CanRequestDocs || CanSuspend || CanReinstate;
}

/// <summary>An option for the status / type / document-type selects.</summary>
public sealed class ProviderFilterOptionVm
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}
