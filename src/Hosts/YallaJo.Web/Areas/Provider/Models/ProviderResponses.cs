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

/// <summary>Mirrors <c>DocumentSummary</c>. Patch 2G: FileUrl removed (V2 breaking change).</summary>
public sealed class ProviderDocumentResponse
{
    public Guid DocumentId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>Mirrors <c>RegisterProviderResult</c> from <c>POST /api/v1/provider/register</c>.</summary>
public sealed class RegisterProviderResponse
{
    public Guid ApplicationId { get; init; }
}

/// <summary>
/// Mirrors <c>AddProviderDocumentResult</c> from <c>POST /api/v1/provider/documents/upload</c>.
/// Patch 2G: FileUrl removed (V2 breaking change).
/// </summary>
public sealed class AddProviderDocumentResponse
{
    public Guid DocumentId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
}

/// <summary>
/// Mirrors <c>ReplaceProviderDocumentResult</c> from
/// <c>POST /api/v1/provider/documents/{id}/replace-upload</c>.
/// Patch 2G: NewFileUrl removed (V2 breaking change).
/// </summary>
public sealed class ReplaceProviderDocumentResponse
{
    public Guid DocumentId { get; init; }
}

/// <summary>
/// Mirrors <c>ProviderSettingsResult</c> from <c>GET /api/v1/provider/settings</c>.
/// <c>ProviderType</c> is the numeric enum value (the API serializes enums as numbers
/// for this client; see <see cref="YallaJo.Web.Services.ApiClient"/> deserialize options).
/// </summary>
public sealed class ProviderSettingsResponse
{
    public string BusinessName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int ProviderType { get; init; }
    public string? TypeSpecificDataJson { get; init; }
}
