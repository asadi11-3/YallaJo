using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.RequestGdprDeletion;

public sealed record RequestGdprDeletionCommand(Guid UserId) : ICommand;
