using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Agency.GetAgencies;

public sealed record GetAgenciesQuery(int Page = 1, int PageSize = 20) : IQuery<GetAgenciesResult>;



public sealed record AgencyListItemDto(
    Guid UserId,
    string BusinessName,
    string ContactEmail,
    string? Description,
    DateTime ApprovedAt);
