using Accounts.Application.Commands.Provider.ReplaceDocument;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.ReplaceDocumentUpload;

/// <summary>
/// Replaces an application document with an actual uploaded file (multipart).
/// Stores the file first (with signature validation + FileAsset materialization), then swaps
/// the document. This is the only supported replace path; the legacy URL-based JSON command was
/// removed in Patch 2F.
/// </summary>
public sealed record ReplaceProviderDocumentUploadCommand(
    Guid DocumentId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime? ExpiresAt) : ICommand<ReplaceProviderDocumentResult>;
