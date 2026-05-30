using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Agency.GetMyInvitations;

public sealed record InvitationDto(
    Guid Id,
    Guid AgencyUserId,
    Guid GuideUserId,
    string? Message,
    decimal ProposedCommissionPercentage,
    AgencyInvitationStatus Status,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    DateTime CreatedAt);

/// <summary>Direction: 'received' for guide, 'sent' for agency.</summary>
public sealed record GetMyInvitationsQuery(string Direction = "received") : IQuery<IReadOnlyList<InvitationDto>>;
