using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyDeviceTokens;

public sealed record DeviceTokenDto(Guid Id, string DeviceId, string Platform, DateTime LastSeenAt, DateTime CreatedAt);

public sealed record GetMyDeviceTokensQuery(Guid UserId) : IRequest<Result<IReadOnlyList<DeviceTokenDto>>>;
