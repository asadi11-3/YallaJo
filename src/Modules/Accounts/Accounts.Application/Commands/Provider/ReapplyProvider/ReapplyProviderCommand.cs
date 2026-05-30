using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.ReapplyProvider;

public sealed record ReapplyProviderCommand : ICommand<ReapplyProviderResult>;
