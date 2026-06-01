using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.TrustDevice;

public sealed class TrustDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<TrustDeviceCommand>
{
    public async Task<Result> Handle(TrustDeviceCommand request, CancellationToken ct)
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
            return Result.Forbidden("You are not authorized to trust this device.");

        if (!device.IsTrusted)
            device.Trust();

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

        return Result.Success();
    }
}
