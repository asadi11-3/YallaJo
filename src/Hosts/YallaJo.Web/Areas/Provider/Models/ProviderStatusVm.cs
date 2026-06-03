namespace YallaJo.Web.Areas.Provider.Models;

/// <summary>
/// View model for the provider application status page.
/// <see cref="HasApplication"/> is false when the user has not applied yet (404 from API).
/// </summary>
public sealed class ProviderStatusVm
{
    public bool HasApplication { get; init; }

    public Guid ApplicationId { get; init; }
    public string TypeLabel { get; init; } = string.Empty;
    public string BusinessName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public string? SuspensionReason { get; init; }
    public int ReapplicationCount { get; init; }
    public DateTime? CoolingPeriodEndsAt { get; init; }
    public IReadOnlyList<ProviderDocumentVm> Documents { get; init; } = [];

    public bool HasDocuments => Documents.Count > 0;

    /// <summary>Submit is allowed only from Draft or MoreDocsNeeded states.</summary>
    public bool CanSubmit =>
        HasApplication &&
        (string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(Status, "MoreDocsNeeded", StringComparison.OrdinalIgnoreCase));
}

/// <summary>A read-only document row on the status page.</summary>
public sealed class ProviderDocumentVm
{
    public string DocumentTypeLabel { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}
