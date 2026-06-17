using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.DownloadProviderDocument;

/// <summary>
/// Requests an authorized, server-mediated download of a provider application document.
/// The handler verifies the current user owns the document's parent application (or is
/// admin-tier) before opening the file stream. Intentionally NOT cacheable — it returns
/// a live file stream tied to the request.
/// </summary>
public sealed record DownloadProviderDocumentQuery(Guid DocumentId)
    : IQuery<DownloadProviderDocumentResult>;
