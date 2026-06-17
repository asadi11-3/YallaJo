using Accounts.Domain.Enums;

namespace Accounts.Application.Commands.Provider.AddDocument;

// Patch 2G: FileUrl removed (V2 breaking change). The authorized download endpoint
// GET /api/v1/provider/documents/{documentId}/download streams via FileAsset V2 — clients
// no longer need a legacy on-disk URL in upload responses.
public sealed record AddProviderDocumentResult(Guid DocumentId, DocumentType DocumentType);
