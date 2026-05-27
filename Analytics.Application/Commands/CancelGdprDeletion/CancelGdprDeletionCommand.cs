using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CancelGdprDeletion;

public sealed record CancelGdprDeletionCommand(Guid UserId) : ICommand;
