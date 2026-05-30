using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Admin.RequestMoreDocs;

public sealed record RequestMoreDocsCommand(
    Guid ApplicationId,
    IReadOnlyList<DocumentType> MissingDocumentTypes,
    string Notes) : ICommand<RequestMoreDocsResult>;
