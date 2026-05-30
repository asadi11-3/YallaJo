using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.DeleteDeviceToken;

public sealed record DeleteDeviceTokenCommand(Guid TokenId, Guid CallerUserId) : IRequest<Result>;
