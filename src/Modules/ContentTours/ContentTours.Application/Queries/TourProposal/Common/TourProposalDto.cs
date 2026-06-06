using ContentTours.Domain.Enums;
using TourProposalEntity = ContentTours.Domain.Entities.TourProposal;

namespace ContentTours.Application.Queries.TourProposal.Common;

public sealed record TourProposalDto(
    Guid Id,
    Guid TourGuideId,
    Guid GuideUserId,
    TourProposalStatus Status,
    string Title,
    string Description,
    string ShortDescription,
    Guid PlaceId,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    bool RequestExclusive,
    Guid? ReviewedByAdminId,
    DateTime? ReviewedAt,
    string? RejectionReason,
    Guid? CreatedTourId,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static TourProposalDto From(TourProposalEntity proposal) => new(
        proposal.Id,
        proposal.TourGuideId,
        proposal.GuideUserId,
        proposal.Status,
        proposal.Title,
        proposal.Description,
        proposal.ShortDescription,
        proposal.PlaceId,
        proposal.DurationMinutes,
        proposal.MaxGroupSize,
        proposal.BasePrice,
        proposal.Currency,
        proposal.RequestExclusive,
        proposal.ReviewedByAdminId,
        proposal.ReviewedAt,
        proposal.RejectionReason,
        proposal.CreatedTourId,
        proposal.CreatedAt,
        proposal.UpdatedAt);
}
