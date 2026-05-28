using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Agency.GetMyAgencyGuides;

public sealed record AgencyGuideDto(Guid AffiliationId, Guid GuideUserId, decimal CommissionPercentage, DateTime JoinedAt);

public sealed record GetMyAgencyGuidesQuery : IQuery<IReadOnlyList<AgencyGuideDto>>;
