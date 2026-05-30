using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.RegisterDeviceToken;

internal sealed class RegisterDeviceTokenCommandHandler(
    IDeviceTokenRepository deviceTokenRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider) : IRequestHandler<RegisterDeviceTokenCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        // M-R9: UPSERT by (UserId, DeviceId) — unique per device
        var existing = await deviceTokenRepository.GetByUserAndDeviceIdAsync(request.UserId, request.DeviceId, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateToken(request.Token);
            existing.MarkSeen(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await cache.RemoveByTagAsync(MessagingCacheKeys.DeviceTokensTag(request.UserId), cancellationToken).ConfigureAwait(false);
            return Result.Success(existing.Id);
        }

        var token = DeviceToken.Register(request.UserId, request.DeviceId, request.Token, request.Platform, timeProvider);
        await deviceTokenRepository.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(MessagingCacheKeys.DeviceTokensTag(request.UserId), cancellationToken).ConfigureAwait(false);
        return Result.Success(token.Id);
    }
}
