using Accounts.Domain.Enums;

namespace Accounts.Application.Commands.Provider.AddDocument;

public sealed record AddProviderDocumentResult(Guid DocumentId, DocumentType DocumentType, string FileUrl);
