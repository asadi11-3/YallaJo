using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<Result<int>>;
