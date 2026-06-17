namespace YallaJo.Web.Areas.Admin.Models.Providers;

/// <summary>
/// View model for the admin provider-application details page
/// (<c>/admin/providers/{id}</c>). Carries the full application detail plus the
/// status-aware action availability flags and the request-docs option list.
/// </summary>
public sealed class ProviderDetailsVm
{
    public Guid ApplicationId { get; init; }
    public Guid UserId { get; init; }

    public string TypeLabel { get; init; } = string.Empty;
    public string BusinessName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "text-bg-secondary";

    public DateTime? SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public string? RejectionReason { get; init; }
    public string? SuspensionReason { get; init; }
    public int ReapplicationCount { get; init; }
    public DateTime? CoolingPeriodEndsAt { get; init; }

    public IReadOnlyList<string> MissingDocumentTypes { get; init; } = [];
    public IReadOnlyList<ProviderDocumentVm> Documents { get; init; } = [];

    public bool HasDocuments => Documents.Count > 0;
    public bool HasMissingDocuments => MissingDocumentTypes.Count > 0;

    // Status-aware action availability (mirrors the backend state machine and the queue).
    public bool CanApprove { get; init; }
    public bool CanReject { get; init; }
    public bool CanRequestDocs { get; init; }
    public bool CanSuspend { get; init; }
    public bool CanReinstate { get; init; }

    public bool HasAnyAction => CanApprove || CanReject || CanRequestDocs || CanSuspend || CanReinstate;

    /// <summary>Document-type options for the request-documents form.</summary>
    public IReadOnlyList<ProviderFilterOptionVm> DocumentTypeOptions { get; init; } = [];
}

/// <summary>One uploaded document row on the details page.</summary>
public sealed class ProviderDocumentVm
{
    public Guid DocumentId { get; init; }
    public string DocumentTypeLabel { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Authorized MVC download link (<c>/admin/providers/documents/{id}/download</c>) that proxies the
    /// API stream endpoint. Replaces the legacy on-disk FileUrl (404'd by the API static-file middleware).
    /// </summary>
    public string DownloadUrl { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }

    /// <summary>Human-friendly file size, e.g. "2.3 MB".</summary>
    public string FileSizeLabel
    {
        get
        {
            if (FileSizeBytes <= 0) return "—";
            string[] units = ["B", "KB", "MB", "GB"];
            double size = FileSizeBytes;
            var unit = 0;
            while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
            return $"{size:0.#} {units[unit]}";
        }
    }
}
