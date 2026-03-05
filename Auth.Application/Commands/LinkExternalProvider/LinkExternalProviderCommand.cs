using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.LinkExternalProvider;

public sealed record LinkExternalProviderCommand(
    string Provider,
    string ProviderUserId,
    string? ProviderEmail) : ICommand<Guid>;
