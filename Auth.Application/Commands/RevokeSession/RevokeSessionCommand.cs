// Auth.Application/Commands/RevokeSession/RevokeSessionCommand.cs
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId) : ICommand;
