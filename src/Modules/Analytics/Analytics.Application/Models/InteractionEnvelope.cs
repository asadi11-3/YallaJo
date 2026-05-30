using Analytics.Domain.Enums;

namespace Analytics.Application.Models;

public readonly record struct InteractionEnvelope(
    Guid? UserId,
    string? SessionId,
    EntityType EntityType,
    Guid EntityId,
    InteractionType InteractionType,
    DateTime OccurredAt,
    string? ClientIpHash,
    string? UserAgent);
