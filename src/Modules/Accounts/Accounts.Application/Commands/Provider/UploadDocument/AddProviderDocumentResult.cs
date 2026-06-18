using Accounts.Domain.Enums;

namespace Accounts.Application.Commands.Provider.UploadDocument;

public sealed record AddProviderDocumentResult(Guid DocumentId, DocumentType DocumentType);
