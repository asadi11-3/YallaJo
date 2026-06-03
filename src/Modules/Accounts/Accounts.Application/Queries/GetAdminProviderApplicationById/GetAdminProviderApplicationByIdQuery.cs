using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetAdminProviderApplicationById;

public sealed record GetAdminProviderApplicationByIdQuery(Guid ApplicationId)
    : IQuery<AdminProviderApplicationDetailsResult>;
