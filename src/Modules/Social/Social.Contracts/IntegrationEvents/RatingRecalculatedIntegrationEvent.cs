using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

/// <summary>social.rating.recalculated.v1 — emitted after Bayesian rating recalculation.</summary>
public sealed record RatingRecalculatedIntegrationEvent(
    string TargetType, Guid TargetId,
    decimal NewAverageRating, decimal NewBayesianScore,
    int ReviewCount, DateTime RecalculatedAt) : IntegrationEventBase;
