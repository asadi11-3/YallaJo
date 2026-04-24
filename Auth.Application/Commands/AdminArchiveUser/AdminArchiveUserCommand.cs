using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AdminArchiveUser;

public sealed record AdminArchiveUserCommand(Guid UserId) : ICommand;
