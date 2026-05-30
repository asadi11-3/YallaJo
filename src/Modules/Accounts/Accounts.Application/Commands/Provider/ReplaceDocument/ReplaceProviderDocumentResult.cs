namespace Accounts.Application.Commands.Provider.ReplaceDocument;

public sealed record ReplaceProviderDocumentResult(Guid DocumentId, string NewFileUrl);
