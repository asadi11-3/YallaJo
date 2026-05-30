using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Agency.GetAgencyDetail;

/// <summary>Public detail view of a single agency.</summary>
public sealed record GetAgencyDetailQuery(Guid AgencyUserId) : IQuery<AgencyDetailDto>;

public sealed record AgencyDetailDto(
    Guid UserId,
    string BusinessName,
    string ContactEmail,
    string ContactPhone,
    string? Address,
    string? Description,
    ProviderType ProviderType,
    int ActiveGuideCount,
    DateTime ApprovedAt);
