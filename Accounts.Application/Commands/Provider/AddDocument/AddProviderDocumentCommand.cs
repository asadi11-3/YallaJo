using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.AddDocument;

public sealed record AddProviderDocumentCommand(
    DocumentType DocumentType,
    string FileUrl,
    string FileName,
    long FileSizeBytes,
    DateTime? ExpiresAt) : ICommand<AddProviderDocumentResult>;
