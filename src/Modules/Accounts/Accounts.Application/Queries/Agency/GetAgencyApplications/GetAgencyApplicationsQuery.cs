using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Agency.GetAgencyApplications;

public sealed record AgencyApplicationDto(
    Guid Id,
    Guid GuideUserId,
    Guid AgencyUserId,
    string? Message,
    AgencyApplicationStatus Status,
    string? RejectionReason,
    DateTime? ReviewedAt,
    DateTime CreatedAt);

public sealed record GetAgencyApplicationsQuery : IQuery<IReadOnlyList<AgencyApplicationDto>>;
