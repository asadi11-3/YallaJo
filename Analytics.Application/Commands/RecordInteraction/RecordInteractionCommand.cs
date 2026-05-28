using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.RecordInteraction;

public sealed record RecordInteractionCommand(Guid? UserId, string? SessionId, string EntityType, Guid EntityId, string InteractionType, string? ClientIp, string? UserAgent) : ICommand;
