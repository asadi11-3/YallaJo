using ContentTours.Application.Queries.TourProposal.Common;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourProposal.ListTourProposals;

/// <summary>
/// List tour proposals. When <paramref name="GuideId"/> is supplied, returns that
/// guide's own proposals (optionally filtered by status). When omitted, returns the
/// admin-review queue (currently-submitted proposals).
/// </summary>
public sealed record ListTourProposalsQuery(Guid? GuideId = null, TourProposalStatus? Status = null)
    : IQuery<IReadOnlyList<TourProposalDto>>;
