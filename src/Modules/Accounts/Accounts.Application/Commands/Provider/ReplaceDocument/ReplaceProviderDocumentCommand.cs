using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.ReplaceDocument;

public sealed record ReplaceProviderDocumentCommand(
    Guid DocumentId,
    string FileUrl,
    string FileName,
    long FileSizeBytes,
    DateTime? ExpiresAt) : ICommand<ReplaceProviderDocumentResult>;
