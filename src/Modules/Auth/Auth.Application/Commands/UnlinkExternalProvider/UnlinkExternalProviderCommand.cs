using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.UnlinkExternalProvider;

public sealed record UnlinkExternalProviderCommand(Guid ExternalProviderId) : ICommand;
