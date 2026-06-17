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

    public bool CanUploadDocuments => CanSubmit;

    public ProviderDocumentUploadVm UploadForm { get; init; } = new();

    /// <summary>Maximum reapplications allowed by the backend (Accounts domain: MaxReapplications).</summary>
    public const int MaxReapplications = 3;

    /// <summary>True while a rejected application's cooling period is still active.</summary>
    public bool CoolingPeriodActive =>
        CoolingPeriodEndsAt.HasValue && DateTime.UtcNow < CoolingPeriodEndsAt.Value;

    /// <summary>True when the rejected applicant has used up all reapplication attempts.</summary>
    public bool ReapplyLimitReached => ReapplicationCount >= MaxReapplications;

    /// <summary>Total reapplication attempts allowed (instance accessor for views).</summary>
    public int ReapplyMaxAttempts => MaxReapplications;

    /// <summary>How many reapplication attempts remain (never negative).</summary>
    public int ReapplyAttemptsRemaining => Math.Max(0, MaxReapplications - ReapplicationCount);

    /// <summary>
    /// Reapply is allowed only from the Rejected state, once the cooling period has
    /// passed and the reapplication limit has not been reached (mirrors the backend
    /// <c>ProviderApplication.Reapply()</c> rule).
    /// </summary>
    public bool CanReapply =>
        HasApplication &&
        string.Equals(Status, "Rejected", StringComparison.OrdinalIgnoreCase) &&
        !CoolingPeriodActive &&
        !ReapplyLimitReached;
}

/// <summary>A document row on the status page (supports inline replace).</summary>
public sealed class ProviderDocumentVm
{
    public Guid DocumentId { get; init; }
    public string DocumentTypeLabel { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}
