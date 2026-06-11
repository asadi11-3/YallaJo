using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.GetMyTourStatusCounts;

/// <summary>
/// [Backend] B1 — per-status tour counts for the provider "my tours" listing tabs.
/// Owner-scoped: counts only tours created by <paramref name="EffectiveUserId"/>.
/// </summary>
public sealed record GetMyTourStatusCountsQuery(Guid EffectiveUserId)
    : IQuery<TourStatusCountsDto>;
