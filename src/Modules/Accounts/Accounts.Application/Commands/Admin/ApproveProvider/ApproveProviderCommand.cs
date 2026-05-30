using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Admin.ApproveProvider;

public sealed record ApproveProviderCommand(Guid ApplicationId) : ICommand<ApproveProviderResult>;
