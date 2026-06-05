namespace YallaJo.Web.Areas.Provider.Models;

/// <summary>
/// Mirrors the API <c>RegisterProviderRequest</c> for <c>POST /api/v1/provider/register</c>.
/// <c>Type</c> is sent as the enum name string (the API accepts string enum values).
/// </summary>
public sealed record RegisterProviderRequest(
    string Type,
    string BusinessName,
    string ContactEmail,
    string ContactPhone,
    string Address,
    string Description,
    string? TypeSpecificDataJson = null);

/// <summary>
/// Mirrors the API <c>ReplaceProviderDocumentRequest</c> for
/// <c>PUT /api/v1/provider/documents/{id}</c>. Sends the new file metadata
/// (the file bytes are uploaded separately; this swaps the stored reference).
/// </summary>
public sealed record ReplaceProviderDocumentRequest(
    string FileUrl,
    string FileName,
    long FileSizeBytes,
    DateTime? ExpiresAt);
