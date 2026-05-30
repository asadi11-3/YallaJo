using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.UpdatePreferences;

internal sealed class UpdatePreferencesCommandHandler(
    INotificationPreferenceRepository preferenceRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<UpdatePreferencesCommand, Result>
{
    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        foreach (var upd in request.Updates)
        {
            // M-R1: Critical types cannot be disabled
            if (!upd.IsEnabled && upd.Type.IsCritical())
                return Result.Failure(
                    new Error("NotificationPreference.CannotDisableCritical",
                        $"Notification type '{upd.Type}' is critical and cannot be disabled."),
                    Outcome.UnprocessableEntity);

            var existing = await preferenceRepository.GetByUserAndTypeChannelAsync(request.UserId, upd.Type, upd.Channel, cancellationToken);
            if (existing is not null)
            {
                existing.SetEnabled(upd.IsEnabled);
            }
            else
            {
                var pref = NotificationPreference.Create(request.UserId, upd.Type, upd.Channel, upd.IsEnabled);
                await preferenceRepository.UpsertAsync(pref, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(MessagingCacheKeys.NotificationPreferencesTag(request.UserId), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
