using Auth.Application.Caching;
using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.UntrustDevice;

public sealed class UntrustDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<UntrustDeviceCommand>
{
    public async Task<Result> Handle(UntrustDeviceCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Unauthorized("Authentication is required.");

        var device = await deviceRepository.GetByIdAsync(
            request.DeviceId,
            ct: ct,
            asNoTracking: false);

        if (device is null)
            return Result.NotFound($"Device {request.DeviceId} was not found.");

        if (device.UserId != currentUser.UserId.Value)
            return Result.Forbidden("You are not authorized to untrust this device.");

        if (device.IsTrusted)
            device.Untrust();

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("Device.ConcurrencyConflict", "The device was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        // The sessions list (which displays IsTrusted) is cached per user —
        // invalidate so the change is visible immediately.
        await cache.RemoveByTagAsync(AuthCacheKeys.UserSessionsTag(currentUser.UserId.Value), ct);

        return Result.Success();
    }
}
