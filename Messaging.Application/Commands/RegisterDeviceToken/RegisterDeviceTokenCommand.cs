using MediatR;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(
    Guid UserId,
    string DeviceId,
    DevicePlatform Platform,
    string Token) : IRequest<Result<Guid>>;
