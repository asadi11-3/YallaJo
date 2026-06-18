using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.UploadDocument;

public sealed record UploadProviderDocumentCommand(
    DocumentType DocumentType,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime? ExpiresAt) : ICommand<AddProviderDocumentResult>;
