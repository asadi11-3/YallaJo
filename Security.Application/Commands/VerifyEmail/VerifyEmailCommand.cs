using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(Guid UserId, Guid EmailId) : ICommand;
