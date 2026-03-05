using Auth.Domain.Repositories;
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

        // 1. Load device (tracked — will be mutated)
        var device = await deviceRepository.GetByIdAsync(
            request.DeviceId,
            ct: ct,
            asNoTracking: false);

        if (device is null)
            return Result.NotFound($"Device {request.DeviceId} was not found.");

        // 2. Security: caller may only trust their own devices
        if (device.UserId != currentUser.UserId.Value)
            return Result.Forbidden("You are not authorized to trust this device.");

        // 3. Trust (idempotent — already-trusted is fine)
        if (!device.IsTrusted)
            device.Trust();

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
