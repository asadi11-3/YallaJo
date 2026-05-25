using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourProposal.Create;

public sealed record CreateTourProposalCommand(
    string Title,
    string Description,
    string? ShortDescription,
    Guid PlaceId,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    bool RequestExclusive)
    : ICommand<Guid>;
