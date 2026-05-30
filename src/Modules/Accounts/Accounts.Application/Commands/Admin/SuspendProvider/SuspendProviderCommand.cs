using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Admin.SuspendProvider;

public sealed record SuspendProviderCommand(Guid ApplicationId, string Reason) : ICommand<SuspendProviderResult>;
