using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetRatingSummariesBatch;

/// <summary>
/// [Backend] B6 — batch rating summaries for up to 50 entities in one grouped query (API7).
/// Replaces N single-entity /ratings calls.
/// </summary>
public sealed record GetRatingSummariesBatchQuery(
    ReviewTargetType EntityType,
    IReadOnlyList<Guid> EntityIds)
    : IQuery<IReadOnlyList<RatingSummaryBatchItemDto>>;

/// <summary>Rating aggregate for one entity. Entities without reviews are returned with zeros.</summary>
public sealed record RatingSummaryBatchItemDto(Guid EntityId, decimal Average, int Count);
