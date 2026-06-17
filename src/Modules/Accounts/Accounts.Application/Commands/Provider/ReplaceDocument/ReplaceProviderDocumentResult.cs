namespace Accounts.Application.Commands.Provider.ReplaceDocument;

/// <param name="NewFileUrl">
/// DEPRECATED in V2: legacy on-disk URL (/uploads/provider-application-documents/...) that is
/// 404'd by the API static-file middleware (Patch 1A) and will be removed in a future patch.
/// Prefer the authorized download endpoint: GET /api/v1/provider/documents/{documentId}/download.
/// </param>
public sealed record ReplaceProviderDocumentResult(Guid DocumentId, string NewFileUrl);
