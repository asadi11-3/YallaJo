using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.RequestMoreDocs;

public sealed record RequestMoreDocsCommand(Guid Id, string Reason, Guid ReviewedByUserId) : ICommand;
