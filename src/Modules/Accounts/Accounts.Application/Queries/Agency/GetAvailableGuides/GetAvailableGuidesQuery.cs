using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Agency.GetAvailableGuides;

public sealed record AvailableGuideDto(Guid UserId, string BusinessName, string ContactEmail, DateTime CreatedAt);

public sealed record GetAvailableGuidesQuery(int Page = 1, int PageSize = 20) : IQuery<IReadOnlyList<AvailableGuideDto>>;
