using ContentTours.Application.Queries.TourProposal.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourProposal.GetTourProposalById;

public sealed record GetTourProposalByIdQuery(Guid Id) : IQuery<TourProposalDto>;
