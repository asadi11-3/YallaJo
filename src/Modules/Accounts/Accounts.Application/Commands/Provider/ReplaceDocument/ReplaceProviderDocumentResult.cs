namespace Accounts.Application.Commands.Provider.ReplaceDocument;

// Patch 2G: NewFileUrl removed (V2 breaking change). The authorized download endpoint
// GET /api/v1/provider/documents/{documentId}/download streams via FileAsset V2 — clients
// no longer need a legacy on-disk URL in replace-upload responses.
public sealed record ReplaceProviderDocumentResult(Guid DocumentId);
