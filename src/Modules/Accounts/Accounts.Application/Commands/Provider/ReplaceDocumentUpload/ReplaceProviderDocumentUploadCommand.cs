using Accounts.Application.Commands.Provider.ReplaceDocument;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.ReplaceDocumentUpload;

/// <summary>
/// Replaces an application document with an actual uploaded file (multipart).
/// Additive sibling of <see cref="ReplaceDocument.ReplaceProviderDocumentCommand"/>, which
/// expects a pre-existing URL; this command stores the file first, then swaps the document.
/// </summary>
public sealed record ReplaceProviderDocumentUploadCommand(
    Guid DocumentId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime? ExpiresAt) : ICommand<ReplaceProviderDocumentResult>;
