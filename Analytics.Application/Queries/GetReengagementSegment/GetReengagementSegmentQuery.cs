using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetReengagementSegment;

public sealed record GetReengagementSegmentQuery(
    string Rule,
    EntityType? EntityKind = null,
    Guid? EntityId = null) : IQuery<SegmentResponse>;

public sealed record SegmentResponse(IReadOnlyList<Guid> UserIds, int Count);
