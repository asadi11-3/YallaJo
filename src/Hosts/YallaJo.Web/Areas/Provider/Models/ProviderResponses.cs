namespace YallaJo.Web.Areas.Provider.Models;

/// <summary>
/// Mirrors <c>GetMyApplicationStatusResult</c> from <c>GET /api/v1/provider/status</c>.
/// Enum-typed API fields are modeled as strings (the API serializes enums as strings).
/// </summary>
public sealed class ProviderStatusResponse
{
    public Guid ApplicationId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string BusinessName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public string? SuspensionReason { get; init; }
    public int ReapplicationCount { get; init; }
    public DateTime? CoolingPeriodEndsAt { get; init; }
    public List<ProviderDocumentResponse> Documents { get; init; } = [];
}

/// <summary>Mirrors <c>DocumentSummary</c>.</summary>
public sealed class ProviderDocumentResponse
{
    public Guid DocumentId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>Mirrors <c>RegisterProviderResult</c> from <c>POST /api/v1/provider/register</c>.</summary>
public sealed class RegisterProviderResponse
{
    public Guid ApplicationId { get; init; }
}
