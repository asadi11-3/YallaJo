using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Admin.RejectProvider;

public sealed record RejectProviderCommand(Guid ApplicationId, string Reason) : ICommand<RejectProviderResult>;
