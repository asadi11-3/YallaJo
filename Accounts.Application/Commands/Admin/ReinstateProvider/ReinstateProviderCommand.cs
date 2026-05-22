using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Admin.ReinstateProvider;

public sealed record ReinstateProviderCommand(Guid ApplicationId) : ICommand<ReinstateProviderResult>;
